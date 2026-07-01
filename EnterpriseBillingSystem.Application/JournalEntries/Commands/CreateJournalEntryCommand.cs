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

public record JournalEntryDetailInput(
    string AccountCode,
    decimal DebitAmount,
    decimal CreditAmount,
    string? Description = null
);

public record CreateJournalEntryCommand(
    DateTime EntryDate,
    string Description,
    string? ReferenceDocument,
    Guid? ReferenceId,
    string SourceModule,
    List<JournalEntryDetailInput> Details,
    bool PostImmediately = true
) : IRequest<Guid>;

public class CreateJournalEntryCommandValidator : AbstractValidator<CreateJournalEntryCommand>
{
    public CreateJournalEntryCommandValidator()
    {
        RuleFor(x => x.Description)
            .NotEmpty().WithMessage("La descripción del asiento es requerida.")
            .MaximumLength(500).WithMessage("La descripción no puede exceder los 500 caracteres.");

        RuleFor(x => x.SourceModule)
            .NotEmpty().WithMessage("El módulo origen es requerido.")
            .MaximumLength(50).WithMessage("El módulo origen no puede exceder los 50 caracteres.");

        RuleFor(x => x.Details)
            .NotEmpty().WithMessage("El asiento debe tener al menos un detalle.");
    }
}

public class CreateJournalEntryCommandHandler : IRequestHandler<CreateJournalEntryCommand, Guid>
{
    private readonly IJournalEntryRepository _journalEntryRepository;
    private readonly IAccountRepository _accountRepository;
    private readonly ICurrentUserService _currentUserService;
    private readonly IRepository<AccountingPeriod> _periodRepository;
    private readonly IUnitOfWork _unitOfWork;

    public CreateJournalEntryCommandHandler(
        IJournalEntryRepository journalEntryRepository,
        IAccountRepository accountRepository,
        ICurrentUserService currentUserService,
        IRepository<AccountingPeriod> periodRepository,
        IUnitOfWork unitOfWork)
    {
        _journalEntryRepository = journalEntryRepository;
        _accountRepository = accountRepository;
        _currentUserService = currentUserService;
        _periodRepository = periodRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<Guid> Handle(CreateJournalEntryCommand request, CancellationToken cancellationToken)
    {
        // 1. Validar período contable cerrado
        var year = request.EntryDate.Year;
        var month = request.EntryDate.Month;
        var periods = await _periodRepository.FindAsync(p => p.Year == year && p.Month == month && p.IsClosed);
        var isClosed = periods.Any();

        if (isClosed)
        {
            throw new InvalidOperationException("No se permiten movimientos en un período contable cerrado.");
        }

        // 2. Validar partida doble (Débitos == Créditos)
        var totalDebits = request.Details.Sum(d => d.DebitAmount);
        var totalCredits = request.Details.Sum(d => d.CreditAmount);

        if (totalDebits != totalCredits)
        {
            throw new InvalidOperationException($"El asiento contable no está cuadrado. Total Débitos: {totalDebits}, Total Créditos: {totalCredits}.");
        }

        if (totalDebits <= 0)
        {
            throw new InvalidOperationException("El monto del asiento debe ser mayor a 0.");
        }

        // 3. Generar número correlativo
        var entryNumber = await _journalEntryRepository.GenerateEntryNumberAsync(cancellationToken);

        var currentUserId = _currentUserService.UserId ?? "System";

        var journalEntry = new JournalEntry
        {
            Id = Guid.NewGuid(),
            EntryNumber = entryNumber,
            EntryDate = request.EntryDate,
            Description = request.Description,
            ReferenceDocument = request.ReferenceDocument,
            ReferenceId = request.ReferenceId,
            SourceModule = request.SourceModule,
            Status = request.PostImmediately ? JournalEntryStatus.Posted : JournalEntryStatus.Draft,
            PostedByUserId = request.PostImmediately ? currentUserId : null,
            PostedAt = request.PostImmediately ? DateTime.UtcNow : null,
            CreatedBy = currentUserId,
            CreatedOnUtc = DateTime.UtcNow,
            BranchId = _currentUserService.BranchId
        };

        // 4. Mapear y validar detalles
        foreach (var detailInput in request.Details)
        {
            var account = await _accountRepository.GetByCodeAsync(detailInput.AccountCode, cancellationToken);
            if (account == null)
            {
                throw new ArgumentException($"La cuenta contable con código '{detailInput.AccountCode}' no existe.");
            }

            if (!account.IsActive)
            {
                throw new InvalidOperationException($"La cuenta contable '{account.Name}' ({account.Code}) no está activa.");
            }

            if (!account.IsPostingAccount)
            {
                throw new InvalidOperationException($"La cuenta contable '{account.Name}' ({account.Code}) no es una cuenta de movimiento (Posting Account).");
            }

            journalEntry.Details.Add(new JournalEntryDetail
            {
                Id = Guid.NewGuid(),
                JournalEntryId = journalEntry.Id,
                AccountId = account.Id,
                DebitAmount = detailInput.DebitAmount,
                CreditAmount = detailInput.CreditAmount,
                Description = detailInput.Description
            });
        }

        await _journalEntryRepository.AddAsync(journalEntry);
        // NOTA: SaveChangesAsync será invocado por el UnitOfWork de la transacción principal que llamó a este comando.
        // Pero si se invoca de forma directa (API), llamamos a SaveChangesAsync.
        // Dado que el controlador llama a Mediator.Send() y espera que guarde, podemos guardar si no estamos en una transacción externa o si queremos asegurar persistencia.
        // En nuestro sistema, la transacción abarca el Command handler principal. El Handler principal llamará a SaveChangesAsync al final de su flujo.
        // Sin embargo, si CreateJournalEntryCommand es invocado directamente desde el controller, necesitamos guardar.
        // Para soportar ambos casos, guardamos aquí. Si hay una transacción en curso (EF Core TransactionScope), se confirmará al final.
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return journalEntry.Id;
    }
}
