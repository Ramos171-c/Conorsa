using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using FluentValidation;
using EnterpriseBillingSystem.Domain.Entities;
using EnterpriseBillingSystem.Domain.Repositories;

namespace EnterpriseBillingSystem.Application.Taxes.Commands;

public record UpdateTaxCommand(
    Guid Id,
    string Name,
    decimal Rate,
    bool IsActive
) : IRequest<bool>;

public class UpdateTaxCommandValidator : AbstractValidator<UpdateTaxCommand>
{
    private readonly IRepository<Tax> _taxRepository;

    public UpdateTaxCommandValidator(IRepository<Tax> taxRepository)
    {
        _taxRepository = taxRepository;

        RuleFor(x => x.Id)
            .NotEmpty().WithMessage("El Id es requerido.");

        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("El nombre del impuesto es requerido.")
            .MaximumLength(100).WithMessage("El nombre no puede exceder 100 caracteres.")
            .MustAsync(async (command, name, cancellation) =>
            {
                var existing = await _taxRepository.FindAsync(t => t.Name == name && t.Id != command.Id);
                return !existing.Any();
            }).WithMessage("Ya existe otra marca activa con este nombre.");

        RuleFor(x => x.Rate)
            .GreaterThanOrEqualTo(0).WithMessage("La tasa del impuesto no puede ser negativa.");
    }
}

public class UpdateTaxCommandHandler : IRequestHandler<UpdateTaxCommand, bool>
{
    private readonly IRepository<Tax> _taxRepository;
    private readonly IUnitOfWork _unitOfWork;

    public UpdateTaxCommandHandler(
        IRepository<Tax> taxRepository,
        IUnitOfWork unitOfWork)
    {
        _taxRepository = taxRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<bool> Handle(UpdateTaxCommand request, CancellationToken cancellationToken)
    {
        var tax = await _taxRepository.GetByIdAsync(request.Id);
        if (tax == null) return false;

        tax.Name = request.Name;
        tax.Rate = request.Rate;
        tax.IsActive = request.IsActive;

        _taxRepository.Update(tax);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return true;
    }
}
