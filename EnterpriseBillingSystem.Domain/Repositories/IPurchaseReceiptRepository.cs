using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using EnterpriseBillingSystem.Domain.Entities;

namespace EnterpriseBillingSystem.Domain.Repositories;

public interface IPurchaseReceiptRepository : IRepository<PurchaseReceipt>
{
    Task<PurchaseReceipt?> GetByIdWithDetailsAsync(Guid id, CancellationToken cancellationToken = default);
    Task<PurchaseReceipt?> GetByReceiptNumberAsync(string receiptNumber, CancellationToken cancellationToken = default);
    Task<string> GenerateReceiptNumberAsync(CancellationToken cancellationToken = default);
    Task<(IEnumerable<PurchaseReceipt> Items, int TotalCount)> GetPagedAsync(
        Guid? supplierId,
        Guid? purchaseOrderId,
        string? status,
        DateTime? fromDate,
        DateTime? toDate,
        int pageNumber,
        int pageSize,
        CancellationToken cancellationToken = default);
}
