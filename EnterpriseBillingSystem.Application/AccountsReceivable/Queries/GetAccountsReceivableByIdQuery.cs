using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using EnterpriseBillingSystem.Domain.Repositories;

namespace EnterpriseBillingSystem.Application.AccountsReceivable.Queries;

public record AccountsReceivablePaymentDto(
    Guid Id,
    Guid CashSessionId,
    string CashSessionNumber,
    Guid PaymentMethodId,
    string PaymentMethodName,
    DateTime PaymentDate,
    decimal Amount,
    string? ReferenceNumber,
    string? Notes
);

public record AccountsReceivableDetailDto(
    Guid Id,
    Guid CustomerId,
    string CustomerName,
    string CustomerCode,
    Guid SalesInvoiceId,
    string DocumentNumber,
    DateTime InvoiceDate,
    DateTime DueDate,
    decimal OriginalAmount,
    decimal PaidAmount,
    decimal CurrentBalance,
    string Status,
    DateTime? LastPaymentDate,
    string? Notes,
    List<AccountsReceivablePaymentDto> Payments
);

public record GetAccountsReceivableByIdQuery(Guid Id) : IRequest<AccountsReceivableDetailDto?>;

public class GetAccountsReceivableByIdQueryHandler : IRequestHandler<GetAccountsReceivableByIdQuery, AccountsReceivableDetailDto?>
{
    private readonly IAccountsReceivableRepository _arRepository;

    public GetAccountsReceivableByIdQueryHandler(IAccountsReceivableRepository arRepository)
    {
        _arRepository = arRepository;
    }

    public async Task<AccountsReceivableDetailDto?> Handle(GetAccountsReceivableByIdQuery request, CancellationToken cancellationToken)
    {
        var ar = await _arRepository.GetByIdWithDetailsAsync(request.Id, cancellationToken);
        if (ar == null) return null;

        var payments = ar.Payments
            .Where(p => !p.IsDeleted)
            .Select(p => new AccountsReceivablePaymentDto(
                p.Id,
                p.CashSessionId,
                p.CashSession?.SessionNumber ?? "N/A",
                p.PaymentMethodId,
                p.PaymentMethod?.Name ?? "N/A",
                p.PaymentDate,
                p.Amount,
                p.ReferenceNumber,
                p.Notes
            ))
            .OrderByDescending(p => p.PaymentDate)
            .ToList();

        return new AccountsReceivableDetailDto(
            ar.Id,
            ar.CustomerId,
            ar.Customer?.Name ?? "N/A",
            ar.Customer?.CustomerCode ?? "N/A",
            ar.SalesInvoiceId,
            ar.DocumentNumber,
            ar.InvoiceDate,
            ar.DueDate,
            ar.OriginalAmount,
            ar.PaidAmount,
            ar.CurrentBalance,
            ar.Status.ToString(),
            ar.LastPaymentDate,
            ar.Notes,
            payments
        );
    }
}
