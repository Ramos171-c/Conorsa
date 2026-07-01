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

public class FixedAssetRepository : Repository<FixedAsset>, IFixedAssetRepository
{
    public FixedAssetRepository(ApplicationDbContext context) : base(context)
    {
    }

    public async Task<FixedAsset?> GetByIdWithDetailsAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _context.FixedAssets
            .Include(a => a.Category)
            .Include(a => a.PurchaseInvoice)
            .Include(a => a.Transactions.Where(t => !t.IsDeleted))
            .FirstOrDefaultAsync(a => a.Id == id, cancellationToken);
    }

    public async Task<FixedAsset?> GetByAssetNumberAsync(string assetNumber, CancellationToken cancellationToken = default)
    {
        return await _context.FixedAssets
            .Include(a => a.Category)
            .FirstOrDefaultAsync(a => a.AssetNumber == assetNumber, cancellationToken);
    }

    public async Task<(IEnumerable<FixedAsset> Items, int TotalCount)> GetPagedAsync(
        int pageNumber,
        int pageSize,
        Guid? categoryId,
        FixedAssetStatus? status,
        Guid? branchId,
        CancellationToken cancellationToken = default)
    {
        var query = _context.FixedAssets
            .Include(a => a.Category)
            .AsQueryable();

        if (categoryId.HasValue)
            query = query.Where(a => a.FixedAssetCategoryId == categoryId.Value);

        if (status.HasValue)
            query = query.Where(a => a.Status == status.Value);

        if (branchId.HasValue)
            query = query.Where(a => a.BranchId == branchId.Value);

        var totalCount = await query.CountAsync(cancellationToken);
        var items = await query
            .OrderByDescending(a => a.AcquisitionDate)
            .ThenBy(a => a.AssetNumber)
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        return (items, totalCount);
    }

    public async Task<IEnumerable<FixedAsset>> GetPendingDepreciationAsync(
        int year,
        int month,
        CancellationToken cancellationToken = default)
    {
        var periodStart = new DateTime(year, month, 1, 0, 0, 0, DateTimeKind.Utc);

        return await _context.FixedAssets
            .Include(a => a.Category)
            .Where(a =>
                (a.Status == FixedAssetStatus.Active || a.Status == FixedAssetStatus.Impaired) &&
                a.AccumulatedDepreciation < (a.AcquisitionCost - a.ResidualValue) &&
                a.DepreciationStartDate <= periodStart &&
                (a.LastDepreciationDate == null || a.LastDepreciationDate < periodStart))
            .ToListAsync(cancellationToken);
    }

    public async Task<string> GenerateAssetNumberAsync(CancellationToken cancellationToken = default)
    {
        var year = DateTime.UtcNow.Year;
        var prefix = $"ACT-{year}-";
        var count = await _context.FixedAssets
            .IgnoreQueryFilters()
            .CountAsync(a => a.AssetNumber.StartsWith(prefix), cancellationToken);
        return $"{prefix}{(count + 1):D4}";
    }
}
