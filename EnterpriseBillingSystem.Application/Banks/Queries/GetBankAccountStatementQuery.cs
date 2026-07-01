using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using EnterpriseBillingSystem.Application.Banks.DTOs;
using EnterpriseBillingSystem.Application.Common.Models;
using EnterpriseBillingSystem.Domain.Enums;
using EnterpriseBillingSystem.Domain.Repositories;

namespace EnterpriseBillingSystem.Application.Banks.Queries;

// Estado de cuenta: Saldo inicial + movimientos en el rango + saldo final
public record BankAccountStatementDto(
    Guid BankAccountId,
    string AccountNumber,
    string AccountName,
    string CurrencyCode,
    DateTime StartDate,
    DateTime EndDate,
    decimal OpeningBalance,
    decimal TotalDeposits,
    decimal TotalWithdrawals,
    decimal ClosingBalance,
    IEnumerable<BankTransactionDto> Transactions
);

public record GetBankAccountStatementQuery(
    Guid BankAccountId,
    DateTime StartDate,
    DateTime EndDate
) : IRequest<BankAccountStatementDto>;

public class GetBankAccountStatementQueryHandler : IRequestHandler<GetBankAccountStatementQuery, BankAccountStatementDto>
{
    private readonly IBankAccountRepository _bankAccountRepository;
    private readonly IBankTransactionRepository _bankTransactionRepository;

    public GetBankAccountStatementQueryHandler(
        IBankAccountRepository bankAccountRepository,
        IBankTransactionRepository bankTransactionRepository)
    {
        _bankAccountRepository = bankAccountRepository;
        _bankTransactionRepository = bankTransactionRepository;
    }

    public async Task<BankAccountStatementDto> Handle(GetBankAccountStatementQuery request, CancellationToken cancellationToken)
    {
        var account = await _bankAccountRepository.GetByIdAsync(request.BankAccountId);
        if (account == null)
            throw new ArgumentException("La cuenta bancaria no existe.");

        // Movimientos previos al rango para calcular saldo de apertura
        var allPrior = await _bankTransactionRepository.GetTransactionsByAccountAndPeriodAsync(
            request.BankAccountId, DateTime.MinValue, request.StartDate.AddDays(-1), cancellationToken);

        var openingBalance = allPrior.Sum(t =>
            t.TransactionType == BankTransactionType.Deposit ||
            t.TransactionType == BankTransactionType.InterestIncome ? t.Amount : -t.Amount);

        // Movimientos dentro del rango
        var transactions = await _bankTransactionRepository.GetTransactionsByAccountAndPeriodAsync(
            request.BankAccountId, request.StartDate, request.EndDate, cancellationToken);

        var txList = transactions.ToList();
        var totalDeposits = txList.Where(t =>
            t.TransactionType == BankTransactionType.Deposit ||
            t.TransactionType == BankTransactionType.InterestIncome).Sum(t => t.Amount);
        var totalWithdrawals = txList.Where(t =>
            t.TransactionType != BankTransactionType.Deposit &&
            t.TransactionType != BankTransactionType.InterestIncome).Sum(t => t.Amount);

        var txDtos = txList.Select(t => new BankTransactionDto(
            t.Id,
            t.BankAccountId,
            account.AccountNumber,
            t.TransactionDate,
            t.TransactionType,
            t.TransactionType.ToString(),
            t.Amount,
            t.ReferenceNumber,
            t.Description,
            t.RelatedBankAccountId
        ));

        return new BankAccountStatementDto(
            account.Id,
            account.AccountNumber,
            account.AccountName,
            account.CurrencyCode,
            request.StartDate,
            request.EndDate,
            openingBalance,
            totalDeposits,
            totalWithdrawals,
            openingBalance + totalDeposits - totalWithdrawals,
            txDtos
        );
    }
}
