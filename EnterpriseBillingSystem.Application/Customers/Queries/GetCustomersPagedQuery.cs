using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using EnterpriseBillingSystem.Domain.Repositories;
using EnterpriseBillingSystem.Domain.Enums;
using EnterpriseBillingSystem.Application.Customers.DTOs;
using EnterpriseBillingSystem.Application.Common.Models;

namespace EnterpriseBillingSystem.Application.Customers.Queries;

public record GetCustomersPagedQuery(
    int PageNumber = 1,
    int PageSize = 10,
    string? SearchTerm = null,
    Guid? CategoryId = null,
    CustomerStatus? Status = null
) : IRequest<PagedResult<CustomerDto>>;

public class GetCustomersPagedQueryHandler : IRequestHandler<GetCustomersPagedQuery, PagedResult<CustomerDto>>
{
    private readonly ICustomerRepository _customerRepository;

    public GetCustomersPagedQueryHandler(ICustomerRepository customerRepository)
    {
        _customerRepository = customerRepository;
    }

    public async Task<PagedResult<CustomerDto>> Handle(GetCustomersPagedQuery request, CancellationToken cancellationToken)
    {
        var (items, totalCount) = await _customerRepository.GetPagedAsync(
            request.PageNumber,
            request.PageSize,
            request.SearchTerm,
            request.CategoryId,
            request.Status,
            cancellationToken);

        var dtos = items.Select(c => new CustomerDto(
            c.Id,
            c.CustomerCode,
            c.IdentificationNumber,
            c.IdentificationType,
            c.CustomerType,
            c.Name,
            c.LegalName,
            c.CustomerCategoryId,
            c.CustomerCategory?.Name ?? string.Empty,
            c.CustomerPricingProfileId,
            c.CustomerPricingProfile?.Name ?? string.Empty,
            c.CustomerPricingProfile?.Type ?? CustomerPricingType.Retail,
            c.CreditLimit,
            c.CreditDays,
            c.CanUseCredit,
            c.IsTaxExempt,
            c.DefaultDiscountPercentage,
            c.Status,
            new List<CustomerAddressDto>(), // Omitir detalles en listados para optimizar ancho de banda
            new List<CustomerPhoneDto>(),
            new List<CustomerEmailDto>(),
            new List<CustomerContactDto>(),
            c.RouteId,
            c.Route?.Name
        )).ToList();

        return new PagedResult<CustomerDto>(dtos, totalCount, request.PageNumber, request.PageSize);
    }
}
