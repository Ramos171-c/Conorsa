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

        var result = new List<ConsolidatedProductDto>();

        foreach (var g in detailsGrouped)
        {
            var totalQuantity = g.Sum(x => x.Quantity);
            var totalNetAmount = g.Sum(x => x.NetAmount);

            // Consultar existencias disponibles en la bodega única de la empresa para este producto
            var (stockItems, _) = await _inventoryRepository.GetStockInquiryAsync(null, g.Key.ProductId, 1, 100, cancellationToken);
            var availableStock = Math.Max(0, stockItems.Sum(i => i.PhysicalStock - i.ReservedStock - i.CommittedStock));

            // Deducir del inventario existente para no volver a pedirlo
            var deducted = Math.Min(totalQuantity, availableStock);
            var netToOrder = Math.Max(0, totalQuantity - availableStock);

            var sampleDetail = g.First();
            var presentation = sampleDetail.Product?.Presentations?.FirstOrDefault(p => p.UnitOfMeasureId == sampleDetail.UnitOfMeasureId);
            var unitCost = presentation != null ? presentation.Cost : (sampleDetail.Product?.CurrentCost ?? 0m);

            // El costo total estimado se calcula sobre lo que realmente se debe pedir (neto)
            var totalCost = netToOrder * unitCost;

            string obs;
            if (availableStock >= totalQuantity)
            {
                obs = "Stock suficiente en inventario (No pedir)";
            }
            else if (availableStock > 0)
            {
                obs = $"Stock parcial en inventario ({availableStock:F2} disp.). Se deducen {deducted:F2} pzas. Pedir {netToOrder:F2}";
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
                TotalNetAmount: totalNetAmount,
                TotalCost: totalCost,
                Observation: obs
            ));
        }

        return result.OrderBy(c => c.ProductName).ToList();
    }
}
