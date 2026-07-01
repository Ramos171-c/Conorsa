using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using EnterpriseBillingSystem.Domain.Enums;
using EnterpriseBillingSystem.Domain.Repositories;

namespace EnterpriseBillingSystem.Application.JournalEntries.Queries;

public record IncomeStatementItemDto(
    string AccountCode,
    string AccountName,
    decimal Amount
);

public record IncomeStatementResultDto(
    IEnumerable<IncomeStatementItemDto> Revenues,
    decimal TotalRevenues,
    IEnumerable<IncomeStatementItemDto> Costs,
    decimal TotalCosts,
    IEnumerable<IncomeStatementItemDto> Expenses,
    decimal TotalExpenses,
    decimal NetIncome
);

public record GetIncomeStatementQuery(
    DateTime StartDate,
    DateTime EndDate
) : IRequest<IncomeStatementResultDto>;

public class GetIncomeStatementQueryHandler : IRequestHandler<GetIncomeStatementQuery, IncomeStatementResultDto>
{
    private readonly IAccountRepository _accountRepository;
    private readonly IJournalEntryRepository _journalEntryRepository;

    public GetIncomeStatementQueryHandler(
        IAccountRepository accountRepository,
        IJournalEntryRepository journalEntryRepository)
    {
        _accountRepository = accountRepository;
        _journalEntryRepository = journalEntryRepository;
    }

    public async Task<IncomeStatementResultDto> Handle(GetIncomeStatementQuery request, CancellationToken cancellationToken)
    {
        // 1. Obtener todas las cuentas de movimiento activas
        var accountsList = await _accountRepository.FindAsync(a => a.IsActive && a.IsPostingAccount);
        var accounts = accountsList.ToList();

        // 2. Obtener movimientos del período
        var postedEntries = await _journalEntryRepository.GetPostedEntriesWithDetailsAsync(request.StartDate, request.EndDate, cancellationToken);
        var details = postedEntries.SelectMany(j => j.Details).ToList();

        var revenues = new List<IncomeStatementItemDto>();
        var costs = new List<IncomeStatementItemDto>();
        var expenses = new List<IncomeStatementItemDto>();

        foreach (var account in accounts)
        {
            var accountDetails = details.Where(d => d.AccountId == account.Id).ToList();
            if (!accountDetails.Any()) continue;

            var debits = accountDetails.Sum(d => d.DebitAmount);
            var credits = accountDetails.Sum(d => d.CreditAmount);

            if (account.AccountType == AccountType.Revenue)
            {
                // Ingresos: Naturaleza Acreedora
                var balance = credits - debits;
                if (balance != 0)
                {
                    revenues.Add(new IncomeStatementItemDto(account.Code, account.Name, balance));
                }
            }
            else if (account.AccountType == AccountType.Expense)
            {
                // Gastos o Costos: Naturaleza Deudora
                var balance = debits - credits;
                if (balance != 0)
                {
                    if (account.Code.StartsWith("5"))
                    {
                        costs.Add(new IncomeStatementItemDto(account.Code, account.Name, balance));
                    }
                    else
                    {
                        expenses.Add(new IncomeStatementItemDto(account.Code, account.Name, balance));
                    }
                }
            }
        }

        var totalRevenues = revenues.Sum(r => r.Amount);
        var totalCosts = costs.Sum(c => c.Amount);
        var totalExpenses = expenses.Sum(e => e.Amount);
        var netIncome = totalRevenues - totalCosts - totalExpenses;

        return new IncomeStatementResultDto(
            revenues.OrderBy(r => r.AccountCode),
            totalRevenues,
            costs.OrderBy(c => c.AccountCode),
            totalCosts,
            expenses.OrderBy(e => e.AccountCode),
            totalExpenses,
            netIncome
        );
    }
}
