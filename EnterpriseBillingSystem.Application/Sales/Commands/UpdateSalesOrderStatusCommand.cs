using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using EnterpriseBillingSystem.Domain.Entities;
using EnterpriseBillingSystem.Domain.Enums;
using EnterpriseBillingSystem.Domain.Repositories;
using EnterpriseBillingSystem.Application.Common.Interfaces;

namespace EnterpriseBillingSystem.Application.Sales.Commands;

public record UpdateSalesOrderStatusCommand(Guid SalesOrderId, SalesOrderStatus Status) : IRequest<Unit>;

public class UpdateSalesOrderStatusCommandHandler : IRequestHandler<UpdateSalesOrderStatusCommand, Unit>
{
    private readonly ISalesOrderRepository _salesOrderRepository;
    private readonly IInventoryRepository _inventoryRepository;
    private readonly IInventoryMovementRepository _movementRepository;
    private readonly IProductRepository _productRepository;
    private readonly IRepository<BranchWarehouse> _branchWarehouseRepository;
    private readonly ICurrentUserService _currentUserService;
    private readonly IUnitOfWork _unitOfWork;

    public UpdateSalesOrderStatusCommandHandler(
        ISalesOrderRepository salesOrderRepository,
        IInventoryRepository inventoryRepository,
        IInventoryMovementRepository movementRepository,
        IProductRepository productRepository,
        IRepository<BranchWarehouse> branchWarehouseRepository,
        ICurrentUserService currentUserService,
        IUnitOfWork unitOfWork)
    {
        _salesOrderRepository = salesOrderRepository;
        _inventoryRepository = inventoryRepository;
        _movementRepository = movementRepository;
        _productRepository = productRepository;
        _branchWarehouseRepository = branchWarehouseRepository;
        _currentUserService = currentUserService;
        _unitOfWork = unitOfWork;
    }

