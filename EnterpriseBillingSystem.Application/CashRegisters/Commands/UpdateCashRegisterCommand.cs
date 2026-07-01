using System;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using FluentValidation;
using EnterpriseBillingSystem.Domain.Repositories;

namespace EnterpriseBillingSystem.Application.CashRegisters.Commands;

public record UpdateCashRegisterCommand(
    Guid Id,
    string Name,
    bool IsDefault,
    bool IsActive
) : IRequest<Unit>;

public class UpdateCashRegisterCommandValidator : AbstractValidator<UpdateCashRegisterCommand>
{
    public UpdateCashRegisterCommandValidator()
    {
        RuleFor(x => x.Id)
            .NotEmpty().WithMessage("El Id de la caja es requerido.");

        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("El nombre de la caja es requerido.")
            .MaximumLength(100).WithMessage("El nombre no puede exceder los 100 caracteres.");
    }
}

public class UpdateCashRegisterCommandHandler : IRequestHandler<UpdateCashRegisterCommand, Unit>
{
    private readonly ICashRegisterRepository _cashRegisterRepository;
    private readonly IUnitOfWork _unitOfWork;

    public UpdateCashRegisterCommandHandler(
        ICashRegisterRepository cashRegisterRepository,
        IUnitOfWork unitOfWork)
    {
        _cashRegisterRepository = cashRegisterRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<Unit> Handle(UpdateCashRegisterCommand request, CancellationToken cancellationToken)
    {
        var register = await _cashRegisterRepository.GetByIdAsync(request.Id);
        if (register == null)
            throw new ArgumentException($"La caja con Id '{request.Id}' no existe.");

        // Si cambia a predeterminada, quitamos la bandera de la otra caja
        if (request.IsDefault && !register.IsDefault)
        {
            var existingDefault = await _cashRegisterRepository.GetDefaultRegisterByBranchAsync(register.BranchId ?? Guid.Empty, cancellationToken);
            if (existingDefault != null && existingDefault.Id != register.Id)
            {
                existingDefault.IsDefault = false;
                _cashRegisterRepository.Update(existingDefault);
            }
        }

        register.Name = request.Name;
        register.IsDefault = request.IsDefault;
        register.IsActive = request.IsActive;
        register.LastModifiedBy = "System";
        register.LastModifiedOnUtc = DateTime.UtcNow;

        _cashRegisterRepository.Update(register);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return Unit.Value;
    }
}
