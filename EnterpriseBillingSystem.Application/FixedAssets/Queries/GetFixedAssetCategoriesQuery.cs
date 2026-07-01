using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using EnterpriseBillingSystem.Application.Common.Models;
using EnterpriseBillingSystem.Application.FixedAssets.DTOs;
using EnterpriseBillingSystem.Domain.Repositories;

namespace EnterpriseBillingSystem.Application.FixedAssets.Queries;

public record GetFixedAssetCategoriesQuery(
    int PageNumber = 1,
    int PageSize = 20,
    string? SearchTerm = null
) : IRequest<PagedResult<FixedAssetCategoryDto>>;

public class GetFixedAssetCategoriesQueryHandler : IRequestHandler<GetFixedAssetCategoriesQuery, PagedResult<FixedAssetCategoryDto>>
{
    private readonly IFixedAssetCategoryRepository _categoryRepository;

    public GetFixedAssetCategoriesQueryHandler(IFixedAssetCategoryRepository categoryRepository)
    {
        _categoryRepository = categoryRepository;
    }

    public async Task<PagedResult<FixedAssetCategoryDto>> Handle(
        GetFixedAssetCategoriesQuery request,
        CancellationToken cancellationToken)
    {
        var (items, total) = await _categoryRepository.GetPagedAsync(
            request.PageNumber, request.PageSize, request.SearchTerm, cancellationToken);

        var dtos = items.Select(c => new FixedAssetCategoryDto(
            c.Id, c.Code, c.Name,
            c.AssetAccountCode,
            c.AccumulatedDepreciationAccountCode,
            c.DepreciationExpenseAccountCode,
            c.UsefulLifeMonths,
            c.IsActive
        )).ToList();

        return new PagedResult<FixedAssetCategoryDto>(dtos, total, request.PageNumber, request.PageSize);
    }
}
