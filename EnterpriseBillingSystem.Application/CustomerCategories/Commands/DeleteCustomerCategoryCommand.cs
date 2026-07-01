using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using FluentValidation;
using EnterpriseBillingSystem.Domain.Repositories;

namespace EnterpriseBillingSystem.Application.CustomerCategories.Commands;

public record DeleteCustomerCategoryCommand(Guid Id) : IRequest<bool>;

public class DeleteCustomerCategoryCommandValidator : AbstractValidator<DeleteCustomerCategoryCommand>
{
    public DeleteCustomerCategoryCommandValidator()
    {
        RuleFor(x => x.Id)
            .NotEmpty().WithMessage("El ID de la categoría es requerido.");
    }
}

public class DeleteCustomerCategoryCommandHandler : IRequestHandler<DeleteCustomerCategoryCommand, bool>
{
    private readonly ICustomerCategoryRepository _customerCategoryRepository;
    private readonly ICustomerRepository _customerRepository;
    private readonly IUnitOfWork _unitOfWork;

    public DeleteCustomerCategoryCommandHandler(
        ICustomerCategoryRepository customerCategoryRepository,
        ICustomerRepository customerRepository,
        IUnitOfWork unitOfWork)
    {
        _customerCategoryRepository = customerCategoryRepository;
        _customerRepository = customerRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<bool> Handle(DeleteCustomerCategoryCommand request, CancellationToken cancellationToken)
    {
        var category = await _customerCategoryRepository.GetByIdAsync(request.Id);
        if (category == null) return false;

        // Check if there are active customers in this category
        var hasCustomers = await _customerRepository.FindAsync(c => c.CustomerCategoryId == request.Id);
        if (hasCustomers.Any())
        {
            throw new InvalidOperationException("No se puede eliminar la categoría porque tiene clientes asociados.");
        }

        _customerCategoryRepository.Remove(category);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return true;
    }
}
