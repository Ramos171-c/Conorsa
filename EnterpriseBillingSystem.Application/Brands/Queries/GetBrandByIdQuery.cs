using System;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using EnterpriseBillingSystem.Domain.Entities;
using EnterpriseBillingSystem.Domain.Repositories;
using EnterpriseBillingSystem.Application.Brands.DTOs;

namespace EnterpriseBillingSystem.Application.Brands.Queries;

public record GetBrandByIdQuery(Guid Id) : IRequest<BrandDto?>;

public class GetBrandByIdQueryHandler : IRequestHandler<GetBrandByIdQuery, BrandDto?>
{
    private readonly IRepository<Brand> _brandRepository;

    public GetBrandByIdQueryHandler(IRepository<Brand> brandRepository)
    {
        _brandRepository = brandRepository;
    }

    public async Task<BrandDto?> Handle(GetBrandByIdQuery request, CancellationToken cancellationToken)
    {
        var brand = await _brandRepository.GetByIdAsync(request.Id);
        if (brand == null) return null;

        return new BrandDto(
            Id: brand.Id,
            Name: brand.Name,
            Description: brand.Description,
            IsActive: brand.IsActive
        );
    }
}
