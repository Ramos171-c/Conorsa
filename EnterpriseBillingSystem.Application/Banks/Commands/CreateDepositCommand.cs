using System;
using System.Collections.Generic;
using System.Linq;
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
/// Registra un depósito en cuenta bancaria.
/// Si se proporciona CashSessionId, realiza de forma atómica una salida de caja (CashOut).
/// Genera asiento contable: Dr Banco / Cr Caja General (si viene de caja) o Dr Banco / Cr Cuenta definida por usuario.
/// </summary>
public record CreateDepositCommand(
    Guid BankAccountId,
    DateTime TransactionDate,
    decimal Amount,
    string ReferenceNumber,
    string Description,
    Guid? CashSessionId = null,
    Guid? CashPaymentMethodId = null
) : IRequest<Guid>;

public class CreateDepositCommandValidator : AbstractValidator<CreateDepositCommand>
{
    public CreateDepositCommandValidator()
    {
        RuleFor(x => x.Amount)
            .GreaterThan(0).WithMessage("El monto del depósito debe ser mayor a 0.");

        RuleFor(x => x.ReferenceNumber)
            .NotEmpty().WithMessage("El número de referencia es requerido.")
            .MaximumLength(100).WithMessage("La referencia no puede exceder 100 caracteres.");

        RuleFor(x => x.Description)
            .MaximumLength(500).WithMessage("La descripción no puede exceder 500 caracteres.");

        RuleFor(x => x.CashPaymentMethodId)
            .NotNull().When(x => x.CashSessionId.HasValue)
            .WithMessage("Debe especificar el método de pago si se indica sesión de caja.");
    }
}

public class CreateDepositCommandHandler : IRequestHandler<CreateDepositCommand, Guid>
{
    private readonly IBankAccountRepository _bankAccountRepository;
    private readonly IBankTransactionRepository _bankTransactionRepository;
    private readonly ICashSessionRepository _cashSessionRepository;
    private readonly IRepository<CashMovement> _cashMovementRepository;
    private readonly IAccountRepository _accountRepository;
    private readonly ICurrentUserService _currentUserService;
    private readonly ISender _mediator;
    private readonly IUnitOfWork _unitOfWork;

    public CreateDepositCommandHandler(
        IBankAccountRepository bankAccountRepository,
        IBankTransactionRepository bankTransactionRepository,
        ICashSessionRepository cashSessionRepository,
        IRepository<CashMovement> cashMovementRepository,
        IAccountRepository accountRepository,
        ICurrentUserService currentUserService,
        ISender mediator,
        IUnitOfWork unitOfWork)
    {
        _bankAccountRepository = bankAccountRepository;
        _bankTransactionRepository = bankTransactionRepository;
        _cashSessionRepository = cashSessionRepository;
        _cashMovementRepository = cashMovementRepository;
        _accountRepository = accountRepository;
        _currentUserService = currentUserService;
        _mediator = mediator;
        _unitOfWork = unitOfWork;
    }

    public async Task<Guid> Handle(CreateDepositCommand request, CancellationToken cancellationToken)
    {
        // 1. Obtener cuenta bancaria
        var bankAccount = await _bankAccountRepository.GetByIdAsync(request.BankAccountId);
        if (bankAccount == null)
            throw new ArgumentException("La cuenta bancaria no existe.");

        if (!bankAccount.IsActive)
            throw new InvalidOperationException("La cuenta bancaria no está activa.");

        // 2. Registrar movimiento de caja (si aplica)
        if (request.CashSessionId.HasValue && request.CashPaymentMethodId.HasValue)
        {
            var cashSession = await _cashSessionRepository.GetByIdAsync(request.CashSessionId.Value);
            if (cashSession == null)
                throw new ArgumentException("La sesión de caja no existe.");

            if (cashSession.Status != CashSessionStatus.Open)
                throw new InvalidOperationException("La sesión de caja no está abierta.");

            var cashMovement = new CashMovement
            {
                Id = Guid.NewGuid(),
                CashSessionId = cashSession.Id,
                MovementType = CashMovementType.CashOut,
                PaymentMethodId = request.CashPaymentMethodId.Value,
                ReferenceDocument = request.ReferenceNumber,
                Amount = request.Amount,
                Reason = $"Depósito bancario a cuenta: {bankAccount.AccountName}",
                Notes = request.Description,
                CreatedAt = DateTime.UtcNow
            };
            await _cashMovementRepository.AddAsync(cashMovement);
        }

        // 3. Crear la transacción bancaria
        var bankTransaction = new BankTransaction
        {
            Id = Guid.NewGuid(),
            BankAccountId = request.BankAccountId,
            TransactionDate = request.TransactionDate,
            TransactionType = BankTransactionType.Deposit,
            Amount = request.Amount,
            ReferenceNumber = request.ReferenceNumber,
            Description = request.Description,
            BranchId = bankAccount.BranchId,
            CreatedBy = _currentUserService.UserId ?? "System",
            CreatedOnUtc = DateTime.UtcNow
        };

        // 4. Actualizar saldo de cuenta bancaria
        bankAccount.CurrentBalance += request.Amount;
        _bankAccountRepository.Update(bankAccount);

        await _bankTransactionRepository.AddAsync(bankTransaction);

        // 5. Generar asiento contable
        // Dr Banco (1121/1122 = AccountingAccountCode de la cuenta bancaria) / Cr Caja General (1110) si viene de caja, sino Cr Banco también en otro flujo
        var creditAccountCode = request.CashSessionId.HasValue ? "1110" : bankAccount.AccountingAccountCode;
        var debitAccountCode = bankAccount.AccountingAccountCode;

        var journalEntryId = await _mediator.Send(new CreateJournalEntryCommand(
            EntryDate: request.TransactionDate,
            Description: $"Depósito bancario - {bankAccount.AccountName} - {request.Description}",
            ReferenceDocument: request.ReferenceNumber,
            ReferenceId: bankTransaction.Id,
            SourceModule: "Treasury",
            Details: new List<JournalEntryDetailInput>
            {
                new(debitAccountCode, request.Amount, 0m, $"Depósito: {request.Description}"),
                new(creditAccountCode, 0m, request.Amount, $"Depósito: {request.Description}")
            },
            PostImmediately: true
        ), cancellationToken);

        bankTransaction.JournalEntryId = journalEntryId;
        _bankTransactionRepository.Update(bankTransaction);

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return bankTransaction.Id;
    }
}
