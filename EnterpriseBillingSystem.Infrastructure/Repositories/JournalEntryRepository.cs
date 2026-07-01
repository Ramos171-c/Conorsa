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

public class JournalEntryRepository : Repository<JournalEntry>, IJournalEntryRepository
{
    public JournalEntryRepository(ApplicationDbContext context) : base(context)
    {
    }

    public async Task<JournalEntry?> GetByIdWithDetailsAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _context.JournalEntries
            .Include(j => j.Details)
                .ThenInclude(d => d.Account)
            .FirstOrDefaultAsync(j => j.Id == id, cancellationToken);
    }

    public async Task<JournalEntry?> GetByReferenceIdAsync(Guid referenceId, CancellationToken cancellationToken = default)
    {
        return await _context.JournalEntries
            .Include(j => j.Details)
                .ThenInclude(d => d.Account)
            .FirstOrDefaultAsync(j => j.ReferenceId == referenceId, cancellationToken);
    }

    public async Task<string> GenerateEntryNumberAsync(CancellationToken cancellationToken = default)
    {
        var today = DateTime.UtcNow.ToString("yyyyMMdd");
        var prefix = $"JE-{today}-";

        var maxEntry = await _context.JournalEntries
            .IgnoreQueryFilters()
            .Where(j => j.EntryNumber.StartsWith(prefix))
            .OrderByDescending(j => j.EntryNumber)
            .FirstOrDefaultAsync(cancellationToken);

        int nextSequence = 1;
        if (maxEntry != null && maxEntry.EntryNumber.Length > prefix.Length)
        {
            var suffix = maxEntry.EntryNumber.Substring(prefix.Length);
            if (int.TryParse(suffix, out int currentSequence))
            {
                nextSequence = currentSequence + 1;
            }
        }

        return $"{prefix}{nextSequence:D5}";
    }

    public async Task<(IEnumerable<JournalEntry> Items, int TotalCount)> GetPagedAsync(
        DateTime? startDate,
        DateTime? endDate,
        string? accountCode,
        string? sourceModule,
        int pageNumber,
        int pageSize,
        CancellationToken cancellationToken = default)
    {
        var query = _context.JournalEntries
            .Include(j => j.Details)
                .ThenInclude(d => d.Account)
            .AsQueryable();

        if (startDate.HasValue)
        {
            query = query.Where(j => j.EntryDate >= startDate.Value);
        }

        if (endDate.HasValue)
        {
            query = query.Where(j => j.EntryDate <= endDate.Value);
        }

        if (!string.IsNullOrWhiteSpace(sourceModule))
        {
            query = query.Where(j => j.SourceModule == sourceModule);
        }

        if (!string.IsNullOrWhiteSpace(accountCode))
        {
            query = query.Where(j => j.Details.Any(d => d.Account.Code == accountCode));
        }

        int totalCount = await query.CountAsync(cancellationToken);

        var items = await query
            .OrderByDescending(j => j.EntryDate)
            .ThenByDescending(j => j.EntryNumber)
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        return (items, totalCount);
    }

    public async Task<IEnumerable<JournalEntry>> GetPostedEntriesWithDetailsAsync(
        DateTime? startDate,
        DateTime? endDate,
        CancellationToken cancellationToken = default)
    {
        var query = _context.JournalEntries
            .Include(j => j.Details)
                .ThenInclude(d => d.Account)
            .Where(j => j.Status == JournalEntryStatus.Posted)
            .AsQueryable();

        if (startDate.HasValue)
        {
            query = query.Where(j => j.EntryDate >= startDate.Value);
        }

        if (endDate.HasValue)
        {
            query = query.Where(j => j.EntryDate <= endDate.Value);
        }

        return await query.ToListAsync(cancellationToken);
    }
}
