using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using EnterpriseBillingSystem.Domain.Repositories;

namespace EnterpriseBillingSystem.Application.AccountsReceivable.Queries;

public record CustomerStatementSummaryDto(
    Guid CustomerId,
    string CustomerName,
    string CustomerCode,
    decimal CreditLimit,
    decimal TotalCreditGranted,
    decimal TotalPaid,
    decimal CurrentBalance
);

public record CustomerStatementInvoiceDto(
    Guid AccountsReceivableId,
    Guid SalesInvoiceId,
    string DocumentNumber,
    DateTime InvoiceDate,
    DateTime DueDate,
    decimal OriginalAmount,
    decimal CurrentBalance,
    string Status
);

public record CustomerStatementPaymentDto(
    Guid PaymentId,
    string DocumentNumber,
    DateTime PaymentDate,
    string PaymentMethodName,
    string? ReferenceNumber,
    decimal Amount,
    string? Notes
);

public record CustomerStatementDto(
    CustomerStatementSummaryDto Summary,
    List<CustomerStatementInvoiceDto> Invoices,
    List<CustomerStatementPaymentDto> Payments
);

public record GetCustomerStatementQuery(Guid CustomerId) : IRequest<CustomerStatementDto?>;

public class GetCustomerStatementQueryHandler : IRequestHandler<GetCustomerStatementQuery, CustomerStatementDto?>
{
    private readonly IAccountsReceivableRepository _arRepository;
    private readonly ICustomerRepository _customerRepository;

    public GetCustomerStatementQueryHandler(
        IAccountsReceivableRepository arRepository,
        ICustomerRepository customerRepository)
    {
        _arRepository = arRepository;
        _customerRepository = customerRepository;
    }

    public async Task<CustomerStatementDto?> Handle(GetCustomerStatementQuery request, CancellationToken cancellationToken)
    {
        // 1. Obtener cliente
        var customer = await _customerRepository.GetByIdAsync(request.CustomerId);
        if (customer == null) return null;

        // 2. Obtener todas las cuentas por cobrar del cliente
        var allArs = await _arRepository.FindAsync(a => a.CustomerId == request.CustomerId);
        var activeArsList = allArs.Where(a => !a.IsDeleted).ToList();

        // 3. Cargar detalles para cada una (o usar eager loading en la consulta)
        // Como el repositorio genérico FindAsync no hace includes, cargamos con detalles de forma manual o cargando los abonos.
        // Cargamos todas las CxC con detalles llamando a GetByIdWithDetailsAsync
        var arDetailsList = new List<Domain.Entities.AccountsReceivable>();
        foreach (var arItem in activeArsList)
        {
            var detail = await _arRepository.GetByIdWithDetailsAsync(arItem.Id, cancellationToken);
            if (detail != null)
            {
                arDetailsList.Add(detail);
            }
        }

        // 4. Calcular consolidados
        decimal totalCreditGranted = arDetailsList
            .Where(a => a.Status != Domain.Enums.AccountsReceivableStatus.Cancelled)
            .Sum(a => a.OriginalAmount);

        decimal totalPaid = arDetailsList
            .Where(a => a.Status != Domain.Enums.AccountsReceivableStatus.Cancelled)
            .Sum(a => a.PaidAmount);

        decimal currentBalance = arDetailsList
            .Where(a => a.Status != Domain.Enums.AccountsReceivableStatus.Cancelled)
            .Sum(a => a.CurrentBalance);

        // 5. Mapear Facturas
        var invoicesDto = arDetailsList
            .Select(a => new CustomerStatementInvoiceDto(
                a.Id,
                a.SalesInvoiceId,
                a.DocumentNumber,
                a.InvoiceDate,
                a.DueDate,
                a.OriginalAmount,
                a.CurrentBalance,
                a.Status.ToString()
            ))
            .OrderByDescending(i => i.InvoiceDate)
            .ToList();

        // 6. Mapear Pagos
        var paymentsDto = arDetailsList
            .SelectMany(a => a.Payments.Where(p => !p.IsDeleted).Select(p => new CustomerStatementPaymentDto(
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

        var summary = new CustomerStatementSummaryDto(
            customer.Id,
            customer.Name,
            customer.CustomerCode,
            customer.CreditLimit,
            totalCreditGranted,
            totalPaid,
            currentBalance
        );

        return new CustomerStatementDto(summary, invoicesDto, paymentsDto);
    }
}
