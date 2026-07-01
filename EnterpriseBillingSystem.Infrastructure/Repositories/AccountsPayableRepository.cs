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

public class AccountsPayableRepository : Repository<AccountsPayable>, IAccountsPayableRepository
{
    public AccountsPayableRepository(ApplicationDbContext context) : base(context)
    {
    }

    public async Task<AccountsPayable?> GetByIdWithDetailsAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _context.AccountsPayables
            .Include(a => a.Supplier)
            .Include(a => a.PurchaseInvoice)
            .Include(a => a.Payments)
                .ThenInclude(p => p.PaymentMethod)
            .Include(a => a.Payments)
                .ThenInclude(p => p.CashSession)
            .FirstOrDefaultAsync(a => a.Id == id, cancellationToken);
    }

    public async Task<AccountsPayable?> GetByInvoiceIdAsync(Guid invoiceId, CancellationToken cancellationToken = default)
    {
        return await _context.AccountsPayables
            .Include(a => a.Supplier)
            .Include(a => a.PurchaseInvoice)
            .FirstOrDefaultAsync(a => a.PurchaseInvoiceId == invoiceId, cancellationToken);
    }

    public async Task<IEnumerable<AccountsPayable>> GetOverdueAccountsAsync(DateTime date, CancellationToken cancellationToken = default)
    {
        return await _context.AccountsPayables
            .Where(a => (a.Status == AccountsPayableStatus.Pending || a.Status == AccountsPayableStatus.PartiallyPaid)
                        && a.DueDate.Date < date.Date
                        && a.CurrentBalance > 0)
            .ToListAsync(cancellationToken);
    }

    public async Task<IEnumerable<AccountsPayable>> GetActiveBySupplierIdAsync(Guid supplierId, CancellationToken cancellationToken = default)
    {
        return await _context.AccountsPayables
            .Where(a => a.SupplierId == supplierId
                        && (a.Status == AccountsPayableStatus.Pending || a.Status == AccountsPayableStatus.PartiallyPaid || a.Status == AccountsPayableStatus.Overdue)
                        && a.CurrentBalance > 0)
            .ToListAsync(cancellationToken);
    }

    public async Task<IEnumerable<AccountsPayable>> GetBySupplierIdWithPaymentsAsync(Guid supplierId, CancellationToken cancellationToken = default)
    {
        return await _context.AccountsPayables
            .Include(a => a.Supplier)
            .Include(a => a.PurchaseInvoice)
            .Include(a => a.Payments)
                .ThenInclude(p => p.PaymentMethod)
            .Where(a => a.SupplierId == supplierId && !a.IsDeleted)
            .ToListAsync(cancellationToken);
    }

    public async Task<IEnumerable<AccountsPayable>> GetActiveWithSuppliersAsync(CancellationToken cancellationToken = default)
    {
        return await _context.AccountsPayables
            .Include(a => a.Supplier)
            .Where(a => a.CurrentBalance > 0 
                        && a.Status != AccountsPayableStatus.Paid 
                        && a.Status != AccountsPayableStatus.Cancelled 
                        && !a.IsDeleted)
            .ToListAsync(cancellationToken);
    }

    public async Task<(IEnumerable<AccountsPayable> Items, int TotalCount)> GetPagedAsync(
        Guid? supplierId,
        string? status,
        DateTime? startDate,
        DateTime? endDate,
        bool? isOverdue,
        bool? isPending,
        int pageNumber,
        int pageSize,
        CancellationToken cancellationToken = default)
    {
        var query = _context.AccountsPayables
            .Include(a => a.Supplier)
            .Include(a => a.PurchaseInvoice)
            .AsQueryable();

        if (supplierId.HasValue)
        {
            query = query.Where(a => a.SupplierId == supplierId.Value);
        }

        if (!string.IsNullOrWhiteSpace(status) && Enum.TryParse<AccountsPayableStatus>(status, true, out var apStatus))
        {
            query = query.Where(a => a.Status == apStatus);
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
            query = query.Where(a => a.Status == AccountsPayableStatus.Overdue);
        }

        if (isPending.HasValue && isPending.Value)
        {
            query = query.Where(a => a.CurrentBalance > 0 
                                     && a.Status != AccountsPayableStatus.Cancelled 
                                     && a.Status != AccountsPayableStatus.Paid);
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
