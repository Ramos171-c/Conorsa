using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using EnterpriseBillingSystem.Domain.Entities;
using EnterpriseBillingSystem.Domain.Enums;
using EnterpriseBillingSystem.Domain.Repositories;
using EnterpriseBillingSystem.Infrastructure.Data;

namespace EnterpriseBillingSystem.Infrastructure.Repositories;

public class AccountsReceivableRepository : Repository<AccountsReceivable>, IAccountsReceivableRepository
{
    public AccountsReceivableRepository(ApplicationDbContext context) : base(context)
    {
    }

    public async Task<AccountsReceivable?> GetByIdWithDetailsAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _context.AccountsReceivables
            .Include(a => a.Customer)
            .Include(a => a.SalesInvoice)
            .Include(a => a.Payments)
                .ThenInclude(p => p.PaymentMethod)
            .Include(a => a.Payments)
                .ThenInclude(p => p.CashSession)
            .FirstOrDefaultAsync(a => a.Id == id, cancellationToken);
    }

    public async Task<AccountsReceivable?> GetByInvoiceIdAsync(Guid invoiceId, CancellationToken cancellationToken = default)
    {
        return await _context.AccountsReceivables
            .Include(a => a.Customer)
            .Include(a => a.SalesInvoice)
            .FirstOrDefaultAsync(a => a.SalesInvoiceId == invoiceId, cancellationToken);
    }

    public async Task<IEnumerable<AccountsReceivable>> GetOverdueAccountsAsync(DateTime date, CancellationToken cancellationToken = default)
    {
        return await _context.AccountsReceivables
            .Where(a => (a.Status == AccountsReceivableStatus.Pending || a.Status == AccountsReceivableStatus.PartiallyPaid)
                        && a.DueDate.Date < date.Date
                        && a.CurrentBalance > 0)
            .ToListAsync(cancellationToken);
    }

    public async Task<IEnumerable<AccountsReceivable>> GetActiveByCustomerIdAsync(Guid customerId, CancellationToken cancellationToken = default)
    {
        return await _context.AccountsReceivables
            .Where(a => a.CustomerId == customerId
                        && (a.Status == AccountsReceivableStatus.Pending || a.Status == AccountsReceivableStatus.PartiallyPaid || a.Status == AccountsReceivableStatus.Overdue)
                        && a.CurrentBalance > 0)
            .ToListAsync(cancellationToken);
    }

    public async Task<(IEnumerable<AccountsReceivable> Items, int TotalCount)> GetPagedAsync(
        Guid? customerId,
        string? status,
        DateTime? startDate,
        DateTime? endDate,
        bool? isOverdue,
        bool? isPending,
        int pageNumber,
        int pageSize,
        CancellationToken cancellationToken = default)
    {
        var query = _context.AccountsReceivables
            .Include(a => a.Customer)
            .Include(a => a.SalesInvoice)
            .AsQueryable();

        if (customerId.HasValue)
        {
            query = query.Where(a => a.CustomerId == customerId.Value);
        }

        if (!string.IsNullOrWhiteSpace(status) && Enum.TryParse<AccountsReceivableStatus>(status, true, out var arStatus))
        {
            query = query.Where(a => a.Status == arStatus);
        }

        if (startDate.HasValue)
        {
            query = query.Where(a => a.InvoiceDate >= startDate.Value);
        }

        if (endDate.HasValue)
        {
            query = query.Where(a => a.InvoiceDate <= endDate.Value);
        }

        if (isOverdue.HasValue && isOverdue.Value)
        {
            query = query.Where(a => a.Status == AccountsReceivableStatus.Overdue);
        }

        if (isPending.HasValue && isPending.Value)
        {
            query = query.Where(a => a.CurrentBalance > 0 
                                     && a.Status != AccountsReceivableStatus.Cancelled 
                                     && a.Status != AccountsReceivableStatus.Paid);
        }

        int totalCount = await query.CountAsync(cancellationToken);

        var items = await query
            .OrderByDescending(a => a.InvoiceDate)
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        return (items, totalCount);
    }
}
