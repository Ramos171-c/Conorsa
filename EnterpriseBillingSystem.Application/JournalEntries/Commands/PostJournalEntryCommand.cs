using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using FluentValidation;
using EnterpriseBillingSystem.Domain.Entities;
using EnterpriseBillingSystem.Domain.Enums;
using EnterpriseBillingSystem.Domain.Repositories;
using EnterpriseBillingSystem.Application.Common.Interfaces;

namespace EnterpriseBillingSystem.Application.JournalEntries.Commands;

public record PostJournalEntryCommand(Guid JournalEntryId) : IRequest<Unit>;

public class PostJournalEntryCommandValidator : AbstractValidator<PostJournalEntryCommand>
{
    public PostJournalEntryCommandValidator()
    {
        RuleFor(x => x.JournalEntryId)
            .NotEmpty().WithMessage("El Id del asiento contable es requerido.");
    }
}

public class PostJournalEntryCommandHandler : IRequestHandler<PostJournalEntryCommand, Unit>
{
    private readonly IJournalEntryRepository _journalEntryRepository;
    private readonly ICurrentUserService _currentUserService;
    private readonly IRepository<AccountingPeriod> _periodRepository;
    private readonly IUnitOfWork _unitOfWork;

    public PostJournalEntryCommandHandler(
        IJournalEntryRepository journalEntryRepository,
        ICurrentUserService currentUserService,
        IRepository<AccountingPeriod> periodRepository,
        IUnitOfWork unitOfWork)
    {
        _journalEntryRepository = journalEntryRepository;
        _currentUserService = currentUserService;
        _periodRepository = periodRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<Unit> Handle(PostJournalEntryCommand request, CancellationToken cancellationToken)
    {
        var entry = await _journalEntryRepository.GetByIdWithDetailsAsync(request.JournalEntryId, cancellationToken);
        if (entry == null)
            throw new ArgumentException($"El asiento contable con Id '{request.JournalEntryId}' no existe.");

        if (entry.Status != JournalEntryStatus.Draft)
            throw new InvalidOperationException($"Solo se pueden contabilizar asientos en estado Borrador. Estado actual: {entry.Status}.");

        // Validar período
        var periods = await _periodRepository.FindAsync(p => p.Year == entry.EntryDate.Year && p.Month == entry.EntryDate.Month && p.IsClosed);
        var isClosed = periods.Any();

        if (isClosed)
            throw new InvalidOperationException("No se permiten movimientos en un período contable cerrado.");

        entry.Status = JournalEntryStatus.Posted;
        entry.PostedByUserId = _currentUserService.UserId ?? "System";
        entry.PostedAt = DateTime.UtcNow;

        _journalEntryRepository.Update(entry);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return Unit.Value;
    }
}
