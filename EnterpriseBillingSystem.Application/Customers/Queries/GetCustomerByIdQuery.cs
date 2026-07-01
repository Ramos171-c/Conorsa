using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using EnterpriseBillingSystem.Domain.Enums;
using EnterpriseBillingSystem.Domain.Repositories;
using EnterpriseBillingSystem.Application.Customers.DTOs;

namespace EnterpriseBillingSystem.Application.Customers.Queries;

public record GetCustomerByIdQuery(Guid Id) : IRequest<CustomerDto?>;

public class GetCustomerByIdQueryHandler : IRequestHandler<GetCustomerByIdQuery, CustomerDto?>
{
    private readonly ICustomerRepository _customerRepository;

    public GetCustomerByIdQueryHandler(ICustomerRepository customerRepository)
    {
        _customerRepository = customerRepository;
    }

    public async Task<CustomerDto?> Handle(GetCustomerByIdQuery request, CancellationToken cancellationToken)
    {
        var customer = await _customerRepository.GetByIdWithDetailsAsync(request.Id, cancellationToken);
        if (customer == null) return null;

        return new CustomerDto(
            customer.Id,
            customer.CustomerCode,
            customer.IdentificationNumber,
            customer.IdentificationType,
            customer.CustomerType,
            customer.Name,
            customer.LegalName,
            customer.CustomerCategoryId,
            customer.CustomerCategory?.Name ?? string.Empty,
            customer.CustomerPricingProfileId,
            customer.CustomerPricingProfile?.Name ?? string.Empty,
            customer.CustomerPricingProfile?.Type ?? CustomerPricingType.Retail,
            customer.CreditLimit,
            customer.CreditDays,
            customer.CanUseCredit,
            customer.IsTaxExempt,
            customer.DefaultDiscountPercentage,
            customer.Status,
            customer.Addresses.Select(a => new CustomerAddressDto(
                a.Id, a.AddressLine1, a.AddressLine2, a.City, a.State, a.ZipCode, a.Country, a.AddressType, a.IsDefault
            )).ToList(),
            customer.Phones.Select(p => new CustomerPhoneDto(
                p.Id, p.PhoneNumber, p.PhoneType, p.IsDefault
            )).ToList(),
            customer.Emails.Select(e => new CustomerEmailDto(
                e.Id, e.EmailAddress, e.EmailType, e.IsDefault
            )).ToList(),
            customer.Contacts.Select(c => new CustomerContactDto(
                c.Id, c.FirstName, c.LastName, c.JobTitle, c.Phone, c.Email, c.Notes, c.IsDefault
            )).ToList(),
            customer.RouteId,
            customer.Route?.Name
        );
    }
}
