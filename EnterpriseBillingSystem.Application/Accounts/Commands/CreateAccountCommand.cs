using System;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using FluentValidation;
using EnterpriseBillingSystem.Domain.Entities;
using EnterpriseBillingSystem.Domain.Enums;
using EnterpriseBillingSystem.Domain.Repositories;

namespace EnterpriseBillingSystem.Application.Accounts.Commands;

public record CreateAccountCommand(
    string Code,
    string Name,
    Guid? ParentAccountId,
    AccountType AccountType,
    AccountNature Nature,
    bool IsPostingAccount
) : IRequest<Guid>;

public class CreateAccountCommandValidator : AbstractValidator<CreateAccountCommand>
{
    private readonly IAccountRepository _accountRepository;

    public CreateAccountCommandValidator(IAccountRepository accountRepository)
    {
        _accountRepository = accountRepository;

        RuleFor(x => x.Code)
            .NotEmpty().WithMessage("El código de la cuenta es requerido.")
            .MaximumLength(50).WithMessage("El código no puede exceder los 50 caracteres.")
            .MustAsync(async (code, cancellation) =>
            {
                var existing = await _accountRepository.GetByCodeAsync(code, cancellation);
                return existing == null;
            }).WithMessage("Ya existe una cuenta contable registrada con este código.");

        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("El nombre de la cuenta es requerido.")
            .MaximumLength(200).WithMessage("El nombre no puede exceder los 200 caracteres.");
    }
}

public class CreateAccountCommandHandler : IRequestHandler<CreateAccountCommand, Guid>
{
    private readonly IAccountRepository _accountRepository;
    private readonly IUnitOfWork _unitOfWork;

    public CreateAccountCommandHandler(IAccountRepository accountRepository, IUnitOfWork unitOfWork)
    {
        _accountRepository = accountRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<Guid> Handle(CreateAccountCommand request, CancellationToken cancellationToken)
    {
        int level = 1;
        if (request.ParentAccountId.HasValue)
        {
            var parent = await _accountRepository.GetByIdAsync(request.ParentAccountId.Value);
            if (parent == null)
                throw new ArgumentException("La cuenta padre especificada no existe.");
            level = parent.Level + 1;
        }

        var account = new Account
        {
            Id = Guid.NewGuid(),
            Code = request.Code,
            Name = request.Name,
            ParentAccountId = request.ParentAccountId,
            AccountType = request.AccountType,
            Nature = request.Nature,
            Level = level,
            IsPostingAccount = request.IsPostingAccount,
            IsActive = true,
            CreatedBy = "System",
            CreatedOnUtc = DateTime.UtcNow
        };

        await _accountRepository.AddAsync(account);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return account.Id;
    }
}
