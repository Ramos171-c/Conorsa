using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using EnterpriseBillingSystem.Application.FixedAssets.DTOs;
using EnterpriseBillingSystem.Domain.Repositories;

namespace EnterpriseBillingSystem.Application.FixedAssets.Queries;

public record GetFixedAssetByIdQuery(Guid Id) : IRequest<FixedAssetDto?>;

public class GetFixedAssetByIdQueryHandler : IRequestHandler<GetFixedAssetByIdQuery, FixedAssetDto?>
{
    private readonly IFixedAssetRepository _assetRepository;

    public GetFixedAssetByIdQueryHandler(IFixedAssetRepository assetRepository)
    {
        _assetRepository = assetRepository;
    }

    public async Task<FixedAssetDto?> Handle(
        GetFixedAssetByIdQuery request,
        CancellationToken cancellationToken)
    {
        var a = await _assetRepository.GetByIdWithDetailsAsync(request.Id, cancellationToken);
        if (a == null) return null;

        return new FixedAssetDto(
            a.Id, a.AssetNumber, a.Name, a.Description,
            a.FixedAssetCategoryId,
            a.Category?.Name ?? string.Empty,
            a.Category?.AssetAccountCode ?? string.Empty,
            a.PurchaseInvoiceId,
            a.AcquisitionDate, a.AcquisitionCost, a.ResidualValue,
            a.UsefulLifeMonths, a.DepreciationStartDate,
            a.AccumulatedDepreciation, a.CurrentBookValue,
            a.Status, a.Status.ToString(),
            a.Location, a.SerialNumber, a.Notes,
            a.LastDepreciationDate, a.BranchId
        );
    }
}
