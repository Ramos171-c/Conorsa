using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using EnterpriseBillingSystem.Domain.Repositories;

namespace EnterpriseBillingSystem.Application.Sales.Queries;

public record ConsolidatedProductDto(
    Guid ProductId,
    string ProductCode,
    string ProductName,
    string UnitOfMeasure,
    decimal TotalQuantity,
    decimal AvailableStock,
    decimal DeductedFromInventory,
    decimal NetQuantityToOrder,
    decimal UnitCost,
    decimal UnitPrice,
    decimal GrossPurchaseCost,
    decimal GrossSalesAmount,
    decimal InventoryDeductedPurchaseCost,
    decimal InventoryDeductedSalesAmount,
    decimal TotalPurchaseCost,
    decimal NetSalesAmount,
    decimal ProfitMarginAmount,
    decimal ProfitMarginPercentage,
    decimal TotalNetAmount,
    decimal TotalCost,
    string Observation = ""
);

public record GetSalesOrderConsolidatedProductsQuery(
    Guid? CustomerId,
    string? Status,
    DateTime? FromDate,
    DateTime? ToDate,
    Guid? RouteId = null
) : IRequest<IEnumerable<ConsolidatedProductDto>>;

public class GetSalesOrderConsolidatedProductsQueryHandler : IRequestHandler<GetSalesOrderConsolidatedProductsQuery, IEnumerable<ConsolidatedProductDto>>
{
    private readonly ISalesOrderRepository _repository;
    private readonly IInventoryRepository _inventoryRepository;

    public GetSalesOrderConsolidatedProductsQueryHandler(
        ISalesOrderRepository repository,
        IInventoryRepository inventoryRepository)
    {
        _repository = repository;
        _inventoryRepository = inventoryRepository;
    }

    public async Task<IEnumerable<ConsolidatedProductDto>> Handle(GetSalesOrderConsolidatedProductsQuery request, CancellationToken cancellationToken)
    {
        var orders = await _repository.GetFilteredWithDetailsAsync(
            request.CustomerId, request.Status, request.FromDate, request.ToDate, request.RouteId, cancellationToken);

        var detailsGrouped = orders
            .SelectMany(o => o.Details)
            .GroupBy(d => new { d.ProductId, Code = d.Product?.InternalCode ?? string.Empty, Name = d.Product?.Name ?? "Producto Desconocido", Uom = d.UnitOfMeasure?.Code ?? string.Empty })
            .ToList();

        // Optimización N+1: Consultar todo el inventario disponible en 1 sola consulta SQL por lotes
        var productIds = detailsGrouped.Select(g => g.Key.ProductId).ToList();
        var stockDict = await _inventoryRepository.GetAvailableStockByProductIdsAsync(productIds, cancellationToken);

        var result = new List<ConsolidatedProductDto>();

        foreach (var g in detailsGrouped)
        {
            var totalQuantity = g.Sum(x => x.Quantity);
            var grossSalesAmount = g.Sum(x => x.NetAmount);
            var unitPrice = totalQuantity > 0 ? grossSalesAmount / totalQuantity : 0m;

            // Obtener existencias disponibles desde el diccionario en memoria (O(1))
            stockDict.TryGetValue(g.Key.ProductId, out decimal availableStock);

            // Deducir del inventario existente para no volver a pedirlo
            var deducted = Math.Min(totalQuantity, availableStock);
            var netToOrder = Math.Max(0, totalQuantity - availableStock);

            var sampleDetail = g.First();
            var presentation = sampleDetail.Product?.Presentations?.FirstOrDefault(p => p.UnitOfMeasureId == sampleDetail.UnitOfMeasureId);
            var unitCost = presentation != null ? presentation.Cost : (sampleDetail.Product?.CurrentCost ?? 0m);

            // 1. Totales Brutos Solicitados por Pedidos
            var grossPurchaseCost = totalQuantity * unitCost;

            // 2. Valores Cubiertos por Inventario Existente (para tener la información de inventario en el reporte)
            var inventoryDeductedPurchaseCost = deducted * unitCost;
            var inventoryDeductedSalesAmount = deducted * unitPrice;

            // 3. Totales Netos a Pedir al Proveedor
            var netPurchaseCost = netToOrder * unitCost;
            var netSalesAmount = netToOrder * unitPrice;

            // 4. Diferencia / Ganancia bruta estimada (Neto Venta - Neto Compra)
            var profitMarginAmount = netSalesAmount - netPurchaseCost;
            var profitMarginPercentage = netSalesAmount > 0 ? (profitMarginAmount / netSalesAmount) * 100m : 0m;

            string obs;
            if (availableStock >= totalQuantity)
            {
                obs = "Stock suficiente en inventario (No pedir)";
            }
            else if (availableStock > 0)
            {
                obs = $"Stock parcial ({availableStock:F2} disp.). Se deducen {deducted:F2} pzas. Pedir {netToOrder:F2}";
            }
            else
            {
                obs = "Sin stock en inventario. Pedir completo";
            }

            result.Add(new ConsolidatedProductDto(
                ProductId: g.Key.ProductId,
                ProductCode: g.Key.Code,
                ProductName: g.Key.Name,
                UnitOfMeasure: g.Key.Uom,
                TotalQuantity: totalQuantity,
                AvailableStock: availableStock,
                DeductedFromInventory: deducted,
                NetQuantityToOrder: netToOrder,
                UnitCost: unitCost,
                UnitPrice: unitPrice,
                GrossPurchaseCost: grossPurchaseCost,
                GrossSalesAmount: grossSalesAmount,
                InventoryDeductedPurchaseCost: inventoryDeductedPurchaseCost,
                InventoryDeductedSalesAmount: inventoryDeductedSalesAmount,
                TotalPurchaseCost: netPurchaseCost,
                NetSalesAmount: netSalesAmount,
                ProfitMarginAmount: profitMarginAmount,
                ProfitMarginPercentage: profitMarginPercentage,
                TotalNetAmount: grossSalesAmount,
                TotalCost: netPurchaseCost,
                Observation: obs
            ));
        }

        return result.OrderBy(c => c.ProductName).ToList();
    }
}