    public async Task<Unit> Handle(UpdateSalesOrderStatusCommand request, CancellationToken cancellationToken)
    {
        var order = await _salesOrderRepository.GetByIdWithDetailsAsync(request.SalesOrderId, cancellationToken);
        if (order == null)
            throw new ArgumentException($"El pedido con Id '{request.SalesOrderId}' no existe.");

        // Deduct from inventory when transitioning to "EnCamino"
        if (request.Status == SalesOrderStatus.EnCamino && order.Status != SalesOrderStatus.EnCamino)
        {
            var warehouse = (await _branchWarehouseRepository.FindAsync(bw => bw.IsDefault && bw.IsActive)).FirstOrDefault()
                ?? (await _branchWarehouseRepository.FindAsync(bw => bw.IsActive)).FirstOrDefault();

            if (warehouse == null)
                throw new InvalidOperationException("No hay bodegas activas configuradas en el sistema para realizar el despacho.");

            var movementNumber = await _movementRepository.GenerateMovementNumberAsync(cancellationToken);
            var movement = new InventoryMovement
            {
                Id = Guid.NewGuid(),
                BranchId = warehouse.BranchId,
                MovementNumber = movementNumber,
                MovementType = MovementType.Exit,
                FromBranchWarehouseId = warehouse.Id,
                ToBranchWarehouseId = null,
                ReferenceDocument = order.OrderNumber,
                Notes = $"Salida por despacho de Pedido {order.OrderNumber}",
                MovementDate = DateTime.UtcNow,
                CreatedBy = _currentUserService.UserId ?? "System",
                CreatedOnUtc = DateTime.UtcNow
            };

            bool requiresMovement = false;

            var faltantesList = new List<string>();

            foreach (var detail in order.Details)
            {
                var product = await _productRepository.GetByIdWithDetailsAsync(detail.ProductId, cancellationToken);
                if (product == null)
                    throw new ArgumentException($"El producto con Id '{detail.ProductId}' no existe.");

                // If service or inventory tracking disabled, skip
                if (product.ProductType == ProductType.Service || !product.TrackInventory)
                    continue;

                // Find corresponding presentation
                var presentation = product.Presentations.FirstOrDefault(p => p.UnitOfMeasureId == detail.UnitOfMeasureId)
                    ?? product.Presentations.FirstOrDefault();

                if (presentation == null)
                    throw new InvalidOperationException($"El producto '{product.Name}' no tiene presentaciones configuradas.");

                decimal conversionFactor = presentation.ConversionFactor;
                
                // Get inventory record
                var inventory = await _inventoryRepository.GetByWarehouseAndProductAsync(warehouse.Id, detail.ProductId, cancellationToken);
                if (inventory != null && inventory.IsDeleted)
                {
                    inventory.IsDeleted = false;
                    inventory.PhysicalStock = 0;
                    inventory.ReservedStock = 0;
                    inventory.CommittedStock = 0;
                }
                decimal availableStockInBaseUnit = inventory?.PhysicalStock ?? 0;
                if (availableStockInBaseUnit < 0) availableStockInBaseUnit = 0;

                decimal requestedInBaseUnit = detail.Quantity * conversionFactor;
                decimal dispatchedInBaseUnit = requestedInBaseUnit;
                decimal dispatchedQty = detail.Quantity;

                if (availableStockInBaseUnit < requestedInBaseUnit)
                {
                    dispatchedInBaseUnit = availableStockInBaseUnit;
                    dispatchedQty = conversionFactor > 0 ? (dispatchedInBaseUnit / conversionFactor) : 0;
                    decimal missingQty = detail.Quantity - dispatchedQty;

                    if (missingQty > 0)
                    {
                        string uomLabel = detail.UnitOfMeasure != null ? detail.UnitOfMeasure.Name : "U/E";
                        faltantesList.Add($"- {product.Name}: Faltó {missingQty:N2} {uomLabel} (Solicitado: {detail.Quantity:N2}, Despachado: {dispatchedQty:N2})");
                    }

                    // Update order detail line quantities and amounts
                    detail.Quantity = Math.Round(dispatchedQty, 4);
                    detail.DiscountAmount = Math.Round((detail.Quantity * detail.UnitPrice) * (detail.DiscountPercentage / 100m), 4);
                    
                    decimal lineSubtotal = (detail.Quantity * detail.UnitPrice) - detail.DiscountAmount;
                    detail.TaxAmount = Math.Round(lineSubtotal * (detail.TaxPercentage / 100m), 4);
                    detail.NetAmount = Math.Round(lineSubtotal + detail.TaxAmount, 4);
                }

                dispatchedInBaseUnit = Math.Round(dispatchedInBaseUnit, 4);
                dispatchedQty = Math.Round(dispatchedQty, 4);

                if (dispatchedInBaseUnit > 0.0000m && dispatchedQty > 0.0000m)
                {
                    if (inventory == null)
                    {
                        inventory = new Domain.Entities.Inventory
                        {
                            Id = Guid.NewGuid(),
                            BranchWarehouseId = warehouse.Id,
                            ProductId = detail.ProductId,
                            PhysicalStock = 0,
                            ReservedStock = 0,
                            CommittedStock = 0,
                            CreatedBy = _currentUserService.UserId ?? "System",
                            CreatedOnUtc = DateTime.UtcNow
                        };
                        await _inventoryRepository.AddAsync(inventory);
                    }

                    // Deduct from stock
                    inventory.PhysicalStock -= dispatchedInBaseUnit;
                    _inventoryRepository.Update(inventory);

                    // Add Kardex movement detail
                    movement.Details.Add(new InventoryMovementDetail
                    {
                        Id = Guid.NewGuid(),
                        InventoryMovementId = movement.Id,
                        BranchId = warehouse.BranchId,
                        ProductId = detail.ProductId,
                        Quantity = dispatchedQty,
                        UnitOfMeasureId = detail.UnitOfMeasureId,
                        ProductPresentationId = presentation.Id,
                        ConversionFactor = conversionFactor,
                        QuantityInBaseUnit = dispatchedInBaseUnit,
                        CreatedBy = _currentUserService.UserId ?? "System",
                        CreatedOnUtc = DateTime.UtcNow
                    });

                    requiresMovement = true;
                }

                // AutoMarkSoldOut check
                if (product.AutoMarkSoldOut)
                {
                    var activeWarehouses = await _branchWarehouseRepository.FindAsync(bw => bw.IsActive);
                    var activeWarehouseIds = activeWarehouses.Select(w => w.Id).ToList();
                    var inventories = await _inventoryRepository.FindAsync(i => i.ProductId == product.Id);
                    
                    var totalPhysicalStock = inventories
                        .Where(i => activeWarehouseIds.Contains(i.BranchWarehouseId))
                        .Sum(i => i.PhysicalStock);

                    var newIsSoldOut = totalPhysicalStock <= 0;
                    if (product.IsSoldOut != newIsSoldOut)
                    {
                        product.IsSoldOut = newIsSoldOut;
                        product.SoldOutAt = newIsSoldOut ? DateTime.UtcNow : null;
                        product.SoldOutBy = newIsSoldOut ? (_currentUserService.UserId ?? "System") : null;
                        _productRepository.Update(product);
                    }
                }
            }

            if (requiresMovement)
            {
                await _movementRepository.AddAsync(movement);
            }

            // If there were missing items, update totals and append notes
            if (faltantesList.Any())
            {
                order.SubTotal = order.Details.Sum(d => d.Quantity * d.UnitPrice);
                order.DiscountAmount = order.Details.Sum(d => d.DiscountAmount);
                order.TaxAmount = order.Details.Sum(d => d.TaxAmount);
                order.TotalAmount = order.Details.Sum(d => d.NetAmount);

                var faltantesText = "\n[Faltantes]:\n" + string.Join("\n", faltantesList);
                order.Notes = (order.Notes ?? "") + faltantesText;
            }
        }

        order.Status = request.Status;
        order.LastModifiedBy = _currentUserService.UserId ?? "System";
        order.LastModifiedOnUtc = DateTime.UtcNow;

        _salesOrderRepository.Update(order);

        try
        {
            await _unitOfWork.SaveChangesAsync(cancellationToken);
        }
        catch (Microsoft.EntityFrameworkCore.DbUpdateException dbEx)
        {
            var innerMessage = dbEx.InnerException?.Message ?? dbEx.Message;
            throw new InvalidOperationException($"Error de persistencia en la base de datos: {innerMessage}", dbEx);
        }

        return Unit.Value;
    }
}
