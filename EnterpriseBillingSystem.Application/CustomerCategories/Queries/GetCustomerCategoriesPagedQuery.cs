using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using EnterpriseBillingSystem.Domain.Repositories;
using EnterpriseBillingSystem.Application.CustomerCategories.DTOs;
using EnterpriseBillingSystem.Application.Common.Models;

namespace EnterpriseBillingSystem.Application.CustomerCategories.Queries;

public record GetCustomerCategoriesPagedQuery(
    int PageNumber = 1,
    int PageSize = 10,
    string? SearchTerm = null
) : IRequest<PagedResult<CustomerCategoryDto>>;

public class GetCustomerCategoriesPagedQueryHandler : IRequestHandler<GetCustomerCategoriesPagedQuery, PagedResult<CustomerCategoryDto>>
{
    private readonly ICustomerCategoryRepository _customerCategoryRepository;

    public GetCustomerCategoriesPagedQueryHandler(ICustomerCategoryRepository customerCategoryRepository)
    {
        _customerCategoryRepository = customerCategoryRepository;
    }

    public async Task<PagedResult<CustomerCategoryDto>> Handle(GetCustomerCategoriesPagedQuery request, CancellationToken cancellationToken)
    {
        var (items, totalCount) = await _customerCategoryRepository.GetPagedAsync(
            request.PageNumber,
            request.PageSize,
            request.SearchTerm,
            cancellationToken);

        var dtos = items.Select(c => new CustomerCategoryDto(
            c.Id,
            c.Name,
            c.Description,
            c.DefaultDiscountPercentage
        )).ToList();

        return new PagedResult<CustomerCategoryDto>(dtos, totalCount, request.PageNumber, request.PageSize);
    }
}
