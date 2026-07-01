using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using EnterpriseBillingSystem.Domain.Entities;

namespace EnterpriseBillingSystem.Domain.Repositories;

public interface IAccountsReceivableRepository : IRepository<AccountsReceivable>
{
    Task<AccountsReceivable?> GetByIdWithDetailsAsync(Guid id, CancellationToken cancellationToken = default);
    Task<AccountsReceivable?> GetByInvoiceIdAsync(Guid invoiceId, CancellationToken cancellationToken = default);
    Task<IEnumerable<AccountsReceivable>> GetOverdueAccountsAsync(DateTime date, CancellationToken cancellationToken = default);
    Task<IEnumerable<AccountsReceivable>> GetActiveByCustomerIdAsync(Guid customerId, CancellationToken cancellationToken = default);
    Task<(IEnumerable<AccountsReceivable> Items, int TotalCount)> GetPagedAsync(
        Guid? customerId,
        string? status,
        DateTime? startDate,
        DateTime? endDate,
        bool? isOverdue,
        bool? isPending,
        int pageNumber,
        int pageSize,
        CancellationToken cancellationToken = default);
}
