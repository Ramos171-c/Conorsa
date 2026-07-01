using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using FluentValidation;
using EnterpriseBillingSystem.Domain.Entities;
using EnterpriseBillingSystem.Domain.Repositories;

namespace EnterpriseBillingSystem.Application.UnitsOfMeasure.Commands;

public record UpdateUnitOfMeasureCommand(
    Guid Id,
    string Code,
    string Name,
    bool IsActive
) : IRequest<bool>;

public class UpdateUnitOfMeasureCommandValidator : AbstractValidator<UpdateUnitOfMeasureCommand>
{
    private readonly IRepository<UnitOfMeasure> _uomRepository;

    public UpdateUnitOfMeasureCommandValidator(IRepository<UnitOfMeasure> uomRepository)
    {
        _uomRepository = uomRepository;

        RuleFor(x => x.Id)
            .NotEmpty().WithMessage("El Id es requerido.");

        RuleFor(x => x.Code)
            .NotEmpty().WithMessage("El código de la unidad de medida es requerido.")
            .MaximumLength(20).WithMessage("El código no puede exceder 20 caracteres.")
            .MustAsync(async (command, code, cancellation) =>
            {
                var existing = await _uomRepository.FindAsync(u => u.Code == code && u.Id != command.Id);
                return !existing.Any();
            }).WithMessage("Ya existe otra unidad de medida activa con este código.");

        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("El nombre es requerido.")
            .MaximumLength(100).WithMessage("El nombre no puede exceder 100 caracteres.");
    }
}

public class UpdateUnitOfMeasureCommandHandler : IRequestHandler<UpdateUnitOfMeasureCommand, bool>
{
    private readonly IRepository<UnitOfMeasure> _uomRepository;
    private readonly IUnitOfWork _unitOfWork;

    public UpdateUnitOfMeasureCommandHandler(
        IRepository<UnitOfMeasure> uomRepository,
        IUnitOfWork unitOfWork)
    {
        _uomRepository = uomRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<bool> Handle(UpdateUnitOfMeasureCommand request, CancellationToken cancellationToken)
    {
        var uom = await _uomRepository.GetByIdAsync(request.Id);
        if (uom == null) return false;

        uom.Code = request.Code;
        uom.Name = request.Name;
        uom.IsActive = request.IsActive;

        _uomRepository.Update(uom);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return true;
    }
}
