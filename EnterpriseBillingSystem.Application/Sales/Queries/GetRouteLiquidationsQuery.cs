using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using EnterpriseBillingSystem.Domain.Repositories;

namespace EnterpriseBillingSystem.Application.Sales.Queries;

public record RouteLiquidationListItemDto(
    Guid Id,
    string LiquidationNumber,
    Guid RouteId,
    string RouteName,
    DateTime LiquidationDate,
    string Status,
    decimal TotalQuantitySent,
    decimal TotalQuantityReturned,
    decimal TotalQuantitySold,
    decimal TotalAmountSold,
    decimal TotalAmountReturned,
    decimal TotalCostSold,
    decimal EstimatedProfit,
    string? Observations,
    string CreatedBy
);

public record RouteLiquidationPagedResultDto(
    IEnumerable<RouteLiquidationListItemDto> Items,
    int TotalCount,
    int PageNumber,
    int PageSize
);

public record GetRouteLiquidationsQuery(
    DateTime? FromDate,
    DateTime? ToDate,
    Guid? RouteId,
    string? Status,
    int PageNumber = 1,
    int PageSize = 20
) : IRequest<RouteLiquidationPagedResultDto>;

public class GetRouteLiquidationsQueryHandler : IRequestHandler<GetRouteLiquidationsQuery, RouteLiquidationPagedResultDto>
{
    private readonly IRouteLiquidationRepository _repository;

    public GetRouteLiquidationsQueryHandler(IRouteLiquidationRepository repository)
    {
        _repository = repository;
    }

    public async Task<RouteLiquidationPagedResultDto> Handle(GetRouteLiquidationsQuery request, CancellationToken cancellationToken)
    {
        var (items, totalCount) = await _repository.GetPagedAsync(
            request.FromDate,
            request.ToDate,
            request.RouteId,
            request.Status,
            request.PageNumber,
            request.PageSize,
            cancellationToken);

        var list = items.Select(rl => new RouteLiquidationListItemDto(
            Id: rl.Id,
            LiquidationNumber: rl.LiquidationNumber,
            RouteId: rl.RouteId,
            RouteName: rl.Route?.Name ?? "Ruta Desconocida",
            LiquidationDate: rl.LiquidationDate,
            Status: rl.Status.ToString(),
            TotalQuantitySent: rl.TotalQuantitySent,
            TotalQuantityReturned: rl.TotalQuantityReturned,
            TotalQuantitySold: rl.TotalQuantitySold,
            TotalAmountSold: rl.TotalAmountSold,
            TotalAmountReturned: rl.TotalAmountReturned,
            TotalCostSold: rl.TotalCostSold,
            EstimatedProfit: rl.EstimatedProfit,
            Observations: rl.Observations,
            CreatedBy: rl.CreatedBy ?? "System"
        ));

        return new RouteLiquidationPagedResultDto(list, totalCount, request.PageNumber, request.PageSize);
    }
}
