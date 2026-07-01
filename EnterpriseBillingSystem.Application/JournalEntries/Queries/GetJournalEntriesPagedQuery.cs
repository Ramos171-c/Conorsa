using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using EnterpriseBillingSystem.Domain.Entities;
using EnterpriseBillingSystem.Domain.Repositories;
using EnterpriseBillingSystem.Application.Common.Models;

namespace EnterpriseBillingSystem.Application.JournalEntries.Queries;

public record JournalEntryDetailDto(
    Guid AccountId,
    string AccountCode,
    string AccountName,
    decimal DebitAmount,
    decimal CreditAmount,
    string? Description
);

public record JournalEntryDto(
    Guid Id,
    string EntryNumber,
    DateTime EntryDate,
    string Description,
    string Status,
    string? ReferenceDocument,
    Guid? ReferenceId,
    string SourceModule,
    decimal TotalAmount,
    List<JournalEntryDetailDto> Details
);

public record GetJournalEntriesPagedQuery(
    DateTime? StartDate,
    DateTime? EndDate,
    string? AccountCode,
    string? SourceModule,
    int PageNumber = 1,
    int PageSize = 20
) : IRequest<PagedResult<JournalEntryDto>>;

public class GetJournalEntriesPagedQueryHandler : IRequestHandler<GetJournalEntriesPagedQuery, PagedResult<JournalEntryDto>>
{
    private readonly IJournalEntryRepository _journalEntryRepository;

    public GetJournalEntriesPagedQueryHandler(IJournalEntryRepository journalEntryRepository)
    {
        _journalEntryRepository = journalEntryRepository;
    }

    public async Task<PagedResult<JournalEntryDto>> Handle(GetJournalEntriesPagedQuery request, CancellationToken cancellationToken)
    {
        var (items, totalCount) = await _journalEntryRepository.GetPagedAsync(
            request.StartDate,
            request.EndDate,
            request.AccountCode,
            request.SourceModule,
            request.PageNumber,
            request.PageSize,
            cancellationToken);

        var dtos = items.Select(j => new JournalEntryDto(
            j.Id,
            j.EntryNumber,
            j.EntryDate,
            j.Description,
            j.Status.ToString(),
            j.ReferenceDocument,
            j.ReferenceId,
            j.SourceModule,
            j.Details.Sum(d => d.DebitAmount), // Sum of debits (which equals credits)
            j.Details.Select(d => new JournalEntryDetailDto(
                d.AccountId,
                d.Account.Code,
                d.Account.Name,
                d.DebitAmount,
                d.CreditAmount,
                d.Description
            )).ToList()
        ));

        return new PagedResult<JournalEntryDto>(dtos, totalCount, request.PageNumber, request.PageSize);
    }
}
