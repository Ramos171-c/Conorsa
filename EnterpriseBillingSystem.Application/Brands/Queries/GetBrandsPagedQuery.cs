using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using EnterpriseBillingSystem.Domain.Repositories;
using EnterpriseBillingSystem.Application.Brands.DTOs;
using EnterpriseBillingSystem.Application.Common.Models;

namespace EnterpriseBillingSystem.Application.Brands.Queries;

public record GetBrandsPagedQuery(
    int PageNumber = 1,
    int PageSize = 10,
    string? SearchTerm = null
) : IRequest<PagedResult<BrandDto>>;

public class GetBrandsPagedQueryHandler : IRequestHandler<GetBrandsPagedQuery, PagedResult<BrandDto>>
{
    private readonly IBrandRepository _brandRepository;

    public GetBrandsPagedQueryHandler(IBrandRepository brandRepository)
    {
        _brandRepository = brandRepository;
    }

    public async Task<PagedResult<BrandDto>> Handle(GetBrandsPagedQuery request, CancellationToken cancellationToken)
    {
        var (items, totalCount) = await _brandRepository.GetPagedAsync(
            request.PageNumber,
            request.PageSize,
            request.SearchTerm,
            cancellationToken);

        var dtos = items.Select(b => new BrandDto(
            b.Id,
            b.Name,
            b.Description,
            b.IsActive
        )).ToList();

        return new PagedResult<BrandDto>(dtos, totalCount, request.PageNumber, request.PageSize);
    }
}
