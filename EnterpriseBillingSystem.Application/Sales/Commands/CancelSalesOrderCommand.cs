using System;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using FluentValidation;
using EnterpriseBillingSystem.Domain.Enums;
using EnterpriseBillingSystem.Domain.Repositories;

namespace EnterpriseBillingSystem.Application.Sales.Commands;

// ─── Command ──────────────────────────────────────────────────────────────────

public record CancelSalesOrderCommand(
    Guid SalesOrderId,
    string? CancellationReason
) : IRequest<Unit>;

// ─── Validator ────────────────────────────────────────────────────────────────

public class CancelSalesOrderCommandValidator : AbstractValidator<CancelSalesOrderCommand>
{
    public CancelSalesOrderCommandValidator()
    {
        RuleFor(x => x.SalesOrderId)
            .NotEmpty().WithMessage("El Id del pedido es requerido.");
    }
}

// ─── Handler ──────────────────────────────────────────────────────────────────

public class CancelSalesOrderCommandHandler : IRequestHandler<CancelSalesOrderCommand, Unit>
{
    private readonly ISalesOrderRepository _salesOrderRepository;
    private readonly IInventoryRepository _inventoryRepository;
    private readonly IInventoryMovementRepository _movementRepository;
    private readonly IProductRepository _productRepository;
    private readonly IRepository<Domain.Entities.BranchWarehouse> _branchWarehouseRepository;
    private readonly IWarehouseRepository _warehouseRepository;
    private readonly Application.Common.Interfaces.ICurrentUserService _currentUserService;
    private readonly IUnitOfWork _unitOfWork;

    public CancelSalesOrderCommandHandler(
        ISalesOrderRepository salesOrderRepository,
        IInventoryRepository inventoryRepository,
        IInventoryMovementRepository movementRepository,
        IProductRepository productRepository,
        IRepository<Domain.Entities.BranchWarehouse> branchWarehouseRepository,
        IWarehouseRepository warehouseRepository,
        Application.Common.Interfaces.ICurrentUserService currentUserService,
        IUnitOfWork unitOfWork)
    {
        _salesOrderRepository = salesOrderRepository;
        _inventoryRepository = inventoryRepository;
        _movementRepository = movementRepository;
        _productRepository = productRepository;
        _branchWarehouseRepository = branchWarehouseRepository;
        _warehouseRepository = warehouseRepository;
        _currentUserService = currentUserService;
        _unitOfWork = unitOfWork;
    }

    public async Task<Unit> Handle(CancelSalesOrderCommand request, CancellationToken cancellationToken)
    {
        var order = await _salesOrderRepository.GetByIdWithDetailsAsync(request.SalesOrderId, cancellationToken);
        if (order == null)
            throw new ArgumentException($"El pedido con Id '{request.SalesOrderId}' no existe.");

        if (order.Status == SalesOrderStatus.Anulado)
            throw new InvalidOperationException("El pedido ya está anulado.");

        // Verificar que no tenga facturas confirmadas
        bool hasPostedInvoices = order.SalesInvoices.Any(si => si.Status == SalesInvoiceStatus.Posted);
        if (hasPostedInvoices)
            throw new InvalidOperationException("No se puede anular un pedido con facturas confirmadas asociadas.");

        // Reversar inventario si el pedido tuvo salidas de inventario (por ejemplo, si pasó a EnCamino)
        var exitMovements = (await _movementRepository.FindAsync(m => m.ReferenceDocument == order.OrderNumber && m.MovementType == MovementType.Exit)).ToList();
        var reversalMovements = (await _movementRepository.FindAsync(m => m.ReferenceDocument == order.OrderNumber && m.MovementType == MovementType.SaleReversal)).ToList();

        if (exitMovements.Any() && !reversalMovements.Any())
        {
            var firstExit = exitMovements.First();
            var warehouseId = firstExit.FromBranchWarehouseId ?? Guid.Empty;

            var movementNumber = await _movementRepository.GenerateMovementNumberAsync(cancellationToken);
            var reversalMovement = new Domain.Entities.InventoryMovement
            {
                Id = Guid.NewGuid(),
                BranchId = firstExit.BranchId,
                MovementNumber = movementNumber,
                MovementType = MovementType.SaleReversal,
                FromBranchWarehouseId = null,
                ToBranchWarehouseId = warehouseId,
                ReferenceDocument = order.OrderNumber,
                Notes = $"Reversión por anulación de Pedido {order.OrderNumber}. Motivo: {request.CancellationReason}",
                MovementDate = DateTime.UtcNow,
                CreatedBy = _currentUserService.UserId ?? "System",
                CreatedOnUtc = DateTime.UtcNow
            };

            bool requiresMovement = false;

            foreach (var detail in order.Details)
            {
                var product = await _productRepository.GetByIdWithDetailsAsync(detail.ProductId, cancellationToken);
                if (product == null || product.ProductType == ProductType.Service || !product.TrackInventory)
                    continue;

                var presentation = product.Presentations?.FirstOrDefault(p => p.UnitOfMeasureId == detail.UnitOfMeasureId)
                    ?? product.Presentations?.FirstOrDefault();

                decimal conversionFactor = presentation?.ConversionFactor > 0 ? presentation.ConversionFactor : 1.0000m;
                Guid presentationId = presentation?.Id ?? Guid.Empty;
                decimal baseQtyToReturn = Math.Round(detail.Quantity * conversionFactor, 4);

                if (baseQtyToReturn > 0.0000m)
                {
                    var inventory = await _inventoryRepository.GetByWarehouseAndProductAsync(warehouseId, detail.ProductId, cancellationToken);
                    if (inventory == null)
                    {
                        inventory = new Domain.Entities.Inventory
                        {
                            Id = Guid.NewGuid(),
                            BranchWarehouseId = warehouseId,
                            ProductId = detail.ProductId,
                            PhysicalStock = baseQtyToReturn,
                            ReservedStock = 0,
                            CommittedStock = 0,
                            CreatedBy = _currentUserService.UserId ?? "System",
                            CreatedOnUtc = DateTime.UtcNow
                        };
                        await _inventoryRepository.AddAsync(inventory);
                    }
                    else
                    {
                        inventory.PhysicalStock += baseQtyToReturn;
                        _inventoryRepository.Update(inventory);
                    }

                    reversalMovement.Details.Add(new Domain.Entities.InventoryMovementDetail
                    {
                        Id = Guid.NewGuid(),
                        InventoryMovementId = reversalMovement.Id,
                        BranchId = firstExit.BranchId,
                        ProductId = detail.ProductId,
                        Quantity = detail.Quantity,
                        UnitOfMeasureId = detail.UnitOfMeasureId,
                        ProductPresentationId = presentationId,
                        ConversionFactor = conversionFactor,
                        QuantityInBaseUnit = baseQtyToReturn,
                        CreatedBy = _currentUserService.UserId ?? "System",
                        CreatedOnUtc = DateTime.UtcNow
                    });

                    requiresMovement = true;
                }
            }

            if (requiresMovement)
            {
                await _movementRepository.AddAsync(reversalMovement);
            }
        }

        order.Status = SalesOrderStatus.Anulado;
        order.Notes = string.IsNullOrEmpty(request.CancellationReason)
            ? order.Notes
            : $"{order.Notes}\n[ANULACIÓN]: {request.CancellationReason}";
        order.LastModifiedBy = _currentUserService.UserId ?? "System";
        order.LastModifiedOnUtc = DateTime.UtcNow;

        _salesOrderRepository.Update(order);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return Unit.Value;
    }
}
