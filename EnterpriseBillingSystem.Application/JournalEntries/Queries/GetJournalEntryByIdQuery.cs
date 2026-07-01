using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using EnterpriseBillingSystem.Domain.Repositories;

namespace EnterpriseBillingSystem.Application.JournalEntries.Queries;

public record GetJournalEntryByIdQuery(Guid Id) : IRequest<JournalEntryDto?>;

public class GetJournalEntryByIdQueryHandler : IRequestHandler<GetJournalEntryByIdQuery, JournalEntryDto?>
{
    private readonly IJournalEntryRepository _journalEntryRepository;

    public GetJournalEntryByIdQueryHandler(IJournalEntryRepository journalEntryRepository)
    {
        _journalEntryRepository = journalEntryRepository;
    }

    public async Task<JournalEntryDto?> Handle(GetJournalEntryByIdQuery request, CancellationToken cancellationToken)
    {
        var j = await _journalEntryRepository.GetByIdWithDetailsAsync(request.Id, cancellationToken);
        if (j == null) return null;

        return new JournalEntryDto(
            j.Id,
            j.EntryNumber,
            j.EntryDate,
            j.Description,
            j.Status.ToString(),
            j.ReferenceDocument,
            j.ReferenceId,
            j.SourceModule,
            j.Details.Sum(d => d.DebitAmount),
            j.Details.Select(d => new JournalEntryDetailDto(
                d.AccountId,
                d.Account.Code,
                d.Account.Name,
                d.DebitAmount,
                d.CreditAmount,
                d.Description
            )).ToList()
        );
    }
}
