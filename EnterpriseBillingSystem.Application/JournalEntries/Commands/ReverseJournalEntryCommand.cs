using System;
using System.Collections.Generic;
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

public record ReverseJournalEntryCommand(
    Guid JournalEntryId,
    string? ReversalReason
) : IRequest<Guid>;

public class ReverseJournalEntryCommandValidator : AbstractValidator<ReverseJournalEntryCommand>
{
    public ReverseJournalEntryCommandValidator()
    {
        RuleFor(x => x.JournalEntryId)
            .NotEmpty().WithMessage("El Id del asiento contable a reversar es requerido.");
    }
}

public class ReverseJournalEntryCommandHandler : IRequestHandler<ReverseJournalEntryCommand, Guid>
{
    private readonly IJournalEntryRepository _journalEntryRepository;
    private readonly ICurrentUserService _currentUserService;
    private readonly IRepository<AccountingPeriod> _periodRepository;
    private readonly IUnitOfWork _unitOfWork;

    public ReverseJournalEntryCommandHandler(
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

    public async Task<Guid> Handle(ReverseJournalEntryCommand request, CancellationToken cancellationToken)
    {
        var originalEntry = await _journalEntryRepository.GetByIdWithDetailsAsync(request.JournalEntryId, cancellationToken);
        if (originalEntry == null)
            throw new ArgumentException($"El asiento contable con Id '{request.JournalEntryId}' no existe.");

        if (originalEntry.Status != JournalEntryStatus.Posted)
            throw new InvalidOperationException($"Solo se pueden reversar asientos en estado Contabilizado (Posted). Estado actual: {originalEntry.Status}.");

        // Validar período para la fecha de reversión (hoy)
        var today = DateTime.UtcNow;
        var periods = await _periodRepository.FindAsync(p => p.Year == today.Year && p.Month == today.Month && p.IsClosed);
        var isClosed = periods.Any();

        if (isClosed)
            throw new InvalidOperationException("No se permiten movimientos en un período contable cerrado.");

        // 1. Cambiar estado de la original a Cancelled
        originalEntry.Status = JournalEntryStatus.Cancelled;
        originalEntry.LastModifiedBy = _currentUserService.UserId ?? "System";
        originalEntry.LastModifiedOnUtc = DateTime.UtcNow;
        _journalEntryRepository.Update(originalEntry);

        // 2. Crear asiento reverso
        var reversalEntryNumber = await _journalEntryRepository.GenerateEntryNumberAsync(cancellationToken);
        var currentUserId = _currentUserService.UserId ?? "System";

        var reversalEntry = new JournalEntry
        {
            Id = Guid.NewGuid(),
            EntryNumber = reversalEntryNumber,
            EntryDate = today,
            Description = $"Reversión de asiento {originalEntry.EntryNumber}. Motivo: {request.ReversalReason ?? "Anulación de documento"}",
            ReferenceDocument = originalEntry.ReferenceDocument,
            ReferenceId = originalEntry.ReferenceId,
            SourceModule = originalEntry.SourceModule,
            Status = JournalEntryStatus.Posted,
            PostedByUserId = currentUserId,
            PostedAt = DateTime.UtcNow,
            CreatedBy = currentUserId,
            CreatedOnUtc = DateTime.UtcNow,
            BranchId = originalEntry.BranchId // Usar la misma sucursal
        };

        // 3. Detalles invertidos (Débito <-> Crédito)
        foreach (var originalDetail in originalEntry.Details)
        {
            reversalEntry.Details.Add(new JournalEntryDetail
            {
                Id = Guid.NewGuid(),
                JournalEntryId = reversalEntry.Id,
                AccountId = originalDetail.AccountId,
                DebitAmount = originalDetail.CreditAmount, // Invertir
                CreditAmount = originalDetail.DebitAmount, // Invertir
                Description = $"[Reversión] {originalDetail.Description}"
            });
        }

        await _journalEntryRepository.AddAsync(reversalEntry);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return reversalEntry.Id;
    }
}
