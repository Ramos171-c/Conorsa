using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using EnterpriseBillingSystem.Domain.Entities;
using EnterpriseBillingSystem.Domain.Repositories;

namespace EnterpriseBillingSystem.Application.Products.Queries;

public record ProductPriceHistoryDto(
    Guid Id,
    Guid ProductId,
    Guid? ProductPresentationId,
    decimal OldRetailPrice,
    decimal NewRetailPrice,
    decimal OldSemiWholesalePrice,
    decimal NewSemiWholesalePrice,
    decimal OldWholesalePrice,
    decimal NewWholesalePrice,
    decimal OldCost,
    decimal NewCost,
    string ChangedBy,
    DateTime ChangedAt,
    string? Reason
);

public record GetProductPriceHistoryQuery(Guid ProductId) : IRequest<IEnumerable<ProductPriceHistoryDto>>;

public class GetProductPriceHistoryQueryHandler : IRequestHandler<GetProductPriceHistoryQuery, IEnumerable<ProductPriceHistoryDto>>
{
    private readonly IRepository<ProductPriceHistory> _priceHistoryRepository;

    public GetProductPriceHistoryQueryHandler(IRepository<ProductPriceHistory> priceHistoryRepository)
    {
        _priceHistoryRepository = priceHistoryRepository;
    }

    public async Task<IEnumerable<ProductPriceHistoryDto>> Handle(GetProductPriceHistoryQuery request, CancellationToken cancellationToken)
    {
        var history = await _priceHistoryRepository.FindAsync(h => h.ProductId == request.ProductId);
        return history
            .OrderByDescending(h => h.ChangedAt)
            .Select(h => new ProductPriceHistoryDto(
                Id: h.Id,
                ProductId: h.ProductId,
                ProductPresentationId: h.ProductPresentationId,
                OldRetailPrice: h.OldRetailPrice,
                NewRetailPrice: h.NewRetailPrice,
                OldSemiWholesalePrice: h.OldSemiWholesalePrice,
                NewSemiWholesalePrice: h.NewSemiWholesalePrice,
                OldWholesalePrice: h.OldWholesalePrice,
                NewWholesalePrice: h.NewWholesalePrice,
                OldCost: h.OldCost,
                NewCost: h.NewCost,
                ChangedBy: h.ChangedBy,
                ChangedAt: h.ChangedAt,
                Reason: h.Reason
            ))
            .ToList();
    }
}
