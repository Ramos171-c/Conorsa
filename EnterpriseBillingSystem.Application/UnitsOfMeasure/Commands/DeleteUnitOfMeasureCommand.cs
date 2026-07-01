using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using FluentValidation;
using EnterpriseBillingSystem.Domain.Entities;
using EnterpriseBillingSystem.Domain.Repositories;

namespace EnterpriseBillingSystem.Application.UnitsOfMeasure.Commands;

public record DeleteUnitOfMeasureCommand(Guid Id) : IRequest<bool>;

public class DeleteUnitOfMeasureCommandValidator : AbstractValidator<DeleteUnitOfMeasureCommand>
{
    private readonly IRepository<Product> _productRepository;
    private readonly IRepository<ProductPresentation> _presentationRepository;

    public DeleteUnitOfMeasureCommandValidator(
        IRepository<Product> productRepository,
        IRepository<ProductPresentation> presentationRepository)
    {
        _productRepository = productRepository;
        _presentationRepository = presentationRepository;

        RuleFor(x => x.Id)
            .NotEmpty().WithMessage("El Id es requerido.")
            .MustAsync(async (id, cancellation) =>
            {
                var products = await _productRepository.FindAsync(p => p.DefaultUnitOfMeasureId == id);
                return !products.Any();
            }).WithMessage("No se puede eliminar la unidad de medida porque es la unidad por defecto de algún producto.")
            .MustAsync(async (id, cancellation) =>
            {
                var presentations = await _presentationRepository.FindAsync(p => p.UnitOfMeasureId == id && !p.IsDeleted);
                return !presentations.Any();
            }).WithMessage("No se puede eliminar la unidad de medida porque está siendo utilizada en alguna presentación de producto.");
    }
}

public class DeleteUnitOfMeasureCommandHandler : IRequestHandler<DeleteUnitOfMeasureCommand, bool>
{
    private readonly IRepository<UnitOfMeasure> _uomRepository;
    private readonly IUnitOfWork _unitOfWork;

    public DeleteUnitOfMeasureCommandHandler(
        IRepository<UnitOfMeasure> uomRepository,
        IUnitOfWork unitOfWork)
    {
        _uomRepository = uomRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<bool> Handle(DeleteUnitOfMeasureCommand request, CancellationToken cancellationToken)
    {
        var uom = await _uomRepository.GetByIdAsync(request.Id);
        if (uom == null) return false;

        uom.IsDeleted = true;
        _uomRepository.Update(uom);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return true;
    }
}
