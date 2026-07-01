using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using EnterpriseBillingSystem.Domain.Enums;
using EnterpriseBillingSystem.Domain.Repositories;

namespace EnterpriseBillingSystem.Application.JournalEntries.Queries;

public record TrialBalanceItemDto(
    string AccountCode,
    string AccountName,
    decimal DebitAmount,
    decimal CreditAmount
);

public record TrialBalanceResultDto(
    IEnumerable<TrialBalanceItemDto> Items,
    decimal TotalDebits,
    decimal TotalCredits,
    decimal Difference,
    bool IsBalanced
);

public record GetTrialBalanceQuery(
    DateTime StartDate,
    DateTime EndDate
) : IRequest<TrialBalanceResultDto>;

public class GetTrialBalanceQueryHandler : IRequestHandler<GetTrialBalanceQuery, TrialBalanceResultDto>
{
    private readonly IAccountRepository _accountRepository;
    private readonly IJournalEntryRepository _journalEntryRepository;

    public GetTrialBalanceQueryHandler(
        IAccountRepository accountRepository,
        IJournalEntryRepository journalEntryRepository)
    {
        _accountRepository = accountRepository;
        _journalEntryRepository = journalEntryRepository;
    }

    public async Task<TrialBalanceResultDto> Handle(GetTrialBalanceQuery request, CancellationToken cancellationToken)
    {
        // 1. Obtener cuentas de movimiento (Posting Accounts) activas
        var accountsList = await _accountRepository.FindAsync(a => a.IsActive && a.IsPostingAccount);
        var accounts = accountsList.ToList();

        // 2. Obtener movimientos del período
        var postedEntries = await _journalEntryRepository.GetPostedEntriesWithDetailsAsync(request.StartDate, request.EndDate, cancellationToken);
        var details = postedEntries.SelectMany(j => j.Details).ToList();

        var itemsList = new List<TrialBalanceItemDto>();

        foreach (var account in accounts)
        {
            var accountDetails = details.Where(d => d.AccountId == account.Id).ToList();
            var debits = accountDetails.Sum(d => d.DebitAmount);
            var credits = accountDetails.Sum(d => d.CreditAmount);

            // Solo mostrar cuentas que tengan movimientos o saldo
            if (debits > 0 || credits > 0)
            {
                itemsList.Add(new TrialBalanceItemDto(
                    account.Code,
                    account.Name,
                    debits,
                    credits
                ));
            }
        }

        var totalDebits = itemsList.Sum(i => i.DebitAmount);
        var totalCredits = itemsList.Sum(i => i.CreditAmount);
        var difference = Math.Abs(totalDebits - totalCredits);
        var isBalanced = difference < 0.0001m; // Tolerancia para decimales

        return new TrialBalanceResultDto(
            itemsList.OrderBy(i => i.AccountCode),
            totalDebits,
            totalCredits,
            difference,
            isBalanced
        );
    }
}
