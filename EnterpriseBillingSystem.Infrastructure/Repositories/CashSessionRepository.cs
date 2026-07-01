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

public class CashSessionRepository : Repository<CashSession>, ICashSessionRepository
{
    public CashSessionRepository(ApplicationDbContext context) : base(context)
    {
    }

    public async Task<CashSession?> GetByIdWithDetailsAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _context.CashSessions
            .Include(s => s.CashRegister)
            .Include(s => s.OpenedByUser)
            .Include(s => s.ClosedByUser)
            .Include(s => s.CashMovements)
                .ThenInclude(m => m.PaymentMethod)
            .FirstOrDefaultAsync(s => s.Id == id, cancellationToken);
    }

    public async Task<CashSession?> GetOpenSessionByUserAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        return await _context.CashSessions
            .Include(s => s.CashRegister)
            .FirstOrDefaultAsync(s => s.OpenedByUserId == userId && s.Status == CashSessionStatus.Open, cancellationToken);
    }

    public async Task<CashSession?> GetOpenSessionByRegisterAsync(Guid registerId, CancellationToken cancellationToken = default)
    {
        return await _context.CashSessions
            .FirstOrDefaultAsync(s => s.CashRegisterId == registerId && s.Status == CashSessionStatus.Open, cancellationToken);
    }

    public async Task<string> GenerateSessionNumberAsync(CancellationToken cancellationToken = default)
    {
        var today = DateTime.UtcNow.ToString("yyyyMMdd");
        var prefix = $"CS-{today}-";

        var maxSession = await _context.CashSessions
            .IgnoreQueryFilters()
            .Where(s => s.SessionNumber.StartsWith(prefix))
            .OrderByDescending(s => s.SessionNumber)
            .FirstOrDefaultAsync(cancellationToken);

        int nextSequence = 1;
        if (maxSession != null && maxSession.SessionNumber.Length > prefix.Length)
        {
            var suffix = maxSession.SessionNumber.Substring(prefix.Length);
            if (int.TryParse(suffix, out int currentSequence))
            {
                nextSequence = currentSequence + 1;
            }
        }

        return $"{prefix}{nextSequence:D5}";
    }

    public async Task<(IEnumerable<CashSession> Items, int TotalCount)> GetPagedAsync(
        Guid? registerId,
        string? status,
        int pageNumber,
        int pageSize,
        CancellationToken cancellationToken = default)
    {
        var query = _context.CashSessions
            .Include(s => s.CashRegister)
            .Include(s => s.OpenedByUser)
            .AsQueryable();

        if (registerId.HasValue)
        {
            query = query.Where(s => s.CashRegisterId == registerId.Value);
        }

        if (!string.IsNullOrWhiteSpace(status) && Enum.TryParse<CashSessionStatus>(status, true, out var sessionStatus))
        {
            query = query.Where(s => s.Status == sessionStatus);
        }

        int totalCount = await query.CountAsync(cancellationToken);

        var items = await query
            .OrderByDescending(s => s.OpenedAt)
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        return (items, totalCount);
    }
}
