using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using FluentValidation;
using EnterpriseBillingSystem.Application.Common.Interfaces;
using EnterpriseBillingSystem.Application.Banks.DTOs;
using EnterpriseBillingSystem.Domain.Entities;
using EnterpriseBillingSystem.Domain.Enums;
using EnterpriseBillingSystem.Domain.Repositories;

namespace EnterpriseBillingSystem.Application.Banks.Commands;

public record CreateBankReconciliationCommand(
    Guid BankAccountId,
    DateTime StatementDate,
    decimal StatementBalance,
    string? Notes
) : IRequest<BankReconciliationDto>;

public class CreateBankReconciliationCommandValidator : AbstractValidator<CreateBankReconciliationCommand>
{
    public CreateBankReconciliationCommandValidator()
    {
        RuleFor(x => x.BankAccountId)
            .NotEmpty().WithMessage("La cuenta bancaria es requerida.");

        RuleFor(x => x.StatementDate)
            .LessThanOrEqualTo(DateTime.UtcNow).WithMessage("La fecha del estado de cuenta no puede ser futura.");

        RuleFor(x => x.Notes)
            .MaximumLength(500).WithMessage("Las notas no pueden exceder 500 caracteres.");
    }
}

public class CreateBankReconciliationCommandHandler : IRequestHandler<CreateBankReconciliationCommand, BankReconciliationDto>
{
    private readonly IBankAccountRepository _bankAccountRepository;
    private readonly IBankTransactionRepository _bankTransactionRepository;
    private readonly IBankReconciliationRepository _bankReconciliationRepository;
    private readonly ICurrentUserService _currentUserService;
    private readonly IUnitOfWork _unitOfWork;

    public CreateBankReconciliationCommandHandler(
        IBankAccountRepository bankAccountRepository,
        IBankTransactionRepository bankTransactionRepository,
        IBankReconciliationRepository bankReconciliationRepository,
        ICurrentUserService currentUserService,
        IUnitOfWork unitOfWork)
    {
        _bankAccountRepository = bankAccountRepository;
        _bankTransactionRepository = bankTransactionRepository;
        _bankReconciliationRepository = bankReconciliationRepository;
        _currentUserService = currentUserService;
        _unitOfWork = unitOfWork;
    }

    public async Task<BankReconciliationDto> Handle(CreateBankReconciliationCommand request, CancellationToken cancellationToken)
    {
        var bankAccount = await _bankAccountRepository.GetByIdAsync(request.BankAccountId);
        if (bankAccount == null)
            throw new ArgumentException("La cuenta bancaria no existe.");

        // Calcular SystemBalance: suma de depósitos - suma de retiros hasta la fecha del estado
        var transactions = await _bankTransactionRepository.GetTransactionsByAccountAndPeriodAsync(
            request.BankAccountId,
            DateTime.MinValue,
            request.StatementDate,
            cancellationToken);

        var systemBalance = transactions.Sum(t =>
            t.TransactionType == BankTransactionType.Deposit ||
            t.TransactionType == BankTransactionType.InterestIncome
                ? t.Amount
                : -t.Amount);

        var difference = request.StatementBalance - systemBalance;

        var reconciliation = new BankReconciliation
        {
            Id = Guid.NewGuid(),
            BankAccountId = request.BankAccountId,
            StatementDate = request.StatementDate,
            StatementBalance = request.StatementBalance,
            SystemBalance = systemBalance,
            Difference = difference,
            Notes = request.Notes,
            IsClosed = false,
            BranchId = bankAccount.BranchId,
            CreatedBy = _currentUserService.UserId ?? "System",
            CreatedOnUtc = DateTime.UtcNow
        };

        await _bankReconciliationRepository.AddAsync(reconciliation);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return new BankReconciliationDto(
            reconciliation.Id,
            reconciliation.BankAccountId,
            bankAccount.AccountNumber,
            reconciliation.StatementDate,
            reconciliation.StatementBalance,
            reconciliation.SystemBalance,
            reconciliation.Difference,
            reconciliation.Notes,
            reconciliation.IsClosed
        );
    }
}
