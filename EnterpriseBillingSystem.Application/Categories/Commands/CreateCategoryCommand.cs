using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using FluentValidation;
using EnterpriseBillingSystem.Domain.Entities;
using EnterpriseBillingSystem.Domain.Repositories;

namespace EnterpriseBillingSystem.Application.Categories.Commands;

public record CreateCategoryCommand(
    string Name,
    string? Description,
    Guid? ParentCategoryId
) : IRequest<Guid>;

public class CreateCategoryCommandValidator : AbstractValidator<CreateCategoryCommand>
{
    private readonly IRepository<Category> _categoryRepository;

    public CreateCategoryCommandValidator(IRepository<Category> categoryRepository)
    {
        _categoryRepository = categoryRepository;

        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("El nombre de la categoría es requerido.")
            .MaximumLength(100).WithMessage("El nombre no puede exceder 100 caracteres.")
            .MustAsync(async (name, cancellation) =>
            {
                var existing = await _categoryRepository.FindAsync(c => c.Name == name);
                return !existing.Any();
            }).WithMessage("Ya existe una categoría activa con este nombre.");

        RuleFor(x => x.Description)
            .MaximumLength(250).WithMessage("La descripción no puede exceder 250 caracteres.");

        RuleFor(x => x.ParentCategoryId)
            .MustAsync(async (parentId, cancellation) =>
            {
                if (parentId == null) return true;
                var parent = await _categoryRepository.GetByIdAsync(parentId.Value);
                return parent != null;
            }).WithMessage("La categoría padre especificada no existe.");
    }
}

public class CreateCategoryCommandHandler : IRequestHandler<CreateCategoryCommand, Guid>
{
    private readonly IRepository<Category> _categoryRepository;
    private readonly IUnitOfWork _unitOfWork;

    public CreateCategoryCommandHandler(
        IRepository<Category> categoryRepository,
        IUnitOfWork unitOfWork)
    {
        _categoryRepository = categoryRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<Guid> Handle(CreateCategoryCommand request, CancellationToken cancellationToken)
    {
        var category = new Category
        {
            Name = request.Name,
            Description = request.Description,
            ParentCategoryId = request.ParentCategoryId,
            IsActive = true
        };

        await _categoryRepository.AddAsync(category);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return category.Id;
    }
}
