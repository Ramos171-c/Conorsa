using System;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using FluentValidation;
using EnterpriseBillingSystem.Domain.Entities;
using EnterpriseBillingSystem.Domain.Repositories;

namespace EnterpriseBillingSystem.Application.Accounts.Commands;

public record UpdateAccountCommand(
    Guid Id,
    string Name,
    Guid? ParentAccountId,
    bool IsActive
) : IRequest<Unit>;

public class UpdateAccountCommandValidator : AbstractValidator<UpdateAccountCommand>
{
    public UpdateAccountCommandValidator()
    {
        RuleFor(x => x.Id)
            .NotEmpty().WithMessage("El Id de la cuenta es requerido.");

        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("El nombre de la cuenta es requerido.")
            .MaximumLength(200).WithMessage("El nombre no puede exceder los 200 caracteres.");
    }
}

public class UpdateAccountCommandHandler : IRequestHandler<UpdateAccountCommand, Unit>
{
    private readonly IAccountRepository _accountRepository;
    private readonly IUnitOfWork _unitOfWork;

    public UpdateAccountCommandHandler(IAccountRepository accountRepository, IUnitOfWork unitOfWork)
    {
        _accountRepository = accountRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<Unit> Handle(UpdateAccountCommand request, CancellationToken cancellationToken)
    {
        var account = await _accountRepository.GetByIdAsync(request.Id);
        if (account == null)
            throw new ArgumentException($"La cuenta contable con Id '{request.Id}' no existe.");

        account.Name = request.Name;
        account.IsActive = request.IsActive;

        if (request.ParentAccountId.HasValue)
        {
            if (request.ParentAccountId.Value == account.Id)
                throw new InvalidOperationException("Una cuenta no puede ser su propio padre.");

            var parent = await _accountRepository.GetByIdAsync(request.ParentAccountId.Value);
            if (parent == null)
                throw new ArgumentException("La cuenta padre especificada no existe.");
            
            account.ParentAccountId = request.ParentAccountId;
            account.Level = parent.Level + 1;
        }
        else
        {
            account.ParentAccountId = null;
            account.Level = 1;
        }

        _accountRepository.Update(account);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return Unit.Value;
    }
}
