using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using EnterpriseBillingSystem.Application.Banks.DTOs;
using EnterpriseBillingSystem.Domain.Repositories;

namespace EnterpriseBillingSystem.Application.Banks.Queries;

public record GetBankReconciliationsQuery(
    Guid BankAccountId
) : IRequest<IEnumerable<BankReconciliationDto>>;

public class GetBankReconciliationsQueryHandler : IRequestHandler<GetBankReconciliationsQuery, IEnumerable<BankReconciliationDto>>
{
    private readonly IBankReconciliationRepository _bankReconciliationRepository;
    private readonly IBankAccountRepository _bankAccountRepository;

    public GetBankReconciliationsQueryHandler(
        IBankReconciliationRepository bankReconciliationRepository,
        IBankAccountRepository bankAccountRepository)
    {
        _bankReconciliationRepository = bankReconciliationRepository;
        _bankAccountRepository = bankAccountRepository;
    }

    public async Task<IEnumerable<BankReconciliationDto>> Handle(GetBankReconciliationsQuery request, CancellationToken cancellationToken)
    {
        var account = await _bankAccountRepository.GetByIdAsync(request.BankAccountId);
        if (account == null)
            throw new ArgumentException("La cuenta bancaria no existe.");

        var reconciliations = await _bankReconciliationRepository.FindAsync(r => r.BankAccountId == request.BankAccountId);

        return reconciliations
            .OrderByDescending(r => r.StatementDate)
            .Select(r => new BankReconciliationDto(
                r.Id,
                r.BankAccountId,
                account.AccountNumber,
                r.StatementDate,
                r.StatementBalance,
                r.SystemBalance,
                r.Difference,
                r.Notes,
                r.IsClosed
            ));
    }
}
