using System;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using EnterpriseBillingSystem.Domain.Entities;
using EnterpriseBillingSystem.Domain.Repositories;
using EnterpriseBillingSystem.Application.Taxes.DTOs;

namespace EnterpriseBillingSystem.Application.Taxes.Queries;

public record GetTaxByIdQuery(Guid Id) : IRequest<TaxDto?>;

public class GetTaxByIdQueryHandler : IRequestHandler<GetTaxByIdQuery, TaxDto?>
{
    private readonly IRepository<Tax> _taxRepository;

    public GetTaxByIdQueryHandler(IRepository<Tax> taxRepository)
    {
        _taxRepository = taxRepository;
    }

    public async Task<TaxDto?> Handle(GetTaxByIdQuery request, CancellationToken cancellationToken)
    {
        var tax = await _taxRepository.GetByIdAsync(request.Id);
        if (tax == null) return null;

        return new TaxDto(
            Id: tax.Id,
            Name: tax.Name,
            Rate: tax.Rate,
            IsActive: tax.IsActive
        );
    }
}
