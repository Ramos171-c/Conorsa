using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using EnterpriseBillingSystem.Domain.Repositories;

namespace EnterpriseBillingSystem.Application.Sales.Queries;

public record RouteReturnItemDto(
    Guid LiquidationId,
    string LiquidationNumber,
    DateTime LiquidationDate,
    Guid RouteId,
    string RouteName,
    Guid ProductId,
    string ProductCode,
    string ProductName,
    string UnitOfMeasureCode,
    string? PresentationName,
    decimal QuantitySent,
    decimal QuantityReturned,
    decimal QuantitySold,
    decimal SalePrice,
    decimal SubtotalReturned,
    string? Notes
);

public record RouteReturnsReportDto(
    DateTime? FromDate,
    DateTime? ToDate,
    Guid? RouteId,
    int TotalReturnedItemsCount,
    decimal TotalReturnedQuantity,
    decimal TotalReturnedAmount,
    IEnumerable<RouteReturnItemDto> Items
);

public record GetRouteReturnsReportQuery(
    DateTime? FromDate,
    DateTime? ToDate,
    Guid? RouteId
) : IRequest<RouteReturnsReportDto>;

public class GetRouteReturnsReportQueryHandler : IRequestHandler<GetRouteReturnsReportQuery, RouteReturnsReportDto>
{
    private readonly IRouteLiquidationRepository _repository;

    public GetRouteReturnsReportQueryHandler(IRouteLiquidationRepository repository)
    {
        _repository = repository;
    }

    public async Task<RouteReturnsReportDto> Handle(GetRouteReturnsReportQuery request, CancellationToken cancellationToken)
    {
        try
        {
            var (liquidations, _) = await _repository.GetPagedAsync(
                request.FromDate,
                request.ToDate,
                request.RouteId,
                status: null,
                pageNumber: 1,
                pageSize: 1000,
                cancellationToken);

            var returnItems = new List<RouteReturnItemDto>();

            foreach (var liqHeader in liquidations)
            {
                var fullLiq = await _repository.GetByIdWithDetailsAsync(liqHeader.Id, cancellationToken);
                if (fullLiq?.Details == null) continue;

                foreach (var d in fullLiq.Details.Where(x => x.QuantityReturned > 0))
                {
                    var prodCode = d.Product?.InternalCode ?? "";
                    var prodName = d.Product?.Name ?? "Producto";
                    var uomCode = d.UnitOfMeasure?.Code ?? "UND";
                    var presName = d.ProductPresentation?.Name;

                    returnItems.Add(new RouteReturnItemDto(
                        LiquidationId: fullLiq.Id,
                        LiquidationNumber: fullLiq.LiquidationNumber,
                        LiquidationDate: fullLiq.LiquidationDate,
                        RouteId: fullLiq.RouteId,
                        RouteName: fullLiq.Route?.Name ?? "Ruta Generica",
                        ProductId: d.ProductId,
                        ProductCode: prodCode,
                        ProductName: prodName,
                        UnitOfMeasureCode: uomCode,
                        PresentationName: presName,
                        QuantitySent: d.QuantitySent,
                        QuantityReturned: d.QuantityReturned,
                        QuantitySold: d.QuantitySold,
                        SalePrice: d.SalePrice,
                        SubtotalReturned: d.SubtotalReturned,
                        Notes: string.IsNullOrWhiteSpace(d.Notes) ? fullLiq.Observations : d.Notes
                    ));
                }
            }

            returnItems = returnItems.OrderByDescending(x => x.LiquidationDate).ToList();

            return new RouteReturnsReportDto(
                FromDate: request.FromDate,
                ToDate: request.ToDate,
                RouteId: request.RouteId,
                TotalReturnedItemsCount: returnItems.Count,
                TotalReturnedQuantity: returnItems.Sum(x => x.QuantityReturned),
                TotalReturnedAmount: returnItems.Sum(x => x.SubtotalReturned),
                Items: returnItems
            );
        }
        catch
        {
            return new RouteReturnsReportDto(
                FromDate: request.FromDate,
                ToDate: request.ToDate,
                RouteId: request.RouteId,
                TotalReturnedItemsCount: 0,
                TotalReturnedQuantity: 0,
                TotalReturnedAmount: 0,
                Items: new List<RouteReturnItemDto>()
            );
        }
    }
}
