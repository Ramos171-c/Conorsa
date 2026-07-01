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

public record GetBankAccountsPagedQuery(
    int PageNumber = 1,
    int PageSize = 10,
    Guid? BankId = null,
    string? SearchTerm = null
) : IRequest<PagedResult<BankAccountDto>>;

public class GetBankAccountsPagedQueryHandler : IRequestHandler<GetBankAccountsPagedQuery, PagedResult<BankAccountDto>>
{
    private readonly IBankAccountRepository _bankAccountRepository;

    public GetBankAccountsPagedQueryHandler(IBankAccountRepository bankAccountRepository)
    {
        _bankAccountRepository = bankAccountRepository;
    }

    public async Task<PagedResult<BankAccountDto>> Handle(GetBankAccountsPagedQuery request, CancellationToken cancellationToken)
    {
        var all = await _bankAccountRepository.GetAllAsync();
        var filtered = all.AsQueryable();

        if (request.BankId.HasValue)
            filtered = filtered.Where(a => a.BankId == request.BankId.Value);

        if (!string.IsNullOrWhiteSpace(request.SearchTerm))
        {
            var term = request.SearchTerm.ToLower();
            filtered = filtered.Where(a =>
                a.AccountNumber.ToLower().Contains(term) ||
                a.AccountName.ToLower().Contains(term));
        }

        var totalCount = filtered.Count();
        var items = filtered
            .Skip((request.PageNumber - 1) * request.PageSize)
            .Take(request.PageSize)
            .Select(a => new BankAccountDto(
                a.Id,
                a.BankId,
                a.Bank != null ? a.Bank.Name : string.Empty,
                a.AccountNumber,
                a.AccountName,
                a.CurrencyCode,
                a.CurrentBalance,
                a.AccountingAccountCode,
                a.IsActive,
                a.BranchId
            ))
            .ToList();

        return new PagedResult<BankAccountDto>(items, totalCount, request.PageNumber, request.PageSize);
    }
}
