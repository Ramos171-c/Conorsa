using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using FluentValidation;
using EnterpriseBillingSystem.Domain.Entities;
using EnterpriseBillingSystem.Domain.Repositories;

namespace EnterpriseBillingSystem.Application.Taxes.Commands;

public record CreateTaxCommand(
    string Name,
    decimal Rate
) : IRequest<Guid>;

public class CreateTaxCommandValidator : AbstractValidator<CreateTaxCommand>
{
    private readonly IRepository<Tax> _taxRepository;

    public CreateTaxCommandValidator(IRepository<Tax> taxRepository)
    {
        _taxRepository = taxRepository;

        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("El nombre del impuesto es requerido.")
            .MaximumLength(100).WithMessage("El nombre no puede exceder 100 caracteres.")
            .MustAsync(async (name, cancellation) =>
            {
                var existing = await _taxRepository.FindAsync(t => t.Name == name);
                return !existing.Any();
            }).WithMessage("Ya existe un impuesto activo con este nombre.");

        RuleFor(x => x.Rate)
            .GreaterThanOrEqualTo(0).WithMessage("La tasa del impuesto no puede ser negativa.");
    }
}

public class CreateTaxCommandHandler : IRequestHandler<CreateTaxCommand, Guid>
{
    private readonly IRepository<Tax> _taxRepository;
    private readonly IUnitOfWork _unitOfWork;

    public CreateTaxCommandHandler(
        IRepository<Tax> taxRepository,
        IUnitOfWork unitOfWork)
    {
        _taxRepository = taxRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<Guid> Handle(CreateTaxCommand request, CancellationToken cancellationToken)
    {
        var tax = new Tax
        {
            Name = request.Name,
            Rate = request.Rate,
            IsActive = true
        };

        await _taxRepository.AddAsync(tax);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return tax.Id;
    }
}
