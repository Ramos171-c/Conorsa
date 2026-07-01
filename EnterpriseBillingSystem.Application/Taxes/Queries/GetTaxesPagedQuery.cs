using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using EnterpriseBillingSystem.Domain.Repositories;
using EnterpriseBillingSystem.Application.Taxes.DTOs;
using EnterpriseBillingSystem.Application.Common.Models;

namespace EnterpriseBillingSystem.Application.Taxes.Queries;

public record GetTaxesPagedQuery(
    int PageNumber = 1,
    int PageSize = 10,
    string? SearchTerm = null
) : IRequest<PagedResult<TaxDto>>;

public class GetTaxesPagedQueryHandler : IRequestHandler<GetTaxesPagedQuery, PagedResult<TaxDto>>
{
    private readonly ITaxRepository _taxRepository;

    public GetTaxesPagedQueryHandler(ITaxRepository taxRepository)
    {
        _taxRepository = taxRepository;
    }

    public async Task<PagedResult<TaxDto>> Handle(GetTaxesPagedQuery request, CancellationToken cancellationToken)
    {
        var (items, totalCount) = await _taxRepository.GetPagedAsync(
            request.PageNumber,
            request.PageSize,
            request.SearchTerm,
            cancellationToken);

        var dtos = items.Select(t => new TaxDto(
            t.Id,
            t.Name,
            t.Rate,
            t.IsActive
        )).ToList();

        return new PagedResult<TaxDto>(dtos, totalCount, request.PageNumber, request.PageSize);
    }
}
