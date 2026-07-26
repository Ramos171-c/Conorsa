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
            var sampleDetail = g.First();

            // 1. Identificar la presentación en CAJA/Empaque del proveedor para este producto
            var boxPresentation = sampleDetail.Product?.Presentations?.FirstOrDefault(p => p.ConversionFactor > 1 || string.Equals(p.UnitOfMeasure?.Code, "CAJA", StringComparison.OrdinalIgnoreCase));
            decimal boxFactor = boxPresentation?.ConversionFactor ?? 1.0000m;
            if (boxFactor <= 0) boxFactor = 1.0000m;

            string purchaseUnitName = boxPresentation?.UnitOfMeasure?.Code ?? (boxPresentation?.Name ?? (string.IsNullOrWhiteSpace(g.Key.Uom) ? "Caja" : g.Key.Uom));
            if (string.IsNullOrWhiteSpace(purchaseUnitName)) purchaseUnitName = "Caja";

            // 2. Calcular la Cantidad Total Pedida convertida exactamente a UNIDADES BASE
            decimal totalQuantityBaseUnits = g.Sum(x =>
            {
                var pFactor = x.Product?.Presentations?.FirstOrDefault(p => p.UnitOfMeasureId == x.UnitOfMeasureId)?.ConversionFactor ?? 1.0000m;
                if (pFactor <= 0) pFactor = 1.0000m;
                return x.Quantity * pFactor;
            });

            var grossSalesAmount = g.Sum(x => x.NetAmount);
            var unitPrice = totalQuantityBaseUnits > 0 ? grossSalesAmount / totalQuantityBaseUnits : 0m;
            var unitCost = boxPresentation?.Cost > 0 ? (boxPresentation.Cost / boxFactor) : (sampleDetail.Product?.CurrentCost ?? 0m);

            // 3. Obtener existencias disponibles en unidades base en la bodega
            if (!remainingBaseStock.TryGetValue(g.Key.ProductId, out decimal baseStockAvailable))
            {
                baseStockAvailable = 0m;
            }

            // 4. Deducir del inventario existente en unidades base
            var deductedBaseUnits = Math.Min(totalQuantityBaseUnits, baseStockAvailable);
            var netToOrderBaseUnits = Math.Max(0m, totalQuantityBaseUnits - deductedBaseUnits);

            // Actualizar stock restante del producto
            remainingBaseStock[g.Key.ProductId] = Math.Max(0m, baseStockAvailable - deductedBaseUnits);

            // 5. CÁLCULO EXACTO DEL PEDIDO EN CAJAS AL PROVEEDOR (Redondeo Superior CEILING en Cajas)
            // Ejemplo 1: 5.00 Cajas faltantes -> CEILING(5.00) = 5 CAJAS
            // Ejemplo 2: 4.71 Cajas faltantes -> CEILING(4.71) = 5 CAJAS
            // Ejemplo 3: 0.28 Cajas faltantes (10 unidades) -> CEILING(0.28) = 1 CAJA
            decimal netToOrderInBoxes = netToOrderBaseUnits / boxFactor;
            int suggestedBoxes = netToOrderBaseUnits > 0 ? (int)Math.Ceiling((double)netToOrderInBoxes) : 0;
            decimal suggestedTotalUnits = suggestedBoxes * boxFactor;
            decimal boxCost = unitCost * boxFactor;
            decimal suggestedPurchaseCost = suggestedBoxes * boxCost;

            // Cantidades para mostrar en la fila del consolidado en la UOM solicitada
            var sampleFactor = sampleDetail.Product?.Presentations?.FirstOrDefault(p => p.UnitOfMeasureId == sampleDetail.UnitOfMeasureId)?.ConversionFactor ?? 1.0000m;
            if (sampleFactor <= 0) sampleFactor = 1.0000m;

            var displayTotalQty = totalQuantityBaseUnits / sampleFactor;
            var displayDeducted = deductedBaseUnits / sampleFactor;
            var displayNetToOrder = netToOrderBaseUnits / sampleFactor;
            var displayAvailable = baseStockAvailable / sampleFactor;

            // Totales Financieros
            var grossPurchaseCost = totalQuantityBaseUnits * unitCost;
            var inventoryDeductedPurchaseCost = deductedBaseUnits * unitCost;
            var inventoryDeductedSalesAmount = deductedBaseUnits * unitPrice;
            var netPurchaseCost = netToOrderBaseUnits * unitCost;
            var netSalesAmount = netToOrderBaseUnits * unitPrice;
            var profitMarginAmount = netSalesAmount - netPurchaseCost;
            var profitMarginPercentage = netSalesAmount > 0 ? (profitMarginAmount / netSalesAmount) * 100m : 0m;

            // Recopilar Observaciones Válidas de Vendedores
            var sellerNotesList = orders
                .Where(o => o.Details.Any(d => d.ProductId == g.Key.ProductId) && !string.IsNullOrWhiteSpace(o.Notes))
                .Select(o => o.Notes!.Trim())
                .Where(n => !n.StartsWith("[SOLICITUD ANULACIÓN]", StringComparison.OrdinalIgnoreCase))
                .Distinct()
                .ToList();

            string sellerObs = sellerNotesList.Any() ? string.Join(" | ", sellerNotesList) : string.Empty;
            string supplierName = "Distribuidora Jenny";

            string obs;
            if (deductedBaseUnits >= totalQuantityBaseUnits)
            {
                obs = "Carga completa lista para entrega";
            }
            else if (deductedBaseUnits > 0)
            {
                obs = $"Stock parcial ({displayAvailable:F2} disp.). Se deducen {displayDeducted:F2} pzas. Pedir {displayNetToOrder:F2}";
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
                TotalQuantity: displayTotalQty,
                AvailableStock: displayAvailable,
                DeductedFromInventory: displayDeducted,
                NetQuantityToOrder: displayNetToOrder,
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
                PurchaseUnitName: purchaseUnitName,
                UnitsPerCase: boxFactor,
                SuggestedBoxesToOrder: suggestedBoxes,
                SuggestedTotalUnitsToOrder: netToOrderInBoxes, // Requeridas en Cajas exactas
                BoxCost: boxCost,
                SuggestedPurchaseCost: suggestedPurchaseCost,
                SellerObservations: sellerObs
            ));
        }

        return result.OrderBy(c => c.ProductName).ToList();
    }
}
