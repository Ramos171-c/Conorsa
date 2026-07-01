using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using EnterpriseBillingSystem.Domain.Entities;

namespace EnterpriseBillingSystem.Domain.Repositories;

public interface IPurchaseInvoiceRepository : IRepository<PurchaseInvoice>
{
    Task<PurchaseInvoice?> GetByIdWithDetailsAsync(Guid id, CancellationToken cancellationToken = default);
    Task<PurchaseInvoice?> GetByInternalNumberAsync(string internalNumber, CancellationToken cancellationToken = default);
    Task<string> GenerateInternalInvoiceNumberAsync(CancellationToken cancellationToken = default);
    Task<(IEnumerable<PurchaseInvoice> Items, int TotalCount)> GetPagedAsync(
        Guid? supplierId,
        string? status,
        DateTime? fromDate,
        DateTime? toDate,
        int pageNumber,
        int pageSize,
        CancellationToken cancellationToken = default);
}
