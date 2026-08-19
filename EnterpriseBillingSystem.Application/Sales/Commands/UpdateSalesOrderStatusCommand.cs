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
    private readonly IWarehouseRepository _warehouseRepository;
    private readonly ICurrentUserService _currentUserService;
    private readonly IUnitOfWork _unitOfWork;

    public UpdateSalesOrderStatusCommandHandler(
        ISalesOrderRepository salesOrderRepository,
        IInventoryRepository inventoryRepository,
        IInventoryMovementRepository movementRepository,
        IProductRepository productRepository,
        IRepository<BranchWarehouse> branchWarehouseRepository,
        IWarehouseRepository warehouseRepository,
        ICurrentUserService currentUserService,
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

    public async Task<Unit> Handle(UpdateSalesOrderStatusCommand request, CancellationToken cancellationToken)
    {
        var order = await _salesOrderRepository.GetByIdWithDetailsAsync(request.SalesOrderId, cancellationToken);
        if (order == null)
            throw new ArgumentException($"El pedido con Id '{request.SalesOrderId}' no existe.");

        // Deduct from inventory ONLY when transitioning to "EnCamino" for the first time
        if (request.Status == SalesOrderStatus.EnCamino && order.Status != SalesOrderStatus.EnCamino)
        {
            // Idempotency check: Verify if an Exit movement already exists for this order
            var existingMovements = await _movementRepository.FindAsync(m => m.ReferenceDocument == order.OrderNumber && m.MovementType == MovementType.Exit);
            if (!existingMovements.Any())
            {
                // Explicitly search for "Bodega Exhibición" by querying Warehouse table
                BranchWarehouse? warehouse = null;
                var targetWarehouses = await _warehouseRepository.FindAsync(w => w.Name.Contains("Exhibici"));
                var targetWarehouse = targetWarehouses.FirstOrDefault();

                if (targetWarehouse != null)
                {
                    var bws = await _branchWarehouseRepository.FindAsync(bw => bw.WarehouseId == targetWarehouse.Id && bw.IsActive);
                    warehouse = bws.FirstOrDefault();
                }

                if (warehouse == null)
                {
                    var allActive = await _branchWarehouseRepository.FindAsync(bw => bw.IsActive);
                    warehouse = allActive.FirstOrDefault(bw => bw.IsDefault) ?? allActive.FirstOrDefault();
                }

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

                foreach (var detail in order.Details)
                {
                    var product = await _productRepository.GetByIdWithDetailsAsync(detail.ProductId, cancellationToken);
                    if (product == null)
                        continue;

                    // If service or inventory tracking disabled, skip
                    if (product.ProductType == ProductType.Service || !product.TrackInventory)
                        continue;

                    // Find corresponding presentation
                    var presentation = product.Presentations?.FirstOrDefault(p => p.UnitOfMeasureId == detail.UnitOfMeasureId)
                        ?? product.Presentations?.FirstOrDefault();

                    if (presentation == null)
                    {
                        presentation = new ProductPresentation
                        {
                            Id = Guid.NewGuid(),
                            ProductId = product.Id,
                            Name = product.Name,
                            UnitOfMeasureId = detail.UnitOfMeasureId,
                            ConversionFactor = 1.0000m,
                            Cost = product.CurrentCost,
                            RetailPrice = detail.UnitPrice,
                            IsBaseUnit = true,
                            IsDefaultSalePresentation = true,
                            IsActive = true
                        };
                        if (product.Presentations == null) product.Presentations = new List<ProductPresentation>();
                        product.Presentations.Add(presentation);
                    }

                    decimal conversionFactor = presentation.ConversionFactor > 0 ? presentation.ConversionFactor : 1.0000m;
                    Guid presentationId = presentation.Id;
                    
                    // Get inventory record in warehouse
                    var inventory = await _inventoryRepository.GetByWarehouseAndProductAsync(warehouse.Id, detail.ProductId, cancellationToken);
                    
                    // Back up the original presale quantity requested
                    if (detail.OriginalPresaleQuantity == null)
                    {
                        detail.OriginalPresaleQuantity = detail.Quantity;
                    }

                    decimal requestedInBaseUnit = Math.Round(detail.Quantity * conversionFactor, 4);
                    decimal availableBaseStock = Math.Max(0m, inventory?.PhysicalStock ?? 0m);

                    // AUTOMATIC DEDUCTION: Only deliver what ACTUALLY EXISTS in inventory
                    decimal deliveredInBaseUnit = Math.Min(requestedInBaseUnit, availableBaseStock);
                    decimal deliveredQuantity = conversionFactor > 0 ? Math.Round(deliveredInBaseUnit / conversionFactor, 4) : 0m;
                    decimal missingQuantity = Math.Max(0m, detail.Quantity - deliveredQuantity);

                    if (missingQuantity > 0)
                    {
                        var uomName = presentation.Name ?? "UND";
                        var missingMsg = $"{product.Name}: Pedido={detail.Quantity:N2}, Entregado={deliveredQuantity:N2}, Faltante={missingQuantity:N2} {uomName} (Sin stock en bodega)";
                        if (string.IsNullOrWhiteSpace(order.Notes))
                            order.Notes = $"[FALTANTE POR STOCK]: {missingMsg}";
                        else if (!order.Notes.Contains(product.Name))
                            order.Notes += $"\n[FALTANTE POR STOCK]: {missingMsg}";
                    }

                    // Update detail quantity to reflect ONLY the delivered units
                    detail.Quantity = deliveredQuantity;
                    detail.DiscountAmount = detail.DiscountPercentage > 0 ? (detail.Quantity * detail.UnitPrice * (detail.DiscountPercentage / 100m)) : 0m;
                    detail.TaxAmount = detail.TaxPercentage > 0 ? ((detail.Quantity * detail.UnitPrice - detail.DiscountAmount) * (detail.TaxPercentage / 100m)) : 0m;
                    detail.NetAmount = (detail.Quantity * detail.UnitPrice) - detail.DiscountAmount + detail.TaxAmount;

                    if (deliveredInBaseUnit > 0.0000m)
                    {
                        if (inventory != null)
                        {
                            inventory.PhysicalStock -= deliveredInBaseUnit;
                            _inventoryRepository.Update(inventory);
                        }

                        movement.Details.Add(new InventoryMovementDetail
                        {
                            Id = Guid.NewGuid(),
                            InventoryMovementId = movement.Id,
                            BranchId = warehouse.BranchId,
                            ProductId = detail.ProductId,
                            Quantity = deliveredQuantity,
                            UnitOfMeasureId = detail.UnitOfMeasureId,
                            ProductPresentationId = presentationId,
                            ConversionFactor = conversionFactor,
                            QuantityInBaseUnit = deliveredInBaseUnit,
                            CreatedBy = _currentUserService.UserId ?? "System",
                            CreatedOnUtc = DateTime.UtcNow
                        });

                        requiresMovement = true;
                    }
                }

                if (requiresMovement)
                {
                    await _movementRepository.AddAsync(movement);
                }
            }

            // Recalculate order totals based ONLY on delivered quantities
            order.SubTotal = order.Details.Sum(d => d.Quantity * d.UnitPrice);
            order.DiscountAmount = order.Details.Sum(d => d.DiscountAmount);
            order.TaxAmount = order.Details.Sum(d => d.TaxAmount);
            order.TotalAmount = order.Details.Sum(d => d.NetAmount);
        }

        order.Status = request.Status;
        order.LastModifiedBy = _currentUserService.UserId ?? "System";
        order.LastModifiedOnUtc = DateTime.UtcNow;

        _salesOrderRepository.Update(order);

        try
        {
            await _unitOfWork.SaveChangesAsync(cancellationToken);
        }
        catch (Microsoft.EntityFrameworkCore.DbUpdateConcurrencyException ex)
        {
            foreach (var entry in ex.Entries)
            {
                var databaseValues = await entry.GetDatabaseValuesAsync(cancellationToken);
                if (databaseValues == null)
                {
                    entry.State = Microsoft.EntityFrameworkCore.EntityState.Detached;
                }
                else
                {
                    entry.OriginalValues.SetValues(databaseValues);
                }
            }
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
