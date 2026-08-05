using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using EnterpriseBillingSystem.Domain.Entities;
using EnterpriseBillingSystem.Domain.Enums;
using EnterpriseBillingSystem.Domain.Repositories;
using DomainInventory = EnterpriseBillingSystem.Domain.Entities.Inventory;

namespace EnterpriseBillingSystem.Application.Sales.Commands;

public record CreateRouteLiquidationDetailDto(
    Guid ProductId,
    Guid UnitOfMeasureId,
    Guid? ProductPresentationId,
    decimal QuantitySent,
    decimal QuantityReturned,
    decimal SalePrice,
    decimal Cost,
    string? Notes = null
);

public record CreateRouteLiquidationCommand(
    Guid RouteId,
    string? Observations,
    List<CreateRouteLiquidationDetailDto> Details
) : IRequest<Guid>;

public class CreateRouteLiquidationCommandHandler : IRequestHandler<CreateRouteLiquidationCommand, Guid>
{
    private readonly IRouteLiquidationRepository _liquidationRepository;
    private readonly IInventoryRepository _inventoryRepository;
    private readonly IInventoryMovementRepository _movementRepository;
    private readonly IWarehouseRepository _warehouseRepository;
    private readonly IProductRepository _productRepository;
    private readonly ISalesOrderRepository _salesOrderRepository;
    private readonly IUnitOfWork _unitOfWork;

