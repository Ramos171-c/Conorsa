using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using EnterpriseBillingSystem.Application.FixedAssets.DTOs;
using EnterpriseBillingSystem.Domain.Enums;
using EnterpriseBillingSystem.Domain.Repositories;

namespace EnterpriseBillingSystem.Application.FixedAssets.Queries;

public record FixedAssetRegisterItem(
    string AssetNumber,
    string Name,
    string CategoryName,
    DateTime AcquisitionDate,
    decimal AcquisitionCost,
    decimal AccumulatedDepreciation,
    decimal CurrentBookValue,
    FixedAssetStatus Status,
    string StatusName,
    string? Location
);

public record GetFixedAssetRegisterQuery(
    Guid? CategoryId = null,
    Guid? BranchId = null
) : IRequest<IEnumerable<FixedAssetRegisterItem>>;

public class GetFixedAssetRegisterQueryHandler : IRequestHandler<GetFixedAssetRegisterQuery, IEnumerable<FixedAssetRegisterItem>>
{
    private readonly IFixedAssetRepository _assetRepository;

    public GetFixedAssetRegisterQueryHandler(IFixedAssetRepository assetRepository)
    {
        _assetRepository = assetRepository;
    }

    public async Task<IEnumerable<FixedAssetRegisterItem>> Handle(
        GetFixedAssetRegisterQuery request,
        CancellationToken cancellationToken)
    {
        // Traer todos (sin paginación) para el libro maestro
        var (items, _) = await _assetRepository.GetPagedAsync(
            1, int.MaxValue, request.CategoryId, null, request.BranchId, cancellationToken);

        return items
            .Where(a => a.Status != FixedAssetStatus.Disposed)
            .Select(a => new FixedAssetRegisterItem(
                a.AssetNumber, a.Name,
                a.Category?.Name ?? string.Empty,
                a.AcquisitionDate,
                a.AcquisitionCost,
                a.AccumulatedDepreciation,
                a.CurrentBookValue,
                a.Status, a.Status.ToString(),
                a.Location
            ))
            .OrderBy(a => a.AssetNumber)
            .ToList();
    }
}
