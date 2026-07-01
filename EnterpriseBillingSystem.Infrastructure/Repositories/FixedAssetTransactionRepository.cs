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

public class FixedAssetTransactionRepository : Repository<FixedAssetTransaction>, IFixedAssetTransactionRepository
{
    public FixedAssetTransactionRepository(ApplicationDbContext context) : base(context)
    {
    }

    public async Task<IEnumerable<FixedAssetTransaction>> GetByAssetIdAsync(
        Guid fixedAssetId,
        CancellationToken cancellationToken = default)
    {
        return await _context.FixedAssetTransactions
            .Where(t => t.FixedAssetId == fixedAssetId)
            .OrderByDescending(t => t.TransactionDate)
            .ToListAsync(cancellationToken);
    }

    public async Task<IEnumerable<FixedAssetTransaction>> GetByAssetIdAndTypeAsync(
        Guid fixedAssetId,
        FixedAssetTransactionType transactionType,
        CancellationToken cancellationToken = default)
    {
        return await _context.FixedAssetTransactions
            .Where(t => t.FixedAssetId == fixedAssetId && t.TransactionType == transactionType)
            .OrderByDescending(t => t.TransactionDate)
            .ToListAsync(cancellationToken);
    }

    public async Task<IEnumerable<FixedAssetTransaction>> GetByPeriodAsync(
        int year,
        int month,
        FixedAssetTransactionType? transactionType = null,
        CancellationToken cancellationToken = default)
    {
        var start = new DateTime(year, month, 1, 0, 0, 0, DateTimeKind.Utc);
        var end = start.AddMonths(1);

        var query = _context.FixedAssetTransactions
            .Include(t => t.FixedAsset)
            .Where(t => t.TransactionDate >= start && t.TransactionDate < end);

        if (transactionType.HasValue)
            query = query.Where(t => t.TransactionType == transactionType.Value);

        return await query
            .OrderBy(t => t.TransactionDate)
            .ToListAsync(cancellationToken);
    }
}
