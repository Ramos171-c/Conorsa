using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using EnterpriseBillingSystem.Application.Banks.DTOs;
using EnterpriseBillingSystem.Application.Common.Models;
using EnterpriseBillingSystem.Domain.Repositories;

namespace EnterpriseBillingSystem.Application.Banks.Queries;

public record GetBanksPagedQuery(
    int PageNumber = 1,
    int PageSize = 10,
    string? SearchTerm = null
) : IRequest<PagedResult<BankDto>>;

public class GetBanksPagedQueryHandler : IRequestHandler<GetBanksPagedQuery, PagedResult<BankDto>>
{
    private readonly IBankRepository _bankRepository;

    public GetBanksPagedQueryHandler(IBankRepository bankRepository)
    {
        _bankRepository = bankRepository;
    }

    public async Task<PagedResult<BankDto>> Handle(GetBanksPagedQuery request, CancellationToken cancellationToken)
    {
        var allBanks = await _bankRepository.GetAllAsync();

        var filtered = allBanks.AsQueryable();
        if (!string.IsNullOrWhiteSpace(request.SearchTerm))
        {
            var term = request.SearchTerm.ToLower();
            filtered = filtered.Where(b =>
                b.Code.ToLower().Contains(term) ||
                b.Name.ToLower().Contains(term) ||
                b.Country.ToLower().Contains(term));
        }

        var totalCount = filtered.Count();
        var items = filtered
            .Skip((request.PageNumber - 1) * request.PageSize)
            .Take(request.PageSize)
            .Select(b => new BankDto(b.Id, b.Code, b.Name, b.SwiftCode, b.Country, b.IsActive))
            .ToList();

        return new PagedResult<BankDto>(items, totalCount, request.PageNumber, request.PageSize);
    }
}
