using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using FluentValidation;
using EnterpriseBillingSystem.Application.Common.Interfaces;
using EnterpriseBillingSystem.Application.JournalEntries.Commands;
using EnterpriseBillingSystem.Domain.Entities;
using EnterpriseBillingSystem.Domain.Enums;
using EnterpriseBillingSystem.Domain.Repositories;

namespace EnterpriseBillingSystem.Application.Banks.Commands;

/// <summary>
/// Realiza transferencia atómica entre dos cuentas bancarias.
/// Dr Cuenta Destino / Cr Cuenta Origen
/// </summary>
public record CreateTransferCommand(
    Guid SourceBankAccountId,
    Guid DestinationBankAccountId,
    DateTime TransactionDate,
    decimal Amount,
    string ReferenceNumber,
    string Description
) : IRequest<Guid>;

public class CreateTransferCommandValidator : AbstractValidator<CreateTransferCommand>
{
    public CreateTransferCommandValidator()
    {
        RuleFor(x => x.SourceBankAccountId)
            .NotEmpty().WithMessage("La cuenta origen es requerida.");

        RuleFor(x => x.DestinationBankAccountId)
            .NotEmpty().WithMessage("La cuenta destino es requerida.")
            .NotEqual(x => x.SourceBankAccountId).WithMessage("La cuenta origen y destino no pueden ser la misma.");

        RuleFor(x => x.Amount)
            .GreaterThan(0).WithMessage("El monto de la transferencia debe ser mayor a 0.");

        RuleFor(x => x.ReferenceNumber)
            .NotEmpty().WithMessage("El número de referencia es requerido.")
            .MaximumLength(100);

        RuleFor(x => x.Description)
            .MaximumLength(500);
    }
}

public class CreateTransferCommandHandler : IRequestHandler<CreateTransferCommand, Guid>
{
    private readonly IBankAccountRepository _bankAccountRepository;
    private readonly IBankTransactionRepository _bankTransactionRepository;
    private readonly ICurrentUserService _currentUserService;
    private readonly ISender _mediator;
    private readonly IUnitOfWork _unitOfWork;

    public CreateTransferCommandHandler(
        IBankAccountRepository bankAccountRepository,
        IBankTransactionRepository bankTransactionRepository,
        ICurrentUserService currentUserService,
        ISender mediator,
        IUnitOfWork unitOfWork)
    {
        _bankAccountRepository = bankAccountRepository;
        _bankTransactionRepository = bankTransactionRepository;
        _currentUserService = currentUserService;
        _mediator = mediator;
        _unitOfWork = unitOfWork;
    }

    public async Task<Guid> Handle(CreateTransferCommand request, CancellationToken cancellationToken)
    {
        // 1. Obtener cuentas
        var sourceAccount = await _bankAccountRepository.GetByIdAsync(request.SourceBankAccountId);
        if (sourceAccount == null)
            throw new ArgumentException("La cuenta bancaria origen no existe.");
        if (!sourceAccount.IsActive)
            throw new InvalidOperationException("La cuenta bancaria origen no está activa.");
        if (sourceAccount.CurrentBalance < request.Amount)
            throw new InvalidOperationException($"Saldo insuficiente en cuenta origen. Saldo: {sourceAccount.CurrentBalance:N2}.");

        var destAccount = await _bankAccountRepository.GetByIdAsync(request.DestinationBankAccountId);
        if (destAccount == null)
            throw new ArgumentException("La cuenta bancaria destino no existe.");
        if (!destAccount.IsActive)
            throw new InvalidOperationException("La cuenta bancaria destino no está activa.");

        var currentUserId = _currentUserService.UserId ?? "System";

        // 2. Transacción de salida en cuenta origen
        var outboundTx = new BankTransaction
        {
            Id = Guid.NewGuid(),
            BankAccountId = request.SourceBankAccountId,
            TransactionDate = request.TransactionDate,
            TransactionType = BankTransactionType.Transfer,
            Amount = request.Amount,
            ReferenceNumber = request.ReferenceNumber,
            Description = $"Transferencia saliente a {destAccount.AccountName}. {request.Description}",
            RelatedBankAccountId = request.DestinationBankAccountId,
            BranchId = sourceAccount.BranchId,
            CreatedBy = currentUserId,
            CreatedOnUtc = DateTime.UtcNow
        };

        // 3. Transacción de entrada en cuenta destino
        var inboundTx = new BankTransaction
        {
            Id = Guid.NewGuid(),
            BankAccountId = request.DestinationBankAccountId,
            TransactionDate = request.TransactionDate,
            TransactionType = BankTransactionType.Transfer,
            Amount = request.Amount,
            ReferenceNumber = request.ReferenceNumber,
            Description = $"Transferencia entrante desde {sourceAccount.AccountName}. {request.Description}",
            RelatedBankAccountId = request.SourceBankAccountId,
            BranchId = destAccount.BranchId,
            CreatedBy = currentUserId,
            CreatedOnUtc = DateTime.UtcNow
        };

        // 4. Actualizar saldos
        sourceAccount.CurrentBalance -= request.Amount;
        destAccount.CurrentBalance += request.Amount;
        _bankAccountRepository.Update(sourceAccount);
        _bankAccountRepository.Update(destAccount);

        await _bankTransactionRepository.AddAsync(outboundTx);
        await _bankTransactionRepository.AddAsync(inboundTx);

        // 5. Generar asiento contable: Dr Cuenta Destino / Cr Cuenta Origen
        var journalEntryId = await _mediator.Send(new CreateJournalEntryCommand(
            EntryDate: request.TransactionDate,
            Description: $"Transferencia bancaria de {sourceAccount.AccountName} a {destAccount.AccountName}. {request.Description}",
            ReferenceDocument: request.ReferenceNumber,
            ReferenceId: outboundTx.Id,
            SourceModule: "Treasury",
            Details: new List<JournalEntryDetailInput>
            {
                new(destAccount.AccountingAccountCode, request.Amount, 0m, $"Transferencia entrante: {destAccount.AccountName}"),
                new(sourceAccount.AccountingAccountCode, 0m, request.Amount, $"Transferencia saliente: {sourceAccount.AccountName}")
            },
            PostImmediately: true
        ), cancellationToken);

        outboundTx.JournalEntryId = journalEntryId;
        inboundTx.JournalEntryId = journalEntryId;
        _bankTransactionRepository.Update(outboundTx);
        _bankTransactionRepository.Update(inboundTx);

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return outboundTx.Id;
    }
}
