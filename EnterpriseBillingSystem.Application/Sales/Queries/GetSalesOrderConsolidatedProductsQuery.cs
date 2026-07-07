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
    decimal TotalNetAmount
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

    public GetSalesOrderConsolidatedProductsQueryHandler(ISalesOrderRepository repository)
    {
        _repository = repository;
    }

    public async Task<IEnumerable<ConsolidatedProductDto>> Handle(GetSalesOrderConsolidatedProductsQuery request, CancellationToken cancellationToken)
    {
        var orders = await _repository.GetFilteredWithDetailsAsync(
            request.CustomerId, request.Status, request.FromDate, request.ToDate, request.RouteId, cancellationToken);

        var consolidated = orders
            .SelectMany(o => o.Details)
            .GroupBy(d => new { d.ProductId, Code = d.Product?.InternalCode ?? string.Empty, Name = d.Product?.Name ?? "Producto Desconocido", Uom = d.UnitOfMeasure?.Code ?? string.Empty })
            .Select(g => new ConsolidatedProductDto(
                ProductId: g.Key.ProductId,
                ProductCode: g.Key.Code,
                ProductName: g.Key.Name,
                UnitOfMeasure: g.Key.Uom,
                TotalQuantity: g.Sum(x => x.Quantity),
                TotalNetAmount: g.Sum(x => x.NetAmount)
            ))
            .OrderBy(c => c.ProductName)
            .ToList();

        return consolidated;
    }
}
