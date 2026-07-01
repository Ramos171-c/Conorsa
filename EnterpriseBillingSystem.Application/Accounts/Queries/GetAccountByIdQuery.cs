using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using EnterpriseBillingSystem.Domain.Repositories;

namespace EnterpriseBillingSystem.Application.Accounts.Queries;

public record GetAccountByIdQuery(Guid Id) : IRequest<AccountDto?>;

public class GetAccountByIdQueryHandler : IRequestHandler<GetAccountByIdQuery, AccountDto?>
{
    private readonly IAccountRepository _accountRepository;

    public GetAccountByIdQueryHandler(IAccountRepository accountRepository)
    {
        _accountRepository = accountRepository;
    }

    public async Task<AccountDto?> Handle(GetAccountByIdQuery request, CancellationToken cancellationToken)
    {
        var a = await _accountRepository.GetByIdAsync(request.Id);
        if (a == null) return null;

        return new AccountDto(
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
        );
    }
}
