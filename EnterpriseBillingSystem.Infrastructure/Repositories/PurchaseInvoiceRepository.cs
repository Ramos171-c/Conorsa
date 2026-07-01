using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using EnterpriseBillingSystem.Domain.Entities;
using EnterpriseBillingSystem.Domain.Repositories;
using EnterpriseBillingSystem.Infrastructure.Data;

namespace EnterpriseBillingSystem.Infrastructure.Repositories;

public class PurchaseInvoiceRepository : Repository<PurchaseInvoice>, IPurchaseInvoiceRepository
{
    public PurchaseInvoiceRepository(ApplicationDbContext context) : base(context)
    {
    }

    public async Task<PurchaseInvoice?> GetByIdWithDetailsAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _context.PurchaseInvoices
            .Include(pi => pi.Supplier)
            .Include(pi => pi.PurchaseReceipt)
            .Include(pi => pi.PurchaseOrder)
            .Include(pi => pi.Details)
                .ThenInclude(d => d.Product)
            .Include(pi => pi.Details)
                .ThenInclude(d => d.UnitOfMeasure)
            .FirstOrDefaultAsync(pi => pi.Id == id, cancellationToken);
    }

    public async Task<PurchaseInvoice?> GetByInternalNumberAsync(string internalNumber, CancellationToken cancellationToken = default)
    {
        return await _context.PurchaseInvoices
            .Include(pi => pi.Supplier)
            .FirstOrDefaultAsync(pi => pi.InternalInvoiceNumber == internalNumber, cancellationToken);
    }

    public async Task<string> GenerateInternalInvoiceNumberAsync(CancellationToken cancellationToken = default)
    {
        var maxNumber = await _context.PurchaseInvoices
            .IgnoreQueryFilters()
            .Select(pi => pi.InternalInvoiceNumber)
            .OrderByDescending(n => n)
            .FirstOrDefaultAsync(cancellationToken);

        int nextNumber = 1;
        if (!string.IsNullOrEmpty(maxNumber) && maxNumber.StartsWith("FC-"))
        {
            if (int.TryParse(maxNumber.Substring(3), out int lastNumber))
                nextNumber = lastNumber + 1;
        }

        return $"FC-{nextNumber:D8}";
    }

    public async Task<(IEnumerable<PurchaseInvoice> Items, int TotalCount)> GetPagedAsync(
        Guid? supplierId,
        string? status,
        DateTime? fromDate,
        DateTime? toDate,
        int pageNumber,
        int pageSize,
        CancellationToken cancellationToken = default)
    {
        var query = _context.PurchaseInvoices
            .Include(pi => pi.Supplier)
            .AsNoTracking();

        if (supplierId.HasValue)
            query = query.Where(pi => pi.SupplierId == supplierId.Value);

        if (!string.IsNullOrWhiteSpace(status))
            query = query.Where(pi => pi.Status.ToString() == status);

        if (fromDate.HasValue)
            query = query.Where(pi => pi.InvoiceDate >= fromDate.Value);

        if (toDate.HasValue)
            query = query.Where(pi => pi.InvoiceDate <= toDate.Value);

        int totalCount = await query.CountAsync(cancellationToken);

        var items = await query
            .OrderByDescending(pi => pi.InvoiceDate)
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        return (items, totalCount);
    }
}