    public CreateRouteLiquidationCommandHandler(
        IRouteLiquidationRepository liquidationRepository,
        IInventoryRepository inventoryRepository,
        IInventoryMovementRepository movementRepository,
        IWarehouseRepository warehouseRepository,
        IProductRepository productRepository,
        ISalesOrderRepository salesOrderRepository,
        IUnitOfWork unitOfWork)
    {
        _liquidationRepository = liquidationRepository;
        _inventoryRepository = inventoryRepository;
        _movementRepository = movementRepository;
        _warehouseRepository = warehouseRepository;
        _productRepository = productRepository;
        _salesOrderRepository = salesOrderRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<Guid> Handle(CreateRouteLiquidationCommand request, CancellationToken cancellationToken)
    {
        if (request.Details == null || !request.Details.Any())
        {
            throw new InvalidOperationException("La liquidación debe contener al menos un producto.");
        }

        var liquidationNumber = await _liquidationRepository.GenerateNextLiquidationNumberAsync(cancellationToken);

        var liquidation = new RouteLiquidation
        {
            Id = Guid.NewGuid(),
            LiquidationNumber = liquidationNumber,
            RouteId = request.RouteId,
            LiquidationDate = DateTime.UtcNow,
            Status = RouteLiquidationStatus.Confirmada,
            Observations = request.Observations,
            CreatedBy = "System",
            CreatedOnUtc = DateTime.UtcNow
        };

        // Buscar Bodega Exhibición para reingreso de retorno de inventario
        var allWarehouses = await _warehouseRepository.GetAllAsync();
        var targetWarehouse = allWarehouses.FirstOrDefault(w => w.Name.Contains("Exhibici")) ?? allWarehouses.FirstOrDefault();

        BranchWarehouse? branchWarehouse = null;
        if (targetWarehouse != null)
        {
            branchWarehouse = targetWarehouse.BranchWarehouses.FirstOrDefault();
        }

        InventoryMovement? returnMovement = null;
        if (branchWarehouse != null)
        {
            var movNumber = $"MOV-RET-{DateTime.UtcNow:yyyyMMddHHmmss}-{new Random().Next(100, 999)}";
            returnMovement = new InventoryMovement
            {
                Id = Guid.NewGuid(),
                MovementNumber = movNumber,
                MovementType = MovementType.Entry,
                ToBranchWarehouseId = branchWarehouse.Id,
                ReferenceDocument = liquidationNumber,
                Notes = $"Retorno de Inventario por Liquidación de Ruta {liquidationNumber}",
                MovementDate = DateTime.UtcNow,
                CreatedBy = "System",
                CreatedOnUtc = DateTime.UtcNow
            };
        }

        decimal totalSent = 0;
        decimal totalReturned = 0;
        decimal totalSold = 0;
        decimal totalAmountSold = 0;
        decimal totalAmountReturned = 0;
        decimal totalCostSold = 0;

        foreach (var d in request.Details)
        {
            if (d.QuantityReturned < 0)
            {
                throw new InvalidOperationException("La cantidad retornada no puede ser negativa.");
            }
            if (d.QuantityReturned > d.QuantitySent)
            {
                throw new InvalidOperationException($"La cantidad retornada ({d.QuantityReturned}) no puede superar la cantidad enviada ({d.QuantitySent}).");
            }

            var product = await _productRepository.GetByIdWithDetailsAsync(d.ProductId, cancellationToken);
            var presentation = product?.Presentations?.FirstOrDefault(p => p.Id == d.ProductPresentationId || p.UnitOfMeasureId == d.UnitOfMeasureId);
            decimal conversionFactor = presentation?.ConversionFactor ?? 1.0000m;
            if (conversionFactor <= 0) conversionFactor = 1.0000m;

            decimal qtySold = d.QuantitySent - d.QuantityReturned;
            decimal baseSent = d.QuantitySent * conversionFactor;
            decimal baseReturned = d.QuantityReturned * conversionFactor;
            decimal baseSold = qtySold * conversionFactor;

            decimal subtotalSold = qtySold * d.SalePrice;
            decimal subtotalReturned = d.QuantityReturned * d.SalePrice;
            decimal lineCostSold = qtySold * d.Cost;

            totalSent += d.QuantitySent;
            totalReturned += d.QuantityReturned;
            totalSold += qtySold;
            totalAmountSold += subtotalSold;
            totalAmountReturned += subtotalReturned;
            totalCostSold += lineCostSold;

            var detailEntity = new RouteLiquidationDetail
            {
                Id = Guid.NewGuid(),
                RouteLiquidationId = liquidation.Id,
                ProductId = d.ProductId,
                UnitOfMeasureId = d.UnitOfMeasureId,
                ProductPresentationId = d.ProductPresentationId,
                QuantitySent = d.QuantitySent,
                QuantityReturned = d.QuantityReturned,
                QuantitySold = qtySold,
                BaseQuantitySent = baseSent,
                BaseQuantityReturned = baseReturned,
                BaseQuantitySold = baseSold,
                SalePrice = d.SalePrice,
                Cost = d.Cost,
                SubtotalSold = subtotalSold,
                SubtotalReturned = subtotalReturned,
                Notes = d.Notes
            };

            liquidation.Details.Add(detailEntity);

            // Si hay unidades retornadas > 0, reingresar stock físico en la bodega única
            if (baseReturned > 0 && branchWarehouse != null)
            {
                var invRecord = await _inventoryRepository.GetByWarehouseAndProductAsync(branchWarehouse.Id, d.ProductId, cancellationToken);
                if (invRecord != null)
                {
                    invRecord.PhysicalStock += baseReturned;
                    invRecord.LastModifiedBy = "System";
                    invRecord.LastModifiedOnUtc = DateTime.UtcNow;
                }
                else
                {
                    var newInv = new DomainInventory
                    {
                        Id = Guid.NewGuid(),
                        BranchWarehouseId = branchWarehouse.Id,
                        ProductId = d.ProductId,
                        PhysicalStock = baseReturned,
                        ReservedStock = 0,
                        CommittedStock = 0,
                        CreatedBy = "System",
                        CreatedOnUtc = DateTime.UtcNow
                    };
                    await _inventoryRepository.AddAsync(newInv);
                }

                if (returnMovement != null)
                {
                    var uomId = d.UnitOfMeasureId != Guid.Empty ? d.UnitOfMeasureId : (product?.DefaultUnitOfMeasureId ?? Guid.Empty);
                    returnMovement.Details.Add(new InventoryMovementDetail
                    {
                        Id = Guid.NewGuid(),
                        InventoryMovementId = returnMovement.Id,
                        ProductId = d.ProductId,
                        UnitOfMeasureId = uomId,
                        ProductPresentationId = presentation?.Id ?? Guid.Empty,
                        Quantity = d.QuantityReturned,
                        ConversionFactor = conversionFactor,
                        QuantityInBaseUnit = baseReturned
                    });
                }
            }
        }

        // Actualizar pedidos y facturas de la ruta para restar la devolución de la venta y agregar comentarios
        var routeOrders = await _salesOrderRepository.GetFilteredWithDetailsAsync(
            customerId: null,
            status: null,
            fromDate: null,
            toDate: null,
            routeId: request.RouteId,
            cancellationToken: cancellationToken);

        var activeOrders = routeOrders.Where(o => o.Status != SalesOrderStatus.Anulado).ToList();

        foreach (var d in request.Details.Where(x => x.QuantityReturned > 0))
        {
            decimal remainingToDeduct = d.QuantityReturned;

            foreach (var order in activeOrders)
            {
                if (remainingToDeduct <= 0) break;

                var orderDetail = order.Details.FirstOrDefault(dt => dt.ProductId == d.ProductId && dt.Quantity > 0);
                if (orderDetail != null)
                {
                    decimal deductQty = Math.Min(remainingToDeduct, orderDetail.Quantity);
                    orderDetail.Quantity -= deductQty;
                    remainingToDeduct -= deductQty;

                    // Recalcular detalle
                    var grossAmount = orderDetail.Quantity * orderDetail.UnitPrice;
                    orderDetail.DiscountAmount = grossAmount * (orderDetail.DiscountPercentage / 100);
                    var taxableAmount = grossAmount - orderDetail.DiscountAmount;
                    orderDetail.TaxAmount = taxableAmount * (orderDetail.TaxPercentage / 100);
                    orderDetail.NetAmount = taxableAmount + orderDetail.TaxAmount;

                    // Recalcular totales del pedido
                    order.SubTotal = order.Details.Sum(dt => dt.Quantity * dt.UnitPrice);
                    order.DiscountAmount = order.Details.Sum(dt => dt.Quantity * dt.UnitPrice * (dt.DiscountPercentage / 100));
                    order.TaxAmount = order.Details.Sum(dt => dt.TaxAmount);
                    order.TotalAmount = order.SubTotal - order.DiscountAmount + order.TaxAmount;

                    if (order.TotalAmount <= 0)
                    {
                        order.Status = SalesOrderStatus.Anulado;
                    }
                    else
                    {
                        order.Status = SalesOrderStatus.Completado;
                    }

                    var timeStr = DateTime.UtcNow.AddHours(-6).ToString("dd/MM/yyyy HH:mm");
                    var obsText = !string.IsNullOrWhiteSpace(request.Observations) ? $" Observaciones: {request.Observations}." : "";
                    var prodName = orderDetail.Product?.Name ?? "Producto";
                    order.Notes = $"{order.Notes}\n[LIQUIDACIÓN RUTA {liquidationNumber}]: Se procesó devolución de {deductQty} und de '{prodName}' el {timeStr}. Venta restada.{obsText}".Trim();
                }
            }
        }

        // Marcar el resto de pedidos activos de la ruta como completados si no se ingresaron devoluciones para ellos
        foreach (var order in activeOrders.Where(o => o.Status == SalesOrderStatus.EnCamino || o.Status == SalesOrderStatus.EnProceso))
        {
            order.Status = SalesOrderStatus.Completado;
            var timeStr = DateTime.UtcNow.AddHours(-6).ToString("dd/MM/yyyy HH:mm");
            order.Notes = $"{order.Notes}\n[LIQUIDACIÓN RUTA {liquidationNumber}]: Pedido entregado y liquidado el {timeStr}.".Trim();
        }

        liquidation.TotalQuantitySent = totalSent;
        liquidation.TotalQuantityReturned = totalReturned;
        liquidation.TotalQuantitySold = totalSold;
        liquidation.TotalAmountSold = totalAmountSold;
        liquidation.TotalAmountReturned = totalAmountReturned;
        liquidation.TotalCostSold = totalCostSold;
        liquidation.EstimatedProfit = totalAmountSold - totalCostSold;

        await _liquidationRepository.AddAsync(liquidation);

        if (returnMovement != null && returnMovement.Details.Any())
        {
            await _movementRepository.AddAsync(returnMovement);
        }

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return liquidation.Id;
    }
}
