using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using EnterpriseBillingSystem.Domain.Repositories;

namespace EnterpriseBillingSystem.Application.AccountsPayable.Queries;

public record SupplierStatementSummaryDto(
    Guid SupplierId,
    string SupplierName,
    string SupplierCode,
    decimal TotalPurchased,
    decimal TotalPaid,
    decimal CurrentBalance
);

public record SupplierStatementInvoiceDto(
    Guid AccountsPayableId,
    Guid PurchaseInvoiceId,
    string DocumentNumber,
    DateTime InvoiceDate,
    DateTime DueDate,
    decimal OriginalAmount,
    decimal CurrentBalance,
    string Status
);

public record SupplierStatementPaymentDto(
    Guid PaymentId,
    string DocumentNumber,
    DateTime PaymentDate,
    string PaymentMethodName,
    string? ReferenceNumber,
    decimal Amount,
    string? Notes
);

public record SupplierStatementDto(
    SupplierStatementSummaryDto Summary,
    List<SupplierStatementInvoiceDto> Invoices,
    List<SupplierStatementPaymentDto> Payments
);

public record GetSupplierStatementQuery(Guid SupplierId) : IRequest<SupplierStatementDto?>;

public class GetSupplierStatementQueryHandler : IRequestHandler<GetSupplierStatementQuery, SupplierStatementDto?>
{
    private readonly IAccountsPayableRepository _apRepository;
    private readonly ISupplierRepository _supplierRepository;

    public GetSupplierStatementQueryHandler(
        IAccountsPayableRepository apRepository,
        ISupplierRepository supplierRepository)
    {
        _apRepository = apRepository;
        _supplierRepository = supplierRepository;
    }

    public async Task<SupplierStatementDto?> Handle(GetSupplierStatementQuery request, CancellationToken cancellationToken)
    {
        // 1. Obtener proveedor
        var supplier = await _supplierRepository.GetByIdAsync(request.SupplierId);
        if (supplier == null) return null;

        // 2. Obtener todas las cuentas por pagar del proveedor con Eager Loading en una sola consulta
        var apList = await _apRepository.GetBySupplierIdWithPaymentsAsync(request.SupplierId, cancellationToken);
        var activeApList = apList.ToList();

        // 3. Calcular consolidados de facturas no anuladas
        decimal totalPurchased = activeApList
            .Where(a => a.Status != Domain.Enums.AccountsPayableStatus.Cancelled)
            .Sum(a => a.OriginalAmount);

        decimal totalPaid = activeApList
            .Where(a => a.Status != Domain.Enums.AccountsPayableStatus.Cancelled)
            .Sum(a => a.PaidAmount);

        decimal currentBalance = activeApList
            .Where(a => a.Status != Domain.Enums.AccountsPayableStatus.Cancelled)
            .Sum(a => a.CurrentBalance);

        // 4. Mapear Facturas
        var invoicesDto = activeApList
            .Select(a => new SupplierStatementInvoiceDto(
                a.Id,
                a.PurchaseInvoiceId,
                a.DocumentNumber,
                a.InvoiceDate,
                a.DueDate,
                a.OriginalAmount,
                a.CurrentBalance,
                a.Status.ToString()
            ))
            .OrderByDescending(i => i.InvoiceDate)
            .ToList();

        // 5. Mapear Pagos sin problemas N+1
        var paymentsDto = activeApList
            .SelectMany(a => a.Payments.Where(p => !p.IsDeleted).Select(p => new SupplierStatementPaymentDto(
                p.Id,
                a.DocumentNumber,
                p.PaymentDate,
                p.PaymentMethod?.Name ?? "N/A",
                p.ReferenceNumber,
                p.Amount,
                p.Notes
            )))
            .OrderByDescending(p => p.PaymentDate)
            .ToList();

        var summary = new SupplierStatementSummaryDto(
            supplier.Id,
            supplier.Name,
            supplier.SupplierCode,
            totalPurchased,
            totalPaid,
            currentBalance
        );

        return new SupplierStatementDto(summary, invoicesDto, paymentsDto);
    }
}
