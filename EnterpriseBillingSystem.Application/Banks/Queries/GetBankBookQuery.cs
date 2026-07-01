using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using EnterpriseBillingSystem.Application.Banks.DTOs;
using EnterpriseBillingSystem.Domain.Repositories;

namespace EnterpriseBillingSystem.Application.Banks.Queries;

public record GetBankBookQuery(
    Guid BankAccountId,
    DateTime StartDate,
    DateTime EndDate,
    int PageNumber = 1,
    int PageSize = 50
) : IRequest<IEnumerable<BankTransactionDto>>;

public class GetBankBookQueryHandler : IRequestHandler<GetBankBookQuery, IEnumerable<BankTransactionDto>>
{
    private readonly IBankAccountRepository _bankAccountRepository;
    private readonly IBankTransactionRepository _bankTransactionRepository;

    public GetBankBookQueryHandler(
        IBankAccountRepository bankAccountRepository,
        IBankTransactionRepository bankTransactionRepository)
    {
        _bankAccountRepository = bankAccountRepository;
        _bankTransactionRepository = bankTransactionRepository;
    }

    public async Task<IEnumerable<BankTransactionDto>> Handle(GetBankBookQuery request, CancellationToken cancellationToken)
    {
        var account = await _bankAccountRepository.GetByIdAsync(request.BankAccountId);
        if (account == null)
            throw new ArgumentException("La cuenta bancaria no existe.");

        var transactions = await _bankTransactionRepository.GetTransactionsByAccountAndPeriodAsync(
            request.BankAccountId,
            request.StartDate,
            request.EndDate,
            cancellationToken);

        return transactions
            .OrderBy(t => t.TransactionDate)
            .Skip((request.PageNumber - 1) * request.PageSize)
            .Take(request.PageSize)
            .Select(t => new BankTransactionDto(
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
    }
}
