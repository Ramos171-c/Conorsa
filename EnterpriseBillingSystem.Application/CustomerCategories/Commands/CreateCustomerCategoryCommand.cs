using System;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using FluentValidation;
using EnterpriseBillingSystem.Domain.Entities;
using EnterpriseBillingSystem.Domain.Repositories;

namespace EnterpriseBillingSystem.Application.CustomerCategories.Commands;

public record CreateCustomerCategoryCommand(
    string Name,
    string? Description,
    decimal DefaultDiscountPercentage
) : IRequest<Guid>;

public class CreateCustomerCategoryCommandValidator : AbstractValidator<CreateCustomerCategoryCommand>
{
    private readonly ICustomerCategoryRepository _customerCategoryRepository;

    public CreateCustomerCategoryCommandValidator(ICustomerCategoryRepository customerCategoryRepository)
    {
        _customerCategoryRepository = customerCategoryRepository;

        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("El nombre de la categoría es requerido.")
            .MaximumLength(100).WithMessage("El nombre no puede exceder 100 caracteres.")
            .MustAsync(async (name, cancellation) =>
            {
                return !await _customerCategoryRepository.ExistsByNameAsync(name, null, cancellation);
            }).WithMessage("Ya existe una categoría de cliente con este nombre.");

        RuleFor(x => x.Description)
            .MaximumLength(250).WithMessage("La descripción no puede exceder 250 caracteres.");

        RuleFor(x => x.DefaultDiscountPercentage)
            .InclusiveBetween(0.00m, 100.00m).WithMessage("El porcentaje de descuento debe estar entre 0% y 100%.");
    }
}

public class CreateCustomerCategoryCommandHandler : IRequestHandler<CreateCustomerCategoryCommand, Guid>
{
    private readonly ICustomerCategoryRepository _customerCategoryRepository;
    private readonly IUnitOfWork _unitOfWork;

    public CreateCustomerCategoryCommandHandler(
        ICustomerCategoryRepository customerCategoryRepository,
        IUnitOfWork unitOfWork)
    {
        _customerCategoryRepository = customerCategoryRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<Guid> Handle(CreateCustomerCategoryCommand request, CancellationToken cancellationToken)
    {
        var category = new CustomerCategory
        {
            Name = request.Name,
            Description = request.Description,
            DefaultDiscountPercentage = request.DefaultDiscountPercentage
        };

        await _customerCategoryRepository.AddAsync(category);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return category.Id;
    }
}
