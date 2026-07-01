using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using FluentValidation;
using EnterpriseBillingSystem.Domain.Entities;
using EnterpriseBillingSystem.Domain.Repositories;

namespace EnterpriseBillingSystem.Application.Brands.Commands;

public record CreateBrandCommand(
    string Name,
    string? Description
) : IRequest<Guid>;

public class CreateBrandCommandValidator : AbstractValidator<CreateBrandCommand>
{
    private readonly IRepository<Brand> _brandRepository;

    public CreateBrandCommandValidator(IRepository<Brand> brandRepository)
    {
        _brandRepository = brandRepository;

        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("El nombre de la marca es requerido.")
            .MaximumLength(100).WithMessage("El nombre no puede exceder 100 caracteres.")
            .MustAsync(async (name, cancellation) =>
            {
                var existing = await _brandRepository.FindAsync(b => b.Name == name);
                return !existing.Any();
            }).WithMessage("Ya existe una marca activa con este nombre.");

        RuleFor(x => x.Description)
            .MaximumLength(250).WithMessage("La descripción no puede exceder 250 caracteres.");
    }
}

public class CreateBrandCommandHandler : IRequestHandler<CreateBrandCommand, Guid>
{
    private readonly IRepository<Brand> _brandRepository;
    private readonly IUnitOfWork _unitOfWork;

    public CreateBrandCommandHandler(
        IRepository<Brand> brandRepository,
        IUnitOfWork unitOfWork)
    {
        _brandRepository = brandRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<Guid> Handle(CreateBrandCommand request, CancellationToken cancellationToken)
    {
        var brand = new Brand
        {
            Name = request.Name,
            Description = request.Description,
            IsActive = true
        };

        await _brandRepository.AddAsync(brand);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return brand.Id;
    }
}
