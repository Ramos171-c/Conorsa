using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using EnterpriseBillingSystem.Domain.Entities;

namespace EnterpriseBillingSystem.Domain.Repositories;

public interface ISalesInvoiceRepository : IRepository<SalesInvoice>
{
    Task<SalesInvoice?> GetByIdWithDetailsAsync(Guid id, CancellationToken cancellationToken = default);
    Task<SalesInvoice?> GetByInvoiceNumberAsync(string invoiceNumber, CancellationToken cancellationToken = default);

    /// <summary>Genera el próximo número de factura en formato INV-yyyyMMdd-NNNNN.</summary>
    Task<string> GenerateInvoiceNumberAsync(CancellationToken cancellationToken = default);

    Task<(IEnumerable<SalesInvoice> Items, int TotalCount)> GetPagedAsync(
        Guid? customerId,
        string? status,
        bool? isCreditSale,
        DateTime? fromDate,
        DateTime? toDate,
        int pageNumber,
        int pageSize,
        CancellationToken cancellationToken = default);
}
