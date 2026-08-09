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

public class RouteLiquidationRepository : Repository<RouteLiquidation>, IRouteLiquidationRepository
{
    public RouteLiquidationRepository(ApplicationDbContext context) : base(context)
    {
    }

    public async Task<RouteLiquidation?> GetByIdWithDetailsAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _context.RouteLiquidations
            .Include(rl => rl.Route)
            .Include(rl => rl.Details)
                .ThenInclude(d => d.Product)
            .Include(rl => rl.Details)
                .ThenInclude(d => d.UnitOfMeasure)
            .Include(rl => rl.Details)
                .ThenInclude(d => d.ProductPresentation)
            .AsNoTracking()
            .FirstOrDefaultAsync(rl => rl.Id == id, cancellationToken);
    }

    public async Task<(IEnumerable<RouteLiquidation> Items, int TotalCount)> GetPagedAsync(
        DateTime? fromDate,
        DateTime? toDate,
        Guid? routeId,
        string? status,
        int pageNumber,
        int pageSize,
        CancellationToken cancellationToken = default)
    {
        var query = _context.RouteLiquidations
            .Include(rl => rl.Route)
            .AsNoTracking();

        if (fromDate.HasValue)
        {
            query = query.Where(rl => rl.LiquidationDate >= fromDate.Value.Date);
        }

        if (toDate.HasValue)
        {
            var endOfDay = toDate.Value.Date.AddDays(1).AddTicks(-1);
            query = query.Where(rl => rl.LiquidationDate <= endOfDay);
        }

        if (routeId.HasValue && routeId.Value != Guid.Empty)
        {
            query = query.Where(rl => rl.RouteId == routeId.Value);
        }

        if (!string.IsNullOrWhiteSpace(status) && Enum.TryParse<RouteLiquidationStatus>(status, true, out var statusEnum))
        {
            query = query.Where(rl => rl.Status == statusEnum);
        }

        int totalCount = await query.CountAsync(cancellationToken);

        var items = await query
            .OrderByDescending(rl => rl.LiquidationDate)
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        return (items, totalCount);
    }

    public async Task<string> GenerateNextLiquidationNumberAsync(CancellationToken cancellationToken = default)
    {
        var yearMonth = DateTime.UtcNow.ToString("yyyyMM");
        var prefix = $"LIQ-{yearMonth}-";

        var lastNumber = await _context.RouteLiquidations
            .Where(rl => rl.LiquidationNumber.StartsWith(prefix))
            .OrderByDescending(rl => rl.LiquidationNumber)
            .Select(rl => rl.LiquidationNumber)
            .FirstOrDefaultAsync(cancellationToken);

        int nextSeq = 1;
        if (!string.IsNullOrEmpty(lastNumber) && lastNumber.Length >= prefix.Length + 4)
        {
            var seqStr = lastNumber.Substring(prefix.Length);
            if (int.TryParse(seqStr, out int currentSeq))
            {
                nextSeq = currentSeq + 1;
            }
        }

        return $"{prefix}{nextSeq:D4}";
    }
}
