using System;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using FluentValidation;
using EnterpriseBillingSystem.Application.Common.Interfaces;
using EnterpriseBillingSystem.Domain.Entities;
using EnterpriseBillingSystem.Domain.Repositories;

namespace EnterpriseBillingSystem.Application.Banks.Commands;

public record CreateBankAccountCommand(
    Guid BankId,
    string AccountNumber,
    string AccountName,
    string CurrencyCode,
    string AccountingAccountCode
) : IRequest<Guid>;

public class CreateBankAccountCommandValidator : AbstractValidator<CreateBankAccountCommand>
{
    private readonly IBankRepository _bankRepository;
    private readonly IBankAccountRepository _bankAccountRepository;

    public CreateBankAccountCommandValidator(IBankRepository bankRepository, IBankAccountRepository bankAccountRepository)
    {
        _bankRepository = bankRepository;
        _bankAccountRepository = bankAccountRepository;

        RuleFor(x => x.BankId)
            .MustAsync(async (bankId, cancellation) =>
            {
                var bank = await _bankRepository.GetByIdAsync(bankId);
                return bank != null && !bank.IsDeleted;
            }).WithMessage("El banco especificado no existe o está inactivo.");

        RuleFor(x => x.AccountNumber)
            .NotEmpty().WithMessage("El número de cuenta es requerido.")
            .MaximumLength(50).WithMessage("El número de cuenta no puede exceder 50 caracteres.")
            .MustAsync(async (command, accountNumber, cancellation) =>
            {
                return !await _bankAccountRepository.ExistsAccountNumberInBankAsync(command.BankId, accountNumber, cancellation);
            }).WithMessage("Ya existe una cuenta con este número en el banco seleccionado.");

        RuleFor(x => x.AccountName)
            .NotEmpty().WithMessage("El nombre de la cuenta es requerido.")
            .MaximumLength(200).WithMessage("El nombre no puede exceder 200 caracteres.");

        RuleFor(x => x.CurrencyCode)
            .NotEmpty().WithMessage("El código de moneda es requerido.")
            .MaximumLength(10).WithMessage("El código de moneda no puede exceder 10 caracteres.");

        RuleFor(x => x.AccountingAccountCode)
            .NotEmpty().WithMessage("El código de cuenta contable es requerido.")
            .MaximumLength(50).WithMessage("El código contable no puede exceder 50 caracteres.");
    }
}

public class CreateBankAccountCommandHandler : IRequestHandler<CreateBankAccountCommand, Guid>
{
    private readonly IBankAccountRepository _bankAccountRepository;
    private readonly ICurrentUserService _currentUserService;
    private readonly IUnitOfWork _unitOfWork;

    public CreateBankAccountCommandHandler(
        IBankAccountRepository bankAccountRepository,
        ICurrentUserService currentUserService,
        IUnitOfWork unitOfWork)
    {
        _bankAccountRepository = bankAccountRepository;
        _currentUserService = currentUserService;
        _unitOfWork = unitOfWork;
    }

    public async Task<Guid> Handle(CreateBankAccountCommand request, CancellationToken cancellationToken)
    {
        var account = new BankAccount
        {
            BankId = request.BankId,
            AccountNumber = request.AccountNumber,
            AccountName = request.AccountName,
            CurrencyCode = request.CurrencyCode,
            AccountingAccountCode = request.AccountingAccountCode,
            CurrentBalance = 0m,
            IsActive = true,
            BranchId = _currentUserService.BranchId
        };

        await _bankAccountRepository.AddAsync(account);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return account.Id;
    }
}
