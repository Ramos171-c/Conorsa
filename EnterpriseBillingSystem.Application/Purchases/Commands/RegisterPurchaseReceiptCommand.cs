using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using FluentValidation;
using EnterpriseBillingSystem.Domain.Entities;
using EnterpriseBillingSystem.Domain.Enums;
using EnterpriseBillingSystem.Domain.Repositories;
using EnterpriseBillingSystem.Application.Common.Interfaces;

namespace EnterpriseBillingSystem.Application.Purchases.Commands;

// ─── DTOs ────────────────────────────────────────────────────────────────────

public record ReceiptDetailRequest(
    Guid ProductId,
    Guid ProductPresentationId,
    decimal Quantity,
    decimal UnitPrice
);

public record RegisterPurchaseReceiptCommand(
    Guid SupplierId,
    Guid BranchWarehouseId,
    Guid? PurchaseOrderId,           // Null = compra directa
    DateTime ReceiptDate,
    string? ReferenceDocument,
    string? Notes,
    List<ReceiptDetailRequest> Details
) : IRequest<Guid>;

// ─── Validator ────────────────────────────────────────────────────────────────

public class RegisterPurchaseReceiptCommandValidator : AbstractValidator<RegisterPurchaseReceiptCommand>
{
    public RegisterPurchaseReceiptCommandValidator()
    {
        RuleFor(x => x.SupplierId)
            .NotEmpty().WithMessage("El proveedor es requerido.");

        RuleFor(x => x.BranchWarehouseId)
            .NotEmpty().WithMessage("La bodega de destino es requerida.");

        RuleFor(x => x.ReceiptDate)
            .NotEmpty().WithMessage("La fecha de recepción es requerida.");

        RuleFor(x => x.Details)
            .NotEmpty().WithMessage("La recepción debe tener al menos un detalle.");

        RuleForEach(x => x.Details).ChildRules(detail =>
        {
            detail.RuleFor(d => d.ProductId)
                .NotEmpty().WithMessage("El producto es requerido.");

            detail.RuleFor(d => d.ProductPresentationId)
                .NotEmpty().WithMessage("La presentación del producto es requerida.");

            detail.RuleFor(d => d.Quantity)
                .GreaterThan(0).WithMessage("La cantidad recibida debe ser mayor a 0.");

            detail.RuleFor(d => d.UnitPrice)
                .GreaterThanOrEqualTo(0).WithMessage("El precio unitario no puede ser negativo.");
        });
    }
}

// ─── Handler ─────────────────────────────────────────────────────────────────

public class RegisterPurchaseReceiptCommandHandler : IRequestHandler<RegisterPurchaseReceiptCommand, Guid>
{
    private readonly IPurchaseReceiptRepository _receiptRepository;
    private readonly IPurchaseOrderRepository _purchaseOrderRepository;
    private readonly ISupplierRepository _supplierRepository;
    private readonly IProductRepository _productRepository;
    private readonly IRepository<BranchWarehouse> _branchWarehouseRepository;
    private readonly IInventoryRepository _inventoryRepository;
    private readonly IInventoryMovementRepository _movementRepository;
    private readonly ICurrentUserService _currentUserService;
    private readonly IUnitOfWork _unitOfWork;

    public RegisterPurchaseReceiptCommandHandler(
        IPurchaseReceiptRepository receiptRepository,
        IPurchaseOrderRepository purchaseOrderRepository,
        ISupplierRepository supplierRepository,
        IProductRepository productRepository,
        IRepository<BranchWarehouse> branchWarehouseRepository,
        IInventoryRepository inventoryRepository,
        IInventoryMovementRepository movementRepository,
        ICurrentUserService currentUserService,
        IUnitOfWork unitOfWork)
    {
        _receiptRepository = receiptRepository;
        _purchaseOrderRepository = purchaseOrderRepository;
        _supplierRepository = supplierRepository;
        _productRepository = productRepository;
        _branchWarehouseRepository = branchWarehouseRepository;
        _inventoryRepository = inventoryRepository;
        _movementRepository = movementRepository;
        _currentUserService = currentUserService;
        _unitOfWork = unitOfWork;
    }

