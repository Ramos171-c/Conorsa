using System;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using EnterpriseBillingSystem.Domain.Repositories;
using EnterpriseBillingSystem.Application.CustomerCategories.DTOs;

namespace EnterpriseBillingSystem.Application.CustomerCategories.Queries;

public record GetCustomerCategoryByIdQuery(Guid Id) : IRequest<CustomerCategoryDto?>;

public class GetCustomerCategoryByIdQueryHandler : IRequestHandler<GetCustomerCategoryByIdQuery, CustomerCategoryDto?>
{
    private readonly ICustomerCategoryRepository _customerCategoryRepository;

    public GetCustomerCategoryByIdQueryHandler(ICustomerCategoryRepository customerCategoryRepository)
    {
        _customerCategoryRepository = customerCategoryRepository;
    }

    public async Task<CustomerCategoryDto?> Handle(GetCustomerCategoryByIdQuery request, CancellationToken cancellationToken)
    {
        var category = await _customerCategoryRepository.GetByIdAsync(request.Id);
        if (category == null) return null;

        return new CustomerCategoryDto(
            category.Id,
            category.Name,
            category.Description,
            category.DefaultDiscountPercentage
        );
    }
}
