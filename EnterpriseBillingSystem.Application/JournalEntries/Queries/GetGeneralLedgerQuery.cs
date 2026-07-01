using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using EnterpriseBillingSystem.Domain.Entities;
using EnterpriseBillingSystem.Domain.Enums;
using EnterpriseBillingSystem.Domain.Repositories;

namespace EnterpriseBillingSystem.Application.JournalEntries.Queries;

public record GeneralLedgerItemDto(
    Guid AccountId,
    string AccountCode,
    string AccountName,
    string AccountType,
    string Nature,
    decimal InitialBalance,
    decimal PeriodDebits,
    decimal PeriodCredits,
    decimal FinalBalance,
    bool IsPostingAccount
);

public record GetGeneralLedgerQuery(
    DateTime StartDate,
    DateTime EndDate,
    string? AccountCode = null
) : IRequest<IEnumerable<GeneralLedgerItemDto>>;

public class GetGeneralLedgerQueryHandler : IRequestHandler<GetGeneralLedgerQuery, IEnumerable<GeneralLedgerItemDto>>
{
    private readonly IAccountRepository _accountRepository;
    private readonly IJournalEntryRepository _journalEntryRepository;

    public GetGeneralLedgerQueryHandler(
        IAccountRepository accountRepository,
        IJournalEntryRepository journalEntryRepository)
    {
        _accountRepository = accountRepository;
        _journalEntryRepository = journalEntryRepository;
    }

    public async Task<IEnumerable<GeneralLedgerItemDto>> Handle(GetGeneralLedgerQuery request, CancellationToken cancellationToken)
    {
        // 1. Obtener todas las cuentas activas
        var accountsList = await _accountRepository.FindAsync(a => a.IsActive);
        var accounts = accountsList.ToList();

        // 2. Obtener todos los detalles de asientos contabilizados (Posted) hasta la fecha fin
        var postedEntries = await _journalEntryRepository.GetPostedEntriesWithDetailsAsync(null, request.EndDate, cancellationToken);
        var details = postedEntries.SelectMany(j => j.Details).ToList();

        var ledgerList = new List<GeneralLedgerItemDto>();

        // 3. Calcular para cada cuenta de movimiento (Posting Account)
        foreach (var account in accounts)
        {
            if (account.IsPostingAccount)
            {
                var accountDetails = details.Where(d => d.AccountId == account.Id).ToList();

                var initialDebits = accountDetails.Where(d => d.JournalEntry.EntryDate < request.StartDate).Sum(d => d.DebitAmount);
                var initialCredits = accountDetails.Where(d => d.JournalEntry.EntryDate < request.StartDate).Sum(d => d.CreditAmount);

                var periodDebits = accountDetails.Where(d => d.JournalEntry.EntryDate >= request.StartDate && d.JournalEntry.EntryDate <= request.EndDate).Sum(d => d.DebitAmount);
                var periodCredits = accountDetails.Where(d => d.JournalEntry.EntryDate >= request.StartDate && d.JournalEntry.EntryDate <= request.EndDate).Sum(d => d.CreditAmount);

                decimal initialBalance = 0;
                decimal finalBalance = 0;

                if (account.Nature == AccountNature.Debit)
                {
                    initialBalance = initialDebits - initialCredits;
                    finalBalance = initialBalance + periodDebits - periodCredits;
                }
                else
                {
                    initialBalance = initialCredits - initialDebits;
                    finalBalance = initialBalance + periodCredits - periodDebits;
                }

                ledgerList.Add(new GeneralLedgerItemDto(
                    account.Id,
                    account.Code,
                    account.Name,
                    account.AccountType.ToString(),
                    account.Nature.ToString(),
                    initialBalance,
                    periodDebits,
                    periodCredits,
                    finalBalance,
                    true
                ));
            }
        }

        // 4. Agregar cuentas acumuladoras (Parent Accounts) y consolidar hacia arriba
        var postingLookup = ledgerList.ToDictionary(x => x.AccountId);
        var allAccountLookup = accounts.ToDictionary(x => x.Id);

        foreach (var account in accounts.Where(a => !a.IsPostingAccount).OrderByDescending(a => a.Level))
        {
            // Encontrar todos los descendientes directos e indirectos
            var descendants = GetDescendants(account.Id, accounts);
            var postingDescendants = ledgerList.Where(l => descendants.Contains(l.AccountId)).ToList();

            var initialBalance = postingDescendants.Sum(d => d.InitialBalance);
            var periodDebits = postingDescendants.Sum(d => d.PeriodDebits);
            var periodCredits = postingDescendants.Sum(d => d.PeriodCredits);
            var finalBalance = postingDescendants.Sum(d => d.FinalBalance);

            ledgerList.Add(new GeneralLedgerItemDto(
                account.Id,
                account.Code,
                account.Name,
                account.AccountType.ToString(),
                account.Nature.ToString(),
                initialBalance,
                periodDebits,
                periodCredits,
                finalBalance,
                false
            ));
        }

        // 5. Filtrar por AccountCode si se solicita
        if (!string.IsNullOrWhiteSpace(request.AccountCode))
        {
            var targetAccount = accounts.FirstOrDefault(a => a.Code == request.AccountCode);
            if (targetAccount != null)
            {
                var descendants = GetDescendants(targetAccount.Id, accounts);
                descendants.Add(targetAccount.Id);
                return ledgerList.Where(l => descendants.Contains(l.AccountId)).OrderBy(l => l.AccountCode);
            }
            return Enumerable.Empty<GeneralLedgerItemDto>();
        }

        return ledgerList.OrderBy(l => l.AccountCode);
    }

    private List<Guid> GetDescendants(Guid parentId, List<Account> accounts)
    {
        var list = new List<Guid>();
        var children = accounts.Where(a => a.ParentAccountId == parentId).Select(a => a.Id).ToList();
        list.AddRange(children);

        foreach (var childId in children)
        {
            list.AddRange(GetDescendants(childId, accounts));
        }

        return list;
    }
}
