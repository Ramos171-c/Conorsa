using System;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using FluentValidation;
using EnterpriseBillingSystem.Domain.Repositories;

namespace EnterpriseBillingSystem.Application.Banks.Commands;

public record UpdateBankCommand(
    Guid Id,
    string Name,
    string SwiftCode,
    string Country,
    bool IsActive
) : IRequest<bool>;

public class UpdateBankCommandValidator : AbstractValidator<UpdateBankCommand>
{
    public UpdateBankCommandValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("El nombre del banco es requerido.")
            .MaximumLength(200).WithMessage("El nombre no puede exceder 200 caracteres.");

        RuleFor(x => x.SwiftCode)
            .MaximumLength(20).WithMessage("El código SWIFT no puede exceder 20 caracteres.");

        RuleFor(x => x.Country)
            .MaximumLength(100).WithMessage("El país no puede exceder 100 caracteres.");
    }
}

public class UpdateBankCommandHandler : IRequestHandler<UpdateBankCommand, bool>
{
    private readonly IBankRepository _bankRepository;
    private readonly IUnitOfWork _unitOfWork;

    public UpdateBankCommandHandler(IBankRepository bankRepository, IUnitOfWork unitOfWork)
    {
        _bankRepository = bankRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<bool> Handle(UpdateBankCommand request, CancellationToken cancellationToken)
    {
        var bank = await _bankRepository.GetByIdAsync(request.Id);
        if (bank == null) return false;

        bank.Name = request.Name;
        bank.SwiftCode = request.SwiftCode;
        bank.Country = request.Country;
        bank.IsActive = request.IsActive;

        _bankRepository.Update(bank);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return true;
    }
}
