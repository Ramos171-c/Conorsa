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
    private readonly IRouteLiquidationRepository _liquidationRepository;

    public GetPresaleShortagesReportQueryHandler(
        ISalesOrderRepository salesOrderRepository,
        IRouteLiquidationRepository liquidationRepository)
    {
        _salesOrderRepository = salesOrderRepository;
        _liquidationRepository = liquidationRepository;
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

            // 1. Obtener liquidaciones de ruta del periodo para extraer devoluciones reales
            var (liquidations, _) = await _liquidationRepository.GetPagedAsync(
                fromDate, toDate, request.RouteId, status: null, pageNumber: 1, pageSize: 1000, cancellationToken);

            var returnsByProduct = new Dictionary<Guid, (decimal Qty, decimal Amount)>();
            foreach (var liqHeader in liquidations)
            {
                var fullLiq = await _liquidationRepository.GetByIdWithDetailsAsync(liqHeader.Id, cancellationToken);
                if (fullLiq?.Details == null) continue;

                foreach (var d in fullLiq.Details.Where(x => x.QuantityReturned > 0))
                {
                    if (!returnsByProduct.ContainsKey(d.ProductId))
                    {
                        returnsByProduct[d.ProductId] = (0, 0);
                    }
                    var curRet = returnsByProduct[d.ProductId];
                    returnsByProduct[d.ProductId] = (curRet.Qty + d.QuantityReturned, curRet.Amount + (d.QuantityReturned * d.SalePrice));
                }
            }

            // 2. Agrupar entrega real y preventa original por producto
            var productGroup = new Dictionary<Guid, (string Code, string Name, string Uom, decimal Delivered, decimal Price)>();

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
                    decimal deliv = detail.Quantity;

                    if (!productGroup.ContainsKey(prodId))
                    {
                        productGroup[prodId] = (code, name, uom, 0, price);
                    }

                    var cur = productGroup[prodId];
                    productGroup[prodId] = (code, name, uom, cur.Delivered + deliv, price);
                }
            }

            var items = new List<PresaleShortageItemDto>();

            // Unir productos de facturas y devoluciones
            var allProductIds = productGroup.Keys.Union(returnsByProduct.Keys).Distinct();

            foreach (var prodId in allProductIds)
            {
                string code = "S/K";
                string name = "Producto";
                string uom = "UND";
                decimal price = 0;
                decimal delivered = 0;

                if (productGroup.ContainsKey(prodId))
                {
                    var p = productGroup[prodId];
                    code = p.Code;
                    name = p.Name;
                    uom = p.Uom;
                    price = p.Price;
                    delivered = p.Delivered;
                }

                decimal shortage = returnsByProduct.ContainsKey(prodId) ? returnsByProduct[prodId].Qty : 0;
                decimal lossAmount = returnsByProduct.ContainsKey(prodId) ? returnsByProduct[prodId].Amount : 0;
                decimal requested = delivered + shortage;

                // Si no habia precio cargado, calcular
                if (price == 0 && shortage > 0 && lossAmount > 0)
                {
                    price = Math.Round(lossAmount / shortage, 2);
                }

                items.Add(new PresaleShortageItemDto(
                    ProductCode: code,
                    ProductName: name,
                    UnitOfMeasureCode: uom,
                    RequestedQuantity: requested,
                    DeliveredQuantity: delivered,
                    ShortageQuantity: shortage,
                    UnitPrice: price,
                    TotalLossAmount: Math.Round(lossAmount, 2)
                ));
            }

            // Ordenar de mayor a menor perdida
            items = items.OrderByDescending(x => x.TotalLossAmount).ThenByDescending(x => x.ShortageQuantity).ToList();

            return new PresaleShortagesReportDto(
                FromDate: request.FromDate,
                ToDate: request.ToDate,
                RouteId: request.RouteId,
                TotalUniqueProductsWithShortage: items.Count(x => x.ShortageQuantity > 0),
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
