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
/// Registra un retiro o cargo bancario.
/// Si se proporciona CashSessionId, realiza de forma atómica una entrada de caja (CashIn).
/// BankCharge genera: Dr Gastos Bancarios (6105) / Cr Banco.
/// Withdrawal normal genera: Dr Caja General (1110) / Cr Banco (si viene a caja).
/// </summary>
public record CreateWithdrawalCommand(
    Guid BankAccountId,
    DateTime TransactionDate,
    BankTransactionType TransactionType,
    decimal Amount,
    string ReferenceNumber,
    string Description,
    Guid? CashSessionId = null,
    Guid? CashPaymentMethodId = null
) : IRequest<Guid>;

public class CreateWithdrawalCommandValidator : AbstractValidator<CreateWithdrawalCommand>
{
    public CreateWithdrawalCommandValidator()
    {
        RuleFor(x => x.TransactionType)
            .Must(t => t == BankTransactionType.Withdrawal ||
                       t == BankTransactionType.BankCharge ||
                       t == BankTransactionType.InterestIncome ||
                       t == BankTransactionType.Adjustment)
            .WithMessage("Tipo de transacción no válido para retiro/cargo.");

        RuleFor(x => x.Amount)
            .GreaterThan(0).WithMessage("El monto debe ser mayor a 0.");

        RuleFor(x => x.ReferenceNumber)
            .NotEmpty().WithMessage("El número de referencia es requerido.")
            .MaximumLength(100);

        RuleFor(x => x.Description)
            .MaximumLength(500);
    }
}

public class CreateWithdrawalCommandHandler : IRequestHandler<CreateWithdrawalCommand, Guid>
{
    private readonly IBankAccountRepository _bankAccountRepository;
    private readonly IBankTransactionRepository _bankTransactionRepository;
    private readonly ICashSessionRepository _cashSessionRepository;
    private readonly IRepository<CashMovement> _cashMovementRepository;
    private readonly ICurrentUserService _currentUserService;
    private readonly ISender _mediator;
    private readonly IUnitOfWork _unitOfWork;

    public CreateWithdrawalCommandHandler(
        IBankAccountRepository bankAccountRepository,
        IBankTransactionRepository bankTransactionRepository,
        ICashSessionRepository cashSessionRepository,
        IRepository<CashMovement> cashMovementRepository,
        ICurrentUserService currentUserService,
        ISender mediator,
        IUnitOfWork unitOfWork)
    {
        _bankAccountRepository = bankAccountRepository;
        _bankTransactionRepository = bankTransactionRepository;
        _cashSessionRepository = cashSessionRepository;
        _cashMovementRepository = cashMovementRepository;
        _currentUserService = currentUserService;
        _mediator = mediator;
        _unitOfWork = unitOfWork;
    }

    public async Task<Guid> Handle(CreateWithdrawalCommand request, CancellationToken cancellationToken)
    {
        // 1. Obtener cuenta bancaria
        var bankAccount = await _bankAccountRepository.GetByIdAsync(request.BankAccountId);
        if (bankAccount == null)
            throw new ArgumentException("La cuenta bancaria no existe.");

        if (!bankAccount.IsActive)
            throw new InvalidOperationException("La cuenta bancaria no está activa.");

        if (bankAccount.CurrentBalance < request.Amount)
            throw new InvalidOperationException($"Saldo insuficiente. Saldo actual: {bankAccount.CurrentBalance:N2}, Monto requerido: {request.Amount:N2}.");

        // 2. Registrar movimiento de caja (si aplica: solo en retiro normal)
        if (request.CashSessionId.HasValue && request.CashPaymentMethodId.HasValue &&
            request.TransactionType == BankTransactionType.Withdrawal)
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
                MovementType = CashMovementType.CashIn,
                PaymentMethodId = request.CashPaymentMethodId.Value,
                ReferenceDocument = request.ReferenceNumber,
                Amount = request.Amount,
                Notes = $"Retiro bancario de cuenta: {bankAccount.AccountName}. {request.Description}",
                CreatedAt = DateTime.UtcNow
            };
            await _cashMovementRepository.AddAsync(cashMovement);
        }

        // 3. Crear transacción bancaria
        var bankTransaction = new BankTransaction
        {
            Id = Guid.NewGuid(),
            BankAccountId = request.BankAccountId,
            TransactionDate = request.TransactionDate,
            TransactionType = request.TransactionType,
            Amount = request.Amount,
            ReferenceNumber = request.ReferenceNumber,
            Description = request.Description,
            BranchId = bankAccount.BranchId,
            CreatedBy = _currentUserService.UserId ?? "System",
            CreatedOnUtc = DateTime.UtcNow
        };

        // 4. Actualizar saldo
        bankAccount.CurrentBalance -= request.Amount;
        _bankAccountRepository.Update(bankAccount);

        await _bankTransactionRepository.AddAsync(bankTransaction);

        // 5. Generar asiento contable según tipo
        string debitAccountCode;
        string creditAccountCode = bankAccount.AccountingAccountCode;

        debitAccountCode = request.TransactionType switch
        {
            BankTransactionType.BankCharge => "6105",         // Gastos Bancarios
            BankTransactionType.InterestIncome => "4200",     // Ingresos Financieros (crédito especial abajo)
            BankTransactionType.Withdrawal => request.CashSessionId.HasValue ? "1110" : bankAccount.AccountingAccountCode,
            _ => bankAccount.AccountingAccountCode
        };

        // Para InterestIncome: es ingreso — invertimos: Dr Banco / Cr Ingresos Financieros (se procesa como depósito tipo Adjustment)
        List<JournalEntryDetailInput> journalDetails;
        if (request.TransactionType == BankTransactionType.InterestIncome)
        {
            // Ingreso por intereses: incrementa banco, no decrementa
            bankAccount.CurrentBalance += request.Amount * 2; // compensamos el descuento anterior
            _bankAccountRepository.Update(bankAccount);
            journalDetails = new List<JournalEntryDetailInput>
            {
                new(bankAccount.AccountingAccountCode, request.Amount, 0m, $"Ingreso financiero: {request.Description}"),
                new("4200", 0m, request.Amount, $"Ingreso financiero: {request.Description}")
            };
        }
        else
        {
            journalDetails = new List<JournalEntryDetailInput>
            {
                new(debitAccountCode, request.Amount, 0m, $"Retiro/cargo: {request.Description}"),
                new(creditAccountCode, 0m, request.Amount, $"Retiro/cargo: {request.Description}")
            };
        }

        var journalEntryId = await _mediator.Send(new CreateJournalEntryCommand(
            EntryDate: request.TransactionDate,
            Description: $"{request.TransactionType} bancario - {bankAccount.AccountName} - {request.Description}",
            ReferenceDocument: request.ReferenceNumber,
            ReferenceId: bankTransaction.Id,
            SourceModule: "Treasury",
            Details: journalDetails,
            PostImmediately: true
        ), cancellationToken);

        bankTransaction.JournalEntryId = journalEntryId;
        _bankTransactionRepository.Update(bankTransaction);

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return bankTransaction.Id;
    }
}
