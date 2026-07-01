using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using FluentValidation;
using EnterpriseBillingSystem.Domain.Entities;
using EnterpriseBillingSystem.Domain.Repositories;

namespace EnterpriseBillingSystem.Application.Taxes.Commands;

public record DeleteTaxCommand(Guid Id) : IRequest<bool>;

public class DeleteTaxCommandValidator : AbstractValidator<DeleteTaxCommand>
{
    private readonly IRepository<Product> _productRepository;

    public DeleteTaxCommandValidator(IRepository<Product> productRepository)
    {
        _productRepository = productRepository;

        RuleFor(x => x.Id)
            .NotEmpty().WithMessage("El Id es requerido.")
            .MustAsync(async (id, cancellation) =>
            {
                var productsWithTax = await _productRepository.FindAsync(p => p.TaxId == id && !p.IsDeleted);
                return !productsWithTax.Any();
            }).WithMessage("No se puede eliminar el impuesto porque está siendo utilizado por algún producto.");
    }
}

public class DeleteTaxCommandHandler : IRequestHandler<DeleteTaxCommand, bool>
{
    private readonly IRepository<Tax> _taxRepository;
    private readonly IUnitOfWork _unitOfWork;

    public DeleteTaxCommandHandler(
        IRepository<Tax> taxRepository,
        IUnitOfWork unitOfWork)
    {
        _taxRepository = taxRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<bool> Handle(DeleteTaxCommand request, CancellationToken cancellationToken)
    {
        var tax = await _taxRepository.GetByIdAsync(request.Id);
        if (tax == null) return false;

        tax.IsDeleted = true;
        _taxRepository.Update(tax);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return true;
    }
}
