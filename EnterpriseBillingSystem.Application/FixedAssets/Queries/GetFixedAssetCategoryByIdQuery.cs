using System;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using EnterpriseBillingSystem.Application.FixedAssets.DTOs;
using EnterpriseBillingSystem.Domain.Repositories;

namespace EnterpriseBillingSystem.Application.FixedAssets.Queries;

public record GetFixedAssetCategoryByIdQuery(Guid Id) : IRequest<FixedAssetCategoryDto?>;

public class GetFixedAssetCategoryByIdQueryHandler : IRequestHandler<GetFixedAssetCategoryByIdQuery, FixedAssetCategoryDto?>
{
    private readonly IFixedAssetCategoryRepository _categoryRepository;

    public GetFixedAssetCategoryByIdQueryHandler(IFixedAssetCategoryRepository categoryRepository)
    {
        _categoryRepository = categoryRepository;
    }

    public async Task<FixedAssetCategoryDto?> Handle(
        GetFixedAssetCategoryByIdQuery request,
        CancellationToken cancellationToken)
    {
        var c = await _categoryRepository.GetByIdAsync(request.Id);
        if (c == null) return null;

        return new FixedAssetCategoryDto(
            c.Id, c.Code, c.Name,
            c.AssetAccountCode,
            c.AccumulatedDepreciationAccountCode,
            c.DepreciationExpenseAccountCode,
            c.UsefulLifeMonths,
            c.IsActive
        );
    }
}
