using System;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using FluentValidation;
using EnterpriseBillingSystem.Domain.Entities;
using EnterpriseBillingSystem.Domain.Repositories;

namespace EnterpriseBillingSystem.Application.CustomerCategories.Commands;

public record UpdateCustomerCategoryCommand(
    Guid Id,
    string Name,
    string? Description,
    decimal DefaultDiscountPercentage
) : IRequest<bool>;

public class UpdateCustomerCategoryCommandValidator : AbstractValidator<UpdateCustomerCategoryCommand>
{
    private readonly ICustomerCategoryRepository _customerCategoryRepository;

    public UpdateCustomerCategoryCommandValidator(ICustomerCategoryRepository customerCategoryRepository)
    {
        _customerCategoryRepository = customerCategoryRepository;

        RuleFor(x => x.Id)
            .NotEmpty().WithMessage("El ID de la categoría es requerido.");

        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("El nombre de la categoría es requerido.")
            .MaximumLength(100).WithMessage("El nombre no puede exceder 100 caracteres.")
            .MustAsync(async (cmd, name, cancellation) =>
            {
                return !await _customerCategoryRepository.ExistsByNameAsync(name, cmd.Id, cancellation);
            }).WithMessage("Ya existe otra categoría de cliente con este nombre.");

        RuleFor(x => x.Description)
            .MaximumLength(250).WithMessage("La descripción no puede exceder 250 caracteres.");

        RuleFor(x => x.DefaultDiscountPercentage)
            .InclusiveBetween(0.00m, 100.00m).WithMessage("El porcentaje de descuento debe estar entre 0% y 100%.");
    }
}

public class UpdateCustomerCategoryCommandHandler : IRequestHandler<UpdateCustomerCategoryCommand, bool>
{
    private readonly ICustomerCategoryRepository _customerCategoryRepository;
    private readonly IUnitOfWork _unitOfWork;

    public UpdateCustomerCategoryCommandHandler(
        ICustomerCategoryRepository customerCategoryRepository,
        IUnitOfWork unitOfWork)
    {
        _customerCategoryRepository = customerCategoryRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<bool> Handle(UpdateCustomerCategoryCommand request, CancellationToken cancellationToken)
    {
        var category = await _customerCategoryRepository.GetByIdAsync(request.Id);
        if (category == null) return false;

        category.Name = request.Name;
        category.Description = request.Description;
        category.DefaultDiscountPercentage = request.DefaultDiscountPercentage;

        _customerCategoryRepository.Update(category);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return true;
    }
}
