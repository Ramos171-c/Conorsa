using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using EnterpriseBillingSystem.Domain.Enums;
using EnterpriseBillingSystem.Domain.Repositories;

namespace EnterpriseBillingSystem.Application.JournalEntries.Queries;

public record BalanceSheetItemDto(
    string AccountCode,
    string AccountName,
    decimal Amount
);

public record BalanceSheetResultDto(
    IEnumerable<BalanceSheetItemDto> Assets,
    decimal TotalAssets,
    IEnumerable<BalanceSheetItemDto> Liabilities,
    decimal TotalLiabilities,
    IEnumerable<BalanceSheetItemDto> Equity,
    decimal TotalEquity,
    decimal NetIncome,
    decimal TotalLiabilitiesEquityAndIncome,
    bool IsBalanced
);

public record GetBalanceSheetQuery(
    DateTime EndDate
) : IRequest<BalanceSheetResultDto>;

public class GetBalanceSheetQueryHandler : IRequestHandler<GetBalanceSheetQuery, BalanceSheetResultDto>
{
    private readonly IAccountRepository _accountRepository;
    private readonly IJournalEntryRepository _journalEntryRepository;

    public GetBalanceSheetQueryHandler(
        IAccountRepository accountRepository,
        IJournalEntryRepository journalEntryRepository)
    {
        _accountRepository = accountRepository;
        _journalEntryRepository = journalEntryRepository;
    }

    public async Task<BalanceSheetResultDto> Handle(GetBalanceSheetQuery request, CancellationToken cancellationToken)
    {
        // 1. Obtener todas las cuentas de movimiento activas
        var accountsList = await _accountRepository.FindAsync(a => a.IsActive && a.IsPostingAccount);
        var accounts = accountsList.ToList();

        // 2. Obtener todos los movimientos acumulados hasta EndDate
        var postedEntries = await _journalEntryRepository.GetPostedEntriesWithDetailsAsync(null, request.EndDate, cancellationToken);
        var details = postedEntries.SelectMany(j => j.Details).ToList();

        var assets = new List<BalanceSheetItemDto>();
        var liabilities = new List<BalanceSheetItemDto>();
        var equity = new List<BalanceSheetItemDto>();

        decimal totalRevenues = 0;
        decimal totalCosts = 0;
        decimal totalExpenses = 0;

        foreach (var account in accounts)
        {
            var accountDetails = details.Where(d => d.AccountId == account.Id).ToList();
            if (!accountDetails.Any()) continue;

            var debits = accountDetails.Sum(d => d.DebitAmount);
            var credits = accountDetails.Sum(d => d.CreditAmount);

            switch (account.AccountType)
            {
                case AccountType.Asset:
                    // Activos: Deudora
                    var assetBalance = debits - credits;
                    if (assetBalance != 0)
                        assets.Add(new BalanceSheetItemDto(account.Code, account.Name, assetBalance));
                    break;

                case AccountType.Liability:
                    // Pasivos: Acreedora
                    var liabilityBalance = credits - debits;
                    if (liabilityBalance != 0)
                        liabilities.Add(new BalanceSheetItemDto(account.Code, account.Name, liabilityBalance));
                    break;

                case AccountType.Equity:
                    // Patrimonio: Acreedora
                    var equityBalance = credits - debits;
                    if (equityBalance != 0)
                        equity.Add(new BalanceSheetItemDto(account.Code, account.Name, equityBalance));
                    break;

                case AccountType.Revenue:
                    // Ingresos acumulados (para utilidad neta)
                    totalRevenues += (credits - debits);
                    break;

                case AccountType.Expense:
                    // Costos y Gastos acumulados (para utilidad neta)
                    var expBalance = debits - credits;
                    if (account.Code.StartsWith("5"))
                    {
                        totalCosts += expBalance;
                    }
                    else
                    {
                        totalExpenses += expBalance;
                    }
                    break;
            }
        }

        var totalAssets = assets.Sum(a => a.Amount);
        var totalLiabilities = liabilities.Sum(l => l.Amount);
        var totalEquity = equity.Sum(e => e.Amount);
        var netIncome = totalRevenues - totalCosts - totalExpenses;

        var totalLiabilitiesEquityAndIncome = totalLiabilities + totalEquity + netIncome;
        var difference = Math.Abs(totalAssets - totalLiabilitiesEquityAndIncome);
        var isBalanced = difference < 0.0001m; // Tolerancia de redondeo

        return new BalanceSheetResultDto(
            assets.OrderBy(a => a.AccountCode),
            totalAssets,
            liabilities.OrderBy(l => l.AccountCode),
            totalLiabilities,
            equity.OrderBy(e => e.AccountCode),
            totalEquity,
            netIncome,
            totalLiabilitiesEquityAndIncome,
            isBalanced
        );
    }
}
