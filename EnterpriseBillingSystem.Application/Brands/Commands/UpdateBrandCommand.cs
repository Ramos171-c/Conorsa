using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using FluentValidation;
using EnterpriseBillingSystem.Domain.Entities;
using EnterpriseBillingSystem.Domain.Repositories;

namespace EnterpriseBillingSystem.Application.Brands.Commands;

public record UpdateBrandCommand(
    Guid Id,
    string Name,
    string? Description,
    bool IsActive
) : IRequest<bool>;

public class UpdateBrandCommandValidator : AbstractValidator<UpdateBrandCommand>
{
    private readonly IRepository<Brand> _brandRepository;

    public UpdateBrandCommandValidator(IRepository<Brand> brandRepository)
    {
        _brandRepository = brandRepository;

        RuleFor(x => x.Id)
            .NotEmpty().WithMessage("El Id es requerido.");

        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("El nombre de la marca es requerido.")
            .MaximumLength(100).WithMessage("El nombre no puede exceder 100 caracteres.")
            .MustAsync(async (command, name, cancellation) =>
            {
                var existing = await _brandRepository.FindAsync(b => b.Name == name && b.Id != command.Id);
                return !existing.Any();
            }).WithMessage("Ya existe otra marca activa con este nombre.");

        RuleFor(x => x.Description)
            .MaximumLength(250).WithMessage("La descripción no puede exceder 250 caracteres.");
    }
}

public class UpdateBrandCommandHandler : IRequestHandler<UpdateBrandCommand, bool>
{
    private readonly IRepository<Brand> _brandRepository;
    private readonly IUnitOfWork _unitOfWork;

    public UpdateBrandCommandHandler(
        IRepository<Brand> brandRepository,
        IUnitOfWork unitOfWork)
    {
        _brandRepository = brandRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<bool> Handle(UpdateBrandCommand request, CancellationToken cancellationToken)
    {
        var brand = await _brandRepository.GetByIdAsync(request.Id);
        if (brand == null) return false;

        brand.Name = request.Name;
        brand.Description = request.Description;
        brand.IsActive = request.IsActive;

        _brandRepository.Update(brand);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return true;
    }
}
