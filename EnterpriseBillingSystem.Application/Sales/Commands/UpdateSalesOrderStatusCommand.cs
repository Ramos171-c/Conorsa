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
                    // Get inventory record in Bodega Exhibición
                    var inventory = await _inventoryRepository.GetByWarehouseAndProductAsync(warehouse.Id, detail.ProductId, cancellationToken);
                    decimal availableInBaseUnit = inventory != null ? Math.Max(0.0000m, inventory.PhysicalStock) : 0.0000m;

                    // Back up the original presale quantity requested
                    if (detail.OriginalPresaleQuantity == null)
                    {
                        detail.OriginalPresaleQuantity = detail.Quantity;
                    }

                    decimal requestedInBaseUnit = Math.Round(detail.Quantity * conversionFactor, 4);

                    // Check if stock is insufficient and adjust quantity
                    if (requestedInBaseUnit > availableInBaseUnit)
                    {
                        decimal fulfillableInBaseUnit = availableInBaseUnit;
                        detail.Quantity = Math.Round(fulfillableInBaseUnit / conversionFactor, 4);

                        // Recalculate line amounts
                        var discountPercentage = detail.DiscountPercentage;
                        var taxPercentage = detail.TaxPercentage;
                        
                        var grossAmount = detail.Quantity * detail.UnitPrice;
                        detail.DiscountAmount = Math.Round(grossAmount * (discountPercentage / 100m), 4);
                        var taxableAmount = grossAmount - detail.DiscountAmount;
                        detail.TaxAmount = Math.Round(taxableAmount * (taxPercentage / 100m), 4);
                        detail.NetAmount = Math.Round(taxableAmount + detail.TaxAmount, 4);

                        requestedInBaseUnit = Math.Round(detail.Quantity * conversionFactor, 4);
                    }

                    if (requestedInBaseUnit > 0.0000m)
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

                        inventory.PhysicalStock -= requestedInBaseUnit;
                        _inventoryRepository.Update(inventory);

                        movement.Details.Add(new InventoryMovementDetail
                        {
                            Id = Guid.NewGuid(),
                            InventoryMovementId = movement.Id,
                            BranchId = warehouse.BranchId,
                            ProductId = detail.ProductId,
                            Quantity = detail.Quantity,
                            UnitOfMeasureId = detail.UnitOfMeasureId,
                            ProductPresentationId = presentationId,
                            ConversionFactor = conversionFactor,
                            QuantityInBaseUnit = requestedInBaseUnit,
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
