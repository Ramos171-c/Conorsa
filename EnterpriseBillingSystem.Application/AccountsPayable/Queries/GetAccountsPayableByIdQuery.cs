using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using EnterpriseBillingSystem.Domain.Repositories;

namespace EnterpriseBillingSystem.Application.AccountsPayable.Queries;

public record AccountsPayablePaymentDto(
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

public record AccountsPayableDetailDto(
    Guid Id,
    Guid SupplierId,
    string SupplierName,
    string SupplierCode,
    Guid PurchaseInvoiceId,
    string DocumentNumber,
    DateTime InvoiceDate,
    DateTime DueDate,
    decimal OriginalAmount,
    decimal PaidAmount,
    decimal CurrentBalance,
    string Status,
    DateTime? LastPaymentDate,
    string? Notes,
    List<AccountsPayablePaymentDto> Payments
);

public record GetAccountsPayableByIdQuery(Guid Id) : IRequest<AccountsPayableDetailDto?>;

public class GetAccountsPayableByIdQueryHandler : IRequestHandler<GetAccountsPayableByIdQuery, AccountsPayableDetailDto?>
{
    private readonly IAccountsPayableRepository _apRepository;

    public GetAccountsPayableByIdQueryHandler(IAccountsPayableRepository apRepository)
    {
        _apRepository = apRepository;
    }

    public async Task<AccountsPayableDetailDto?> Handle(GetAccountsPayableByIdQuery request, CancellationToken cancellationToken)
    {
        var ap = await _apRepository.GetByIdWithDetailsAsync(request.Id, cancellationToken);
        if (ap == null) return null;

        var payments = ap.Payments
            .Where(p => !p.IsDeleted)
            .Select(p => new AccountsPayablePaymentDto(
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

        return new AccountsPayableDetailDto(
            ap.Id,
            ap.SupplierId,
            ap.Supplier?.Name ?? "N/A",
            ap.Supplier?.SupplierCode ?? "N/A",
            ap.PurchaseInvoiceId,
            ap.DocumentNumber,
            ap.InvoiceDate,
            ap.DueDate,
            ap.OriginalAmount,
            ap.PaidAmount,
            ap.CurrentBalance,
            ap.Status.ToString(),
            ap.LastPaymentDate,
            ap.Notes,
            payments
        );
    }
}
