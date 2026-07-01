using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using EnterpriseBillingSystem.Application.Common.Models;
using EnterpriseBillingSystem.Application.FixedAssets.DTOs;
using EnterpriseBillingSystem.Domain.Enums;
using EnterpriseBillingSystem.Domain.Repositories;

namespace EnterpriseBillingSystem.Application.FixedAssets.Queries;

public record GetFixedAssetsPagedQuery(
    int PageNumber = 1,
    int PageSize = 20,
    Guid? CategoryId = null,
    FixedAssetStatus? Status = null,
    Guid? BranchId = null
) : IRequest<PagedResult<FixedAssetDto>>;

public class GetFixedAssetsPagedQueryHandler : IRequestHandler<GetFixedAssetsPagedQuery, PagedResult<FixedAssetDto>>
{
    private readonly IFixedAssetRepository _assetRepository;

    public GetFixedAssetsPagedQueryHandler(IFixedAssetRepository assetRepository)
    {
        _assetRepository = assetRepository;
    }

    public async Task<PagedResult<FixedAssetDto>> Handle(
        GetFixedAssetsPagedQuery request,
        CancellationToken cancellationToken)
    {
        var (items, total) = await _assetRepository.GetPagedAsync(
            request.PageNumber, request.PageSize,
            request.CategoryId, request.Status, request.BranchId,
            cancellationToken);

        var dtos = items.Select(a => new FixedAssetDto(
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
        )).ToList();

        return new PagedResult<FixedAssetDto>(dtos, total, request.PageNumber, request.PageSize);
    }
}
