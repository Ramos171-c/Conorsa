using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using EnterpriseBillingSystem.Domain.Repositories;
using EnterpriseBillingSystem.Application.Customers.DTOs;

namespace EnterpriseBillingSystem.Application.Customers.Queries;

public record GetCustomerPricingProfilesQuery : IRequest<IEnumerable<CustomerPricingProfileDto>>;

public class GetCustomerPricingProfilesQueryHandler : IRequestHandler<GetCustomerPricingProfilesQuery, IEnumerable<CustomerPricingProfileDto>>
{
    private readonly IRepository<Domain.Entities.CustomerPricingProfile> _repository;

    public GetCustomerPricingProfilesQueryHandler(IRepository<Domain.Entities.CustomerPricingProfile> repository)
    {
        _repository = repository;
    }

    public async Task<IEnumerable<CustomerPricingProfileDto>> Handle(GetCustomerPricingProfilesQuery request, CancellationToken cancellationToken)
    {
        var profiles = await _repository.GetAllAsync();
        return profiles
            .Where(p => p.IsActive && !p.IsDeleted)
            .Select(p => new CustomerPricingProfileDto(p.Id, p.Name, p.Type, p.IsActive))
            .ToList();
    }
}
