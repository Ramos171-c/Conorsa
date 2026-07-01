using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using FluentValidation;
using EnterpriseBillingSystem.Domain.Entities;
using EnterpriseBillingSystem.Domain.Repositories;

namespace EnterpriseBillingSystem.Application.Brands.Commands;

public record DeleteBrandCommand(Guid Id) : IRequest<bool>;

public class DeleteBrandCommandValidator : AbstractValidator<DeleteBrandCommand>
{
    private readonly IRepository<Product> _productRepository;

    public DeleteBrandCommandValidator(IRepository<Product> productRepository)
    {
        _productRepository = productRepository;

        RuleFor(x => x.Id)
            .NotEmpty().WithMessage("El Id es requerido.")
            .MustAsync(async (id, cancellation) =>
            {
                var products = await _productRepository.FindAsync(p => p.BrandId == id);
                return !products.Any();
            }).WithMessage("No se puede eliminar la marca porque contiene productos asociados.");
    }
}

public class DeleteBrandCommandHandler : IRequestHandler<DeleteBrandCommand, bool>
{
    private readonly IRepository<Brand> _brandRepository;
    private readonly IUnitOfWork _unitOfWork;

    public DeleteBrandCommandHandler(
        IRepository<Brand> brandRepository,
        IUnitOfWork unitOfWork)
    {
        _brandRepository = brandRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<bool> Handle(DeleteBrandCommand request, CancellationToken cancellationToken)
    {
        var brand = await _brandRepository.GetByIdAsync(request.Id);
        if (brand == null) return false;

        brand.IsDeleted = true;
        _brandRepository.Update(brand);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return true;
    }
}
