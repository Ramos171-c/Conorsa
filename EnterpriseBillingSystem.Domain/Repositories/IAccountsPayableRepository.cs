using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using EnterpriseBillingSystem.Domain.Entities;

namespace EnterpriseBillingSystem.Domain.Repositories;

public interface IAccountsPayableRepository : IRepository<AccountsPayable>
{
    Task<AccountsPayable?> GetByIdWithDetailsAsync(Guid id, CancellationToken cancellationToken = default);
    Task<AccountsPayable?> GetByInvoiceIdAsync(Guid invoiceId, CancellationToken cancellationToken = default);
    Task<IEnumerable<AccountsPayable>> GetOverdueAccountsAsync(DateTime date, CancellationToken cancellationToken = default);
    Task<IEnumerable<AccountsPayable>> GetActiveBySupplierIdAsync(Guid supplierId, CancellationToken cancellationToken = default);
    Task<IEnumerable<AccountsPayable>> GetBySupplierIdWithPaymentsAsync(Guid supplierId, CancellationToken cancellationToken = default);
    Task<IEnumerable<AccountsPayable>> GetActiveWithSuppliersAsync(CancellationToken cancellationToken = default);
    Task<(IEnumerable<AccountsPayable> Items, int TotalCount)> GetPagedAsync(
        Guid? supplierId,
        string? status,
        DateTime? startDate,
        DateTime? endDate,
        bool? isOverdue,
        bool? isPending,
        int pageNumber,
        int pageSize,
        CancellationToken cancellationToken = default);
}
