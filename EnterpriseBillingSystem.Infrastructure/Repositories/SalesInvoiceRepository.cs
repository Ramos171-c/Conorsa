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

public class SalesInvoiceRepository : Repository<SalesInvoice>, ISalesInvoiceRepository
{
    public SalesInvoiceRepository(ApplicationDbContext context) : base(context)
    {
    }

    public async Task<SalesInvoice?> GetByIdWithDetailsAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _context.SalesInvoices
            .Include(si => si.Customer)
            .Include(si => si.BranchWarehouse)
                .ThenInclude(bw => bw.Branch)
            .Include(si => si.BranchWarehouse)
                .ThenInclude(bw => bw.Warehouse)
            .Include(si => si.SalesOrder)
            .Include(si => si.Details)
                .ThenInclude(d => d.Product)
            .Include(si => si.Details)
                .ThenInclude(d => d.UnitOfMeasure)
            .FirstOrDefaultAsync(si => si.Id == id, cancellationToken);
    }

    public async Task<SalesInvoice?> GetByInvoiceNumberAsync(string invoiceNumber, CancellationToken cancellationToken = default)
    {
        return await _context.SalesInvoices
            .Include(si => si.Customer)
            .FirstOrDefaultAsync(si => si.InvoiceNumber == invoiceNumber, cancellationToken);
    }

    public async Task<string> GenerateInvoiceNumberAsync(CancellationToken cancellationToken = default)
    {
        // Formato: INV-yyyyMMdd-NNNNN (contador diario)
        var today = DateTime.UtcNow.Date;
        var prefix = $"INV-{today:yyyyMMdd}-";

        var lastToday = await _context.SalesInvoices
            .IgnoreQueryFilters()
            .Where(si => si.InvoiceNumber.StartsWith(prefix))
            .Select(si => si.InvoiceNumber)
            .OrderByDescending(n => n)
            .FirstOrDefaultAsync(cancellationToken);

        int nextSeq = 1;
        if (!string.IsNullOrEmpty(lastToday))
        {
            var seqPart = lastToday.Substring(prefix.Length);
            if (int.TryParse(seqPart, out int lastSeq))
                nextSeq = lastSeq + 1;
        }

        return $"{prefix}{nextSeq:D5}";
    }

    public async Task<(IEnumerable<SalesInvoice> Items, int TotalCount)> GetPagedAsync(
        Guid? customerId,
        string? status,
        bool? isCreditSale,
        DateTime? fromDate,
        DateTime? toDate,
        int pageNumber,
        int pageSize,
        CancellationToken cancellationToken = default)
    {
        var query = _context.SalesInvoices
            .Include(si => si.Customer)
            .AsNoTracking();

        if (customerId.HasValue)
            query = query.Where(si => si.CustomerId == customerId.Value);

        if (!string.IsNullOrWhiteSpace(status) && Enum.TryParse<SalesInvoiceStatus>(status, true, out var statusEnum))
            query = query.Where(si => si.Status == statusEnum);

        if (isCreditSale.HasValue)
            query = query.Where(si => si.IsCreditSale == isCreditSale.Value);

        if (fromDate.HasValue)
            query = query.Where(si => si.InvoiceDate >= fromDate.Value);

        if (toDate.HasValue)
            query = query.Where(si => si.InvoiceDate <= toDate.Value);

        int totalCount = await query.CountAsync(cancellationToken);

        var items = await query
            .OrderByDescending(si => si.InvoiceDate)
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        return (items, totalCount);
    }
}
