using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using EnterpriseBillingSystem.Domain.Entities;

namespace EnterpriseBillingSystem.Domain.Repositories;

public interface IJournalEntryRepository : IRepository<JournalEntry>
{
    Task<JournalEntry?> GetByIdWithDetailsAsync(Guid id, CancellationToken cancellationToken = default);
    Task<JournalEntry?> GetByReferenceIdAsync(Guid referenceId, CancellationToken cancellationToken = default);
    Task<string> GenerateEntryNumberAsync(CancellationToken cancellationToken = default);
    Task<(IEnumerable<JournalEntry> Items, int TotalCount)> GetPagedAsync(
        DateTime? startDate,
        DateTime? endDate,
        string? accountCode,
        string? sourceModule,
        int pageNumber,
        int pageSize,
        CancellationToken cancellationToken = default);
    Task<IEnumerable<JournalEntry>> GetPostedEntriesWithDetailsAsync(
        DateTime? startDate,
        DateTime? endDate,
        CancellationToken cancellationToken = default);
}
