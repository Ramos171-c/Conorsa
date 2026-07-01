using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using EnterpriseBillingSystem.Domain.Entities;
using EnterpriseBillingSystem.Domain.Repositories;

namespace EnterpriseBillingSystem.Application.Purchases.Queries;

public record SupplierCategoryDto(Guid Id, string Name, string? Description);

public record GetSupplierCategoriesQuery : IRequest<IEnumerable<SupplierCategoryDto>>;

public class GetSupplierCategoriesQueryHandler : IRequestHandler<GetSupplierCategoriesQuery, IEnumerable<SupplierCategoryDto>>
{
    private readonly IRepository<SupplierCategory> _categoryRepository;

    public GetSupplierCategoriesQueryHandler(IRepository<SupplierCategory> categoryRepository)
    {
        _categoryRepository = categoryRepository;
    }

    public async Task<IEnumerable<SupplierCategoryDto>> Handle(GetSupplierCategoriesQuery request, CancellationToken cancellationToken)
    {
        var categories = await _categoryRepository.GetAllAsync();
        return categories.Select(c => new SupplierCategoryDto(c.Id, c.Name, c.Description));
    }
}
