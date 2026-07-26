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
    string Observation = "",
    // Nuevos Campos para Pedido de Compra Sugerido (Empaques, Redondeo Superior CEILING y Proveedor)
    string SupplierName = "Distribuidora Jenny",
    string PurchaseUnitName = "Caja",
    decimal UnitsPerCase = 1.00m,
    int SuggestedBoxesToOrder = 0,
    decimal SuggestedTotalUnitsToOrder = 0m,
    decimal BoxCost = 0m,
    decimal SuggestedPurchaseCost = 0m,
    string SellerObservations = ""
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
            .GroupBy(d => new 
            { 
                d.ProductId, 
                Code = d.Product?.InternalCode ?? string.Empty, 
                Name = !string.IsNullOrWhiteSpace(d.Product?.Description) 
                    ? d.Product.Description 
                    : (d.Product?.Name ?? "Producto Desconocido"), 
                Uom = d.UnitOfMeasure != null ? d.UnitOfMeasure.Code : "UND"
            })
            .ToList();

        // Optimización N+1: Consultar todo el inventario disponible en 1 sola consulta SQL por lotes
        var productIds = detailsGrouped.Select(g => g.Key.ProductId).Distinct().ToList();
        var stockDict = await _inventoryRepository.GetAvailableStockByProductIdsAsync(productIds, cancellationToken);
        var remainingBaseStock = new Dictionary<Guid, decimal>(stockDict);

        var result = new List<ConsolidatedProductDto>();

        foreach (var g in detailsGrouped)
        {
            var totalQuantity = g.Sum(x => x.Quantity);
            var grossSalesAmount = g.Sum(x => x.NetAmount);
            var unitPrice = totalQuantity > 0 ? grossSalesAmount / totalQuantity : 0m;

            var sampleDetail = g.First();
            var presentation = sampleDetail.Product?.Presentations?.FirstOrDefault(p => p.UnitOfMeasureId == sampleDetail.UnitOfMeasureId);
            var conversionFactor = presentation?.ConversionFactor ?? 1.0000m;
            if (conversionFactor <= 0) conversionFactor = 1.0000m;

            var unitCost = presentation != null ? presentation.Cost : (sampleDetail.Product?.CurrentCost ?? 0m);

            // Obtener existencias disponibles restantes en la bodega única
            if (!remainingBaseStock.TryGetValue(g.Key.ProductId, out decimal baseStockAvailable))
            {
                baseStockAvailable = 0m;
            }

            // Convertir stock disponible de unidades base a la presentación actual
            var availableInPresUnits = Math.Max(0m, baseStockAvailable / conversionFactor);

            // Deducir del inventario existente real en unidades de presentación
            var deducted = Math.Min(totalQuantity, availableInPresUnits);
            var netToOrder = Math.Max(0m, totalQuantity - deducted);

            // Descontar del fondo global de stock del producto en unidades base
            var deductedBase = deducted * conversionFactor;
            remainingBaseStock[g.Key.ProductId] = Math.Max(0m, baseStockAvailable - deductedBase);

            // 1. Totales Brutos Solicitados por Pedidos
            var grossPurchaseCost = totalQuantity * unitCost;

            // 2. Valores Cubiertos por Inventario Existente
            var inventoryDeductedPurchaseCost = deducted * unitCost;
            var inventoryDeductedSalesAmount = deducted * unitPrice;

            // 3. Totales Netos a Pedir al Proveedor
            var netPurchaseCost = netToOrder * unitCost;
            var netSalesAmount = netToOrder * unitPrice;

            // 4. Diferencia / Ganancia bruta estimada
            var profitMarginAmount = netSalesAmount - netPurchaseCost;
            var profitMarginPercentage = netSalesAmount > 0 ? (profitMarginAmount / netSalesAmount) * 100m : 0m;

            // 5. Cálculo del Pedido de Compra Sugerido (Fórmula CEILING Redondeo Superior de Cajas Completas)
            // Ejemplo: 97 unids / 24 por caja = CEILING(4.04) = 5 cajas
            int suggestedBoxes = netToOrder > 0 ? (int)Math.Ceiling((double)(netToOrder / (conversionFactor > 0 ? conversionFactor : 1m))) : 0;
            decimal suggestedTotalUnits = suggestedBoxes * conversionFactor;
            decimal boxCost = unitCost * conversionFactor;
            decimal suggestedPurchaseCost = suggestedBoxes * boxCost;

            // 6. Recopilar Observaciones Válidas de Vendedores para este producto
            var sellerNotesList = orders
                .Where(o => o.Details.Any(d => d.ProductId == g.Key.ProductId) && !string.IsNullOrWhiteSpace(o.Notes))
                .Select(o => o.Notes!.Trim())
                .Where(n => !n.StartsWith("[SOLICITUD ANULACIÓN]", StringComparison.OrdinalIgnoreCase))
                .Distinct()
                .ToList();

            string sellerObs = sellerNotesList.Any() ? string.Join(" | ", sellerNotesList) : string.Empty;

            string supplierName = "Distribuidora Jenny";

            string obs;
            if (deducted >= totalQuantity)
            {
                obs = "Carga completa lista para entrega";
            }
            else if (deducted > 0)
            {
                obs = $"Stock parcial ({availableInPresUnits:F2} disp.). Se deducen {deducted:F2} pzas. Pedir {netToOrder:F2}";
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
                AvailableStock: availableInPresUnits,
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
                Observation: obs,
                SupplierName: supplierName,
                PurchaseUnitName: string.IsNullOrWhiteSpace(g.Key.Uom) ? "Caja" : g.Key.Uom,
                UnitsPerCase: conversionFactor,
                SuggestedBoxesToOrder: suggestedBoxes,
                SuggestedTotalUnitsToOrder: suggestedTotalUnits,
                BoxCost: boxCost,
                SuggestedPurchaseCost: suggestedPurchaseCost,
                SellerObservations: sellerObs
            ));
        }

        return result.OrderBy(c => c.ProductName).ToList();
    }
}
