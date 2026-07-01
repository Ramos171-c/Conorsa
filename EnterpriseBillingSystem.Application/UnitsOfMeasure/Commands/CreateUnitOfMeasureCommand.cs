using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using FluentValidation;
using EnterpriseBillingSystem.Domain.Entities;
using EnterpriseBillingSystem.Domain.Repositories;

namespace EnterpriseBillingSystem.Application.UnitsOfMeasure.Commands;

public record CreateUnitOfMeasureCommand(
    string Code,
    string Name
) : IRequest<Guid>;

public class CreateUnitOfMeasureCommandValidator : AbstractValidator<CreateUnitOfMeasureCommand>
{
    private readonly IRepository<UnitOfMeasure> _uomRepository;

    public CreateUnitOfMeasureCommandValidator(IRepository<UnitOfMeasure> uomRepository)
    {
        _uomRepository = uomRepository;

        RuleFor(x => x.Code)
            .NotEmpty().WithMessage("El código de la unidad de medida es requerido.")
            .MaximumLength(20).WithMessage("El código no puede exceder 20 caracteres.")
            .MustAsync(async (code, cancellation) =>
            {
                var existing = await _uomRepository.FindAsync(u => u.Code == code);
                return !existing.Any();
            }).WithMessage("Ya existe una unidad de medida activa con este código.");

        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("El nombre es requerido.")
            .MaximumLength(100).WithMessage("El nombre no puede exceder 100 caracteres.");
    }
}

public class CreateUnitOfMeasureCommandHandler : IRequestHandler<CreateUnitOfMeasureCommand, Guid>
{
    private readonly IRepository<UnitOfMeasure> _uomRepository;
    private readonly IUnitOfWork _unitOfWork;

    public CreateUnitOfMeasureCommandHandler(
        IRepository<UnitOfMeasure> uomRepository,
        IUnitOfWork unitOfWork)
    {
        _uomRepository = uomRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<Guid> Handle(CreateUnitOfMeasureCommand request, CancellationToken cancellationToken)
    {
        var uom = new UnitOfMeasure
        {
            Code = request.Code,
            Name = request.Name,
            IsActive = true
        };

        await _uomRepository.AddAsync(uom);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return uom.Id;
    }
}
