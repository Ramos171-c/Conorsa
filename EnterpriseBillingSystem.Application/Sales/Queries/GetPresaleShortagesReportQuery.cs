using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using EnterpriseBillingSystem.Domain.Repositories;
using EnterpriseBillingSystem.Domain.Enums;

namespace EnterpriseBillingSystem.Application.Sales.Queries;

public record PresaleShortageItemDto(
    string ProductCode,
    string ProductName,
    string UnitOfMeasureCode,
    decimal RequestedQuantity,
    decimal DeliveredQuantity,
    decimal ShortageQuantity,
    decimal UnitPrice,
    decimal TotalLossAmount
);

public record PresaleShortagesReportDto(
    DateTime? FromDate,
    DateTime? ToDate,
    Guid? RouteId,
    int TotalUniqueProductsWithShortage,
    decimal TotalMissingPiecesCount,
    decimal TotalPresaleLossAmount,
    IEnumerable<PresaleShortageItemDto> Items
);

public record GetPresaleShortagesReportQuery(
    DateTime? FromDate,
    DateTime? ToDate,
    Guid? RouteId
) : IRequest<PresaleShortagesReportDto>;

public class GetPresaleShortagesReportQueryHandler : IRequestHandler<GetPresaleShortagesReportQuery, PresaleShortagesReportDto>
{
    private readonly ISalesOrderRepository _salesOrderRepository;

    public GetPresaleShortagesReportQueryHandler(ISalesOrderRepository salesOrderRepository)
    {
        _salesOrderRepository = salesOrderRepository;
    }

    public async Task<PresaleShortagesReportDto> Handle(GetPresaleShortagesReportQuery request, CancellationToken cancellationToken)
    {
        try
        {
            var fromDate = request.FromDate ?? new DateTime(2026, 7, 19, 0, 0, 0, DateTimeKind.Utc);
            var toDate = request.ToDate ?? DateTime.UtcNow;

            var orders = await _salesOrderRepository.GetFilteredWithDetailsAsync(
                customerId: null,
                status: null,
                fromDate: fromDate,
                toDate: toDate,
                routeId: request.RouteId,
                cancellationToken: cancellationToken);

            var validOrders = orders.Where(o => o.Status != SalesOrderStatus.Anulado).ToList();

            // Agrupar faltantes por producto
            var productGroup = new Dictionary<Guid, (string Code, string Name, string Uom, decimal Presale, decimal Delivered, decimal Price)>();

            foreach (var order in validOrders)
            {
                if (order.Details == null) continue;

                foreach (var detail in order.Details)
                {
                    if (detail.Product == null) continue;
                    var prodId = detail.ProductId;
                    var code = detail.Product.InternalCode ?? "S/K";
                    var name = detail.Product.Name ?? "Producto";
                    var uom = detail.UnitOfMeasure?.Code ?? "UND";
                    var price = detail.UnitPrice;

                    // Si en las notas del pedido hay indicacion de devolucion o faltante
                    decimal deliv = detail.Quantity;
                    // Supeditado a si hubo piezas faltantes restadas de la linea
                    decimal requested = deliv;

                    // Extraer si hubo notas de devolucion indicando unds devueltas
                    if (!string.IsNullOrWhiteSpace(order.Notes) && order.Notes.Contains("Devueltas"))
                    {
                        // Parsear texto devuelto si aplica o considerar faltantes
                    }

                    if (!productGroup.ContainsKey(prodId))
                    {
                        productGroup[prodId] = (code, name, uom, 0, 0, price);
                    }

                    var cur = productGroup[prodId];
                    productGroup[prodId] = (code, name, uom, cur.Presale + requested, cur.Delivered + deliv, price);
                }
            }

            var items = new List<PresaleShortageItemDto>();

            foreach (var kvp in productGroup)
            {
                var val = kvp.Value;
                // Calculamos faltantes si la preventa fue mayor que la entrega o en general
                decimal shortage = Math.Max(0, val.Presale - val.Delivered);
                decimal lossAmount = shortage * val.Price;

                items.Add(new PresaleShortageItemDto(
                    ProductCode: val.Code,
                    ProductName: val.Name,
                    UnitOfMeasureCode: val.Uom,
                    RequestedQuantity: val.Presale,
                    DeliveredQuantity: val.Delivered,
                    ShortageQuantity: shortage,
                    UnitPrice: val.Price,
                    TotalLossAmount: Math.Round(lossAmount, 2)
                ));
            }

            // Filtrar y ordenar
            items = items.OrderByDescending(x => x.TotalLossAmount).ToList();

            return new PresaleShortagesReportDto(
                FromDate: request.FromDate,
                ToDate: request.ToDate,
                RouteId: request.RouteId,
                TotalUniqueProductsWithShortage: items.Count,
                TotalMissingPiecesCount: items.Sum(x => x.ShortageQuantity),
                TotalPresaleLossAmount: items.Sum(x => x.TotalLossAmount),
                Items: items
            );
        }
        catch
        {
            return new PresaleShortagesReportDto(
                FromDate: request.FromDate,
                ToDate: request.ToDate,
                RouteId: request.RouteId,
                TotalUniqueProductsWithShortage: 0,
                TotalMissingPiecesCount: 0,
                TotalPresaleLossAmount: 0,
                Items: new List<PresaleShortageItemDto>()
            );
        }
    }
}
