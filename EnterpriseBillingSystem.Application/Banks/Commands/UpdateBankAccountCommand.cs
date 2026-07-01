using System;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using FluentValidation;
using EnterpriseBillingSystem.Domain.Repositories;

namespace EnterpriseBillingSystem.Application.Banks.Commands;

public record UpdateBankAccountCommand(
    Guid Id,
    string AccountName,
    string CurrencyCode,
    string AccountingAccountCode,
    bool IsActive
) : IRequest<bool>;

public class UpdateBankAccountCommandValidator : AbstractValidator<UpdateBankAccountCommand>
{
    public UpdateBankAccountCommandValidator()
    {
        RuleFor(x => x.AccountName)
            .NotEmpty().WithMessage("El nombre de la cuenta es requerido.")
            .MaximumLength(200).WithMessage("El nombre no puede exceder 200 caracteres.");

        RuleFor(x => x.CurrencyCode)
            .NotEmpty().WithMessage("El código de moneda es requerido.")
            .MaximumLength(10);

        RuleFor(x => x.AccountingAccountCode)
            .NotEmpty().WithMessage("El código de cuenta contable es requerido.")
            .MaximumLength(50);
    }
}

public class UpdateBankAccountCommandHandler : IRequestHandler<UpdateBankAccountCommand, bool>
{
    private readonly IBankAccountRepository _bankAccountRepository;
    private readonly IUnitOfWork _unitOfWork;

    public UpdateBankAccountCommandHandler(IBankAccountRepository bankAccountRepository, IUnitOfWork unitOfWork)
    {
        _bankAccountRepository = bankAccountRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<bool> Handle(UpdateBankAccountCommand request, CancellationToken cancellationToken)
    {
        var account = await _bankAccountRepository.GetByIdAsync(request.Id);
        if (account == null) return false;

        account.AccountName = request.AccountName;
        account.CurrencyCode = request.CurrencyCode;
        account.AccountingAccountCode = request.AccountingAccountCode;
        account.IsActive = request.IsActive;

        _bankAccountRepository.Update(account);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return true;
    }
}