    public async Task<Guid> Handle(RegisterPurchaseReceiptCommand request, CancellationToken cancellationToken)
    {
        try
        {
            // 1. Validar proveedor
            var supplier = await _supplierRepository.GetByIdAsync(request.SupplierId);
            if (supplier == null)
                throw new ArgumentException("El proveedor especificado no existe.");
            if (supplier.Status == SupplierStatus.Blocked || supplier.Status == SupplierStatus.Inactive)
                throw new InvalidOperationException($"El proveedor '{supplier.Name}' no está disponible para transacciones.");

            // 2. Validar bodega de destino
            var branchWarehouse = await _branchWarehouseRepository.GetByIdAsync(request.BranchWarehouseId);
            if (branchWarehouse == null)
                throw new ArgumentException("La bodega de sucursal especificada no existe.");
            if (!branchWarehouse.IsActive)
                throw new InvalidOperationException("La bodega de sucursal especificada no está activa.");

            // 3. Validar orden de compra (si se especificó)
            PurchaseOrder? purchaseOrder = null;
            if (request.PurchaseOrderId.HasValue && request.PurchaseOrderId.Value != Guid.Empty)
            {
                purchaseOrder = await _purchaseOrderRepository.GetByIdWithDetailsAsync(request.PurchaseOrderId.Value, cancellationToken);
                if (purchaseOrder == null)
                    throw new ArgumentException("La orden de compra especificada no existe.");
                if (purchaseOrder.SupplierId != request.SupplierId)
                    throw new InvalidOperationException("El proveedor de la recepción no coincide con el proveedor de la orden de compra.");
                if (purchaseOrder.Status != PurchaseOrderStatus.Approved && purchaseOrder.Status != PurchaseOrderStatus.PartiallyReceived)
                    throw new InvalidOperationException($"La orden de compra debe estar Aprobada o Parcialmente Recibida. Estado actual: {purchaseOrder.Status}.");
            }

            // Prepare IDs
            var receiptId = Guid.NewGuid();
            var receiptNumber = await _receiptRepository.GenerateReceiptNumberAsync(cancellationToken);

            var movementId = Guid.NewGuid();
            var movementNumber = await _movementRepository.GenerateMovementNumberAsync(cancellationToken);

            var receiptDetails = new List<PurchaseReceiptDetail>();
            var inventoryMovementDetails = new List<(Guid ProductId, Guid PresentationId, Guid UoMId, decimal ConversionFactor, decimal Quantity, decimal UnitPrice)>();

            foreach (var detailReq in request.Details)
            {
                // Validar producto
                var product = await _productRepository.GetByIdWithDetailsAsync(detailReq.ProductId, cancellationToken);
                if (product == null)
                    throw new ArgumentException($"El producto con Id '{detailReq.ProductId}' no existe.");
                if (!product.IsActive)
                    throw new InvalidOperationException($"El producto '{product.Name}' no está activo.");
                if (product.ProductType == ProductType.Service)
                    throw new InvalidOperationException($"El producto '{product.Name}' es un servicio y no puede ser recibido en inventario.");

                // Validar presentación
                var presentation = product.Presentations.FirstOrDefault(p => p.Id == detailReq.ProductPresentationId)
                                   ?? product.Presentations.FirstOrDefault();
                if (presentation == null)
                    throw new ArgumentException($"No se encontró ninguna presentación para el producto '{product.Name}'.");

                // Validar sobrerecepción si hay PO
                if (purchaseOrder != null)
                {
                    var orderDetail = purchaseOrder.Details.FirstOrDefault(d => d.ProductId == detailReq.ProductId);
                    if (orderDetail == null)
                        throw new InvalidOperationException($"El producto '{product.Name}' no se encuentra en la orden de compra.");

                    decimal pendingQty = orderDetail.Quantity - orderDetail.ReceivedQuantity;
                    if (detailReq.Quantity > pendingQty)
                        throw new InvalidOperationException(
                            $"La cantidad a recibir ({detailReq.Quantity}) supera la cantidad pendiente ({pendingQty}) para el producto '{product.Name}'.");

                    // Actualizar cantidad recibida en detalle de OC
                    orderDetail.ReceivedQuantity += detailReq.Quantity;
                }

                receiptDetails.Add(new PurchaseReceiptDetail
                {
                    Id = Guid.NewGuid(),
                    PurchaseReceiptId = receiptId,
                    ProductId = detailReq.ProductId,
                    UnitOfMeasureId = presentation.UnitOfMeasureId,
                    Quantity = detailReq.Quantity,
                    UnitPrice = detailReq.UnitPrice
                });

                inventoryMovementDetails.Add((detailReq.ProductId, presentation.Id, presentation.UnitOfMeasureId, presentation.ConversionFactor, detailReq.Quantity, detailReq.UnitPrice));
            }

            // 5. Actualizar estado de la Orden de Compra si aplica
            if (purchaseOrder != null)
            {
                bool allReceived = purchaseOrder.Details.All(d => d.ReceivedQuantity >= d.Quantity);
                purchaseOrder.Status = allReceived ? PurchaseOrderStatus.Completed : PurchaseOrderStatus.PartiallyReceived;
                purchaseOrder.LastModifiedBy = _currentUserService.UserId ?? "System";
                purchaseOrder.LastModifiedOnUtc = DateTime.UtcNow;
                _purchaseOrderRepository.Update(purchaseOrder);
            }

            // 7. Crear la recepción
            var receipt = new PurchaseReceipt
            {
                Id = receiptId,
                ReceiptNumber = receiptNumber,
                SupplierId = request.SupplierId,
                BranchWarehouseId = request.BranchWarehouseId,
                PurchaseOrderId = (request.PurchaseOrderId.HasValue && request.PurchaseOrderId.Value != Guid.Empty) ? request.PurchaseOrderId : null,
                ReceiptDate = request.ReceiptDate,
                ReferenceDocument = request.ReferenceDocument,
                Notes = request.Notes,
                Status = PurchaseReceiptStatus.Confirmed,
                Details = receiptDetails,
                CreatedBy = _currentUserService.UserId ?? "System",
                CreatedOnUtc = DateTime.UtcNow
            };

            await _receiptRepository.AddAsync(receipt);

            // 8. Impactar inventario
            var movement = new InventoryMovement
            {
                Id = movementId,
                MovementNumber = movementNumber,
                MovementType = MovementType.Entry,
                ToBranchWarehouseId = request.BranchWarehouseId,
                FromBranchWarehouseId = null,
                ReferenceDocument = receipt.ReceiptNumber,
                Notes = $"Recepción de compra {receipt.ReceiptNumber}",
                MovementDate = request.ReceiptDate,
                CreatedBy = _currentUserService.UserId ?? "System",
                CreatedOnUtc = DateTime.UtcNow
            };

            var affectedProducts = new Dictionary<Guid, Product>();

            foreach (var (productId, presentationId, uomId, conversionFactor, quantity, unitPrice) in inventoryMovementDetails)
            {
                var product = await _productRepository.GetByIdWithDetailsAsync(productId, cancellationToken);
                if (product == null) continue;

                if (!affectedProducts.ContainsKey(product.Id))
                {
                    affectedProducts.Add(product.Id, product);
                }

                decimal quantityInBaseUnit = quantity * conversionFactor;

                // Obtener o crear inventario
                var inventory = await _inventoryRepository.GetByWarehouseAndProductAsync(request.BranchWarehouseId, productId, cancellationToken);
                if (inventory == null)
                {
                    inventory = new Domain.Entities.Inventory
                    {
                        Id = Guid.NewGuid(),
                        BranchWarehouseId = request.BranchWarehouseId,
                        ProductId = productId,
                        PhysicalStock = quantityInBaseUnit,
                        ReservedStock = 0,
                        CommittedStock = 0,
                        CreatedBy = _currentUserService.UserId ?? "System",
                        CreatedOnUtc = DateTime.UtcNow
                    };
                    await _inventoryRepository.AddAsync(inventory);
                }
                else
                {
                    if (inventory.IsDeleted)
                    {
                        inventory.IsDeleted = false;
                        inventory.PhysicalStock = 0;
                        inventory.ReservedStock = 0;
                        inventory.CommittedStock = 0;
                    }
                    inventory.PhysicalStock += quantityInBaseUnit;
                    _inventoryRepository.Update(inventory);
                }

                // Detalle del movimiento
                movement.Details.Add(new InventoryMovementDetail
                {
                    Id = Guid.NewGuid(),
                    InventoryMovementId = movementId,
                    ProductId = productId,
                    Quantity = quantity,
                    UnitOfMeasureId = uomId,
                    ProductPresentationId = presentationId,
                    ConversionFactor = conversionFactor,
                    QuantityInBaseUnit = quantityInBaseUnit,
                    CreatedBy = _currentUserService.UserId ?? "System",
                    CreatedOnUtc = DateTime.UtcNow
                });
            }

            await _movementRepository.AddAsync(movement);

            // 8.5 AutoMarkSoldOut
            foreach (var prod in affectedProducts.Values)
            {
                if (prod.AutoMarkSoldOut)
                {
                    var activeWarehouses = await _branchWarehouseRepository.FindAsync(bw => bw.IsActive);
                    var activeWarehouseIds = activeWarehouses.Select(w => w.Id).ToList();
                    var inventories = await _inventoryRepository.FindAsync(i => i.ProductId == prod.Id);
                    
                    var totalPhysicalStock = inventories
                        .Where(i => activeWarehouseIds.Contains(i.BranchWarehouseId))
                        .Sum(i => i.PhysicalStock);

                    var newIsSoldOut = totalPhysicalStock <= 0;
                    if (prod.IsSoldOut != newIsSoldOut)
                    {
                        prod.IsSoldOut = newIsSoldOut;
                        prod.SoldOutAt = newIsSoldOut ? DateTime.UtcNow : null;
                        prod.SoldOutBy = newIsSoldOut ? (_currentUserService.UserId ?? "System") : null;
                        _productRepository.Update(prod);
                    }
                }
            }

            // 9. Guardar todo en una sola transacción
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            return receipt.Id;
        }
        catch (Exception)
        {
            throw;
        }
    }
}
