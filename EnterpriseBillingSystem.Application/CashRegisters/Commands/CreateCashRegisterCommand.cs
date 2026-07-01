using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using FluentValidation;
using EnterpriseBillingSystem.Domain.Entities;
using EnterpriseBillingSystem.Domain.Repositories;

namespace EnterpriseBillingSystem.Application.CashRegisters.Commands;

public record CreateCashRegisterCommand(
    string Code,
    string Name,
    Guid BranchId,
    bool IsDefault
) : IRequest<Guid>;

public class CreateCashRegisterCommandValidator : AbstractValidator<CreateCashRegisterCommand>
{
    private readonly ICashRegisterRepository _cashRegisterRepository;
    private readonly IRepository<Branch> _branchRepository;

    public CreateCashRegisterCommandValidator(
        ICashRegisterRepository cashRegisterRepository,
        IRepository<Branch> branchRepository)
    {
        _cashRegisterRepository = cashRegisterRepository;
        _branchRepository = branchRepository;

        RuleFor(x => x.BranchId)
            .NotEmpty().WithMessage("La sucursal es requerida.")
            .MustAsync(async (branchId, cancellation) =>
            {
                var branch = await _branchRepository.GetByIdAsync(branchId);
                return branch != null && branch.IsActive;
            }).WithMessage("La sucursal especificada no existe o no está activa.");

        RuleFor(x => x.Code)
            .NotEmpty().WithMessage("El código de la caja es requerido.")
            .MaximumLength(30).WithMessage("El código no puede exceder los 30 caracteres.");

        RuleFor(x => x)
            .MustAsync(async (cmd, cancellation) =>
            {
                var existing = await _cashRegisterRepository.FindAsync(r => r.BranchId == cmd.BranchId && r.Code == cmd.Code.ToUpper());
                return !existing.Any();
            }).WithMessage("Ya existe una caja activa con este código en la sucursal.")
            .WithName("Code");

        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("El nombre de la caja es requerido.")
            .MaximumLength(100).WithMessage("El nombre no puede exceder los 100 caracteres.");
    }
}

public class CreateCashRegisterCommandHandler : IRequestHandler<CreateCashRegisterCommand, Guid>
{
    private readonly ICashRegisterRepository _cashRegisterRepository;
    private readonly IUnitOfWork _unitOfWork;

    public CreateCashRegisterCommandHandler(
        ICashRegisterRepository cashRegisterRepository,
        IUnitOfWork unitOfWork)
    {
        _cashRegisterRepository = cashRegisterRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<Guid> Handle(CreateCashRegisterCommand request, CancellationToken cancellationToken)
    {
        // Si se define como predeterminada, quitamos la bandera de la anterior caja default de la sucursal
        if (request.IsDefault)
        {
            var existingDefault = await _cashRegisterRepository.GetDefaultRegisterByBranchAsync(request.BranchId, cancellationToken);
            if (existingDefault != null)
            {
                existingDefault.IsDefault = false;
                _cashRegisterRepository.Update(existingDefault);
            }
        }

        var register = new CashRegister
        {
            Code = request.Code.ToUpper(),
            Name = request.Name,
            BranchId = request.BranchId,
            IsDefault = request.IsDefault,
            IsActive = true,
            CreatedBy = "System",
            CreatedOnUtc = DateTime.UtcNow
        };

        await _cashRegisterRepository.AddAsync(register);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return register.Id;
    }
}
