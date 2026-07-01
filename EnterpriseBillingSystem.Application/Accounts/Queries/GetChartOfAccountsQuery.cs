using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using EnterpriseBillingSystem.Domain.Entities;
using EnterpriseBillingSystem.Domain.Repositories;

namespace EnterpriseBillingSystem.Application.Accounts.Queries;

public record AccountDto(
    Guid Id,
    string Code,
    string Name,
    Guid? ParentAccountId,
    string AccountType,
    string Nature,
    int Level,
    bool IsPostingAccount,
    bool IsActive,
    List<AccountDto> SubAccounts
);

public record GetChartOfAccountsQuery : IRequest<IEnumerable<AccountDto>>;

public class GetChartOfAccountsQueryHandler : IRequestHandler<GetChartOfAccountsQuery, IEnumerable<AccountDto>>
{
    private readonly IAccountRepository _accountRepository;

    public GetChartOfAccountsQueryHandler(IAccountRepository accountRepository)
    {
        _accountRepository = accountRepository;
    }

    public async Task<IEnumerable<AccountDto>> Handle(GetChartOfAccountsQuery request, CancellationToken cancellationToken)
    {
        var allAccounts = await _accountRepository.GetAllAsync();
        
        // Convertir todas a DTOs
        var dtos = allAccounts.Select(a => new AccountDto(
            a.Id,
            a.Code,
            a.Name,
            a.ParentAccountId,
            a.AccountType.ToString(),
            a.Nature.ToString(),
            a.Level,
            a.IsPostingAccount,
            a.IsActive,
            new List<AccountDto>()
        )).ToList();

        // Construir jerarquía
        var lookup = dtos.ToDictionary(x => x.Id);
        var roots = new List<AccountDto>();

        foreach (var dto in dtos)
        {
            if (dto.ParentAccountId.HasValue && lookup.ContainsKey(dto.ParentAccountId.Value))
            {
                lookup[dto.ParentAccountId.Value].SubAccounts.Add(dto);
            }
            else
            {
                roots.Add(dto);
            }
        }

        // Ordenar por código
        return roots.OrderBy(x => x.Code);
    }
}
