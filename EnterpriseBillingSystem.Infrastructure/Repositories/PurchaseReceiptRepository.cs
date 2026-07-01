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

public class PurchaseReceiptRepository : Repository<PurchaseReceipt>, IPurchaseReceiptRepository
{
    public PurchaseReceiptRepository(ApplicationDbContext context) : base(context)
    {
    }

    public async Task<PurchaseReceipt?> GetByIdWithDetailsAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _context.PurchaseReceipts
            .Include(r => r.Supplier)
            .Include(r => r.PurchaseOrder)
            .Include(r => r.BranchWarehouse)
                .ThenInclude(bw => bw.Branch)
            .Include(r => r.BranchWarehouse)
                .ThenInclude(bw => bw.Warehouse)
            .Include(r => r.Details)
                .ThenInclude(d => d.Product)
            .Include(r => r.Details)
                .ThenInclude(d => d.UnitOfMeasure)
            .FirstOrDefaultAsync(r => r.Id == id, cancellationToken);
    }

    public async Task<PurchaseReceipt?> GetByReceiptNumberAsync(string receiptNumber, CancellationToken cancellationToken = default)
    {
        return await _context.PurchaseReceipts
            .Include(r => r.Supplier)
            .FirstOrDefaultAsync(r => r.ReceiptNumber == receiptNumber, cancellationToken);
    }

    public async Task<string> GenerateReceiptNumberAsync(CancellationToken cancellationToken = default)
    {
        var maxNumber = await _context.PurchaseReceipts
            .IgnoreQueryFilters()
            .Select(r => r.ReceiptNumber)
            .OrderByDescending(n => n)
            .FirstOrDefaultAsync(cancellationToken);

        int nextNumber = 1;
        if (!string.IsNullOrEmpty(maxNumber) && maxNumber.StartsWith("RC-"))
        {
            if (int.TryParse(maxNumber.Substring(3), out int lastNumber))
                nextNumber = lastNumber + 1;
        }

        return $"RC-{nextNumber:D8}";
    }

    public async Task<(IEnumerable<PurchaseReceipt> Items, int TotalCount)> GetPagedAsync(
        Guid? supplierId,
        Guid? purchaseOrderId,
        string? status,
        DateTime? fromDate,
        DateTime? toDate,
        int pageNumber,
        int pageSize,
        CancellationToken cancellationToken = default)
    {
        var query = _context.PurchaseReceipts
            .Include(r => r.Supplier)
            .Include(r => r.PurchaseOrder)
            .AsNoTracking();

        if (supplierId.HasValue)
            query = query.Where(r => r.SupplierId == supplierId.Value);

        if (purchaseOrderId.HasValue)
            query = query.Where(r => r.PurchaseOrderId == purchaseOrderId.Value);

        if (!string.IsNullOrWhiteSpace(status))
            query = query.Where(r => r.Status.ToString() == status);

        if (fromDate.HasValue)
            query = query.Where(r => r.ReceiptDate >= fromDate.Value);

        if (toDate.HasValue)
            query = query.Where(r => r.ReceiptDate <= toDate.Value);

        int totalCount = await query.CountAsync(cancellationToken);

        var items = await query
            .OrderByDescending(r => r.ReceiptDate)
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        return (items, totalCount);
    }
}
