using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using EnterpriseBillingSystem.Domain.Repositories;

namespace EnterpriseBillingSystem.Application.Sales.Queries;

public record RouteLiquidationDetailDto(
    Guid Id,
    Guid ProductId,
    string ProductCode,
    string ProductName,
    Guid UnitOfMeasureId,
    string UnitOfMeasureCode,
    Guid? ProductPresentationId,
    string? PresentationName,
    decimal QuantitySent,
    decimal QuantityReturned,
    decimal QuantitySold,
    decimal BaseQuantitySent,
    decimal BaseQuantityReturned,
    decimal BaseQuantitySold,
    decimal SalePrice,
    decimal Cost,
    decimal SubtotalSold,
    decimal SubtotalReturned,
    string? Notes
);

public record RouteLiquidationFullDto(
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
    string CreatedBy,
    IEnumerable<RouteLiquidationDetailDto> Details
);

public record GetRouteLiquidationByIdQuery(Guid Id) : IRequest<RouteLiquidationFullDto?>;

public class GetRouteLiquidationByIdQueryHandler : IRequestHandler<GetRouteLiquidationByIdQuery, RouteLiquidationFullDto?>
{
    private readonly IRouteLiquidationRepository _repository;

    public GetRouteLiquidationByIdQueryHandler(IRouteLiquidationRepository repository)
    {
        _repository = repository;
    }

    public async Task<RouteLiquidationFullDto?> Handle(GetRouteLiquidationByIdQuery request, CancellationToken cancellationToken)
    {
        var rl = await _repository.GetByIdWithDetailsAsync(request.Id, cancellationToken);
        if (rl == null) return null;

        var details = rl.Details.Select(d => new RouteLiquidationDetailDto(
            Id: d.Id,
            ProductId: d.ProductId,
            ProductCode: d.Product?.InternalCode ?? string.Empty,
            ProductName: !string.IsNullOrWhiteSpace(d.Product?.Description) ? d.Product.Description : (d.Product?.Name ?? "Producto"),
            UnitOfMeasureId: d.UnitOfMeasureId,
            UnitOfMeasureCode: d.UnitOfMeasure?.Code ?? "UND",
            ProductPresentationId: d.ProductPresentationId,
            PresentationName: d.ProductPresentation?.Name ?? d.UnitOfMeasure?.Code ?? "UND",
            QuantitySent: d.QuantitySent,
            QuantityReturned: d.QuantityReturned,
            QuantitySold: d.QuantitySold,
            BaseQuantitySent: d.BaseQuantitySent,
            BaseQuantityReturned: d.BaseQuantityReturned,
            BaseQuantitySold: d.BaseQuantitySold,
            SalePrice: d.SalePrice,
            Cost: d.Cost,
            SubtotalSold: d.SubtotalSold,
            SubtotalReturned: d.SubtotalReturned,
            Notes: d.Notes
        ));

        return new RouteLiquidationFullDto(
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
            CreatedBy: rl.CreatedBy ?? "System",
            Details: details
        );
    }
}
