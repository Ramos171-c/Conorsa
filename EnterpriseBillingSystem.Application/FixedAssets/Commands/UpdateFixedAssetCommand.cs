using System;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using FluentValidation;
using EnterpriseBillingSystem.Domain.Repositories;

namespace EnterpriseBillingSystem.Application.FixedAssets.Commands;

public record UpdateFixedAssetCommand(
    Guid Id,
    string Name,
    string? Description,
    DateTime DepreciationStartDate,
    decimal ResidualValue,
    int UsefulLifeMonths,
    string? Location,
    string? SerialNumber,
    string? Notes
) : IRequest<bool>;

public class UpdateFixedAssetCommandValidator : AbstractValidator<UpdateFixedAssetCommand>
{
    public UpdateFixedAssetCommandValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("El nombre es requerido.")
            .MaximumLength(200);

        RuleFor(x => x.UsefulLifeMonths)
            .GreaterThan(0).WithMessage("La vida útil debe ser mayor a 0 meses.");
    }
}

public class UpdateFixedAssetCommandHandler : IRequestHandler<UpdateFixedAssetCommand, bool>
{
    private readonly IFixedAssetRepository _assetRepository;
    private readonly IUnitOfWork _unitOfWork;

    public UpdateFixedAssetCommandHandler(
        IFixedAssetRepository assetRepository,
        IUnitOfWork unitOfWork)
    {
        _assetRepository = assetRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<bool> Handle(UpdateFixedAssetCommand request, CancellationToken cancellationToken)
    {
        var asset = await _assetRepository.GetByIdAsync(request.Id);
        if (asset == null) return false;

        // Solo se puede editar si no ha iniciado depreciación
        if (asset.AccumulatedDepreciation > 0)
            throw new InvalidOperationException(
                "No se puede modificar un activo que ya ha comenzado el proceso de depreciación. " +
                "Utilice revalorización o ajustes contables.");

        asset.Name = request.Name;
        asset.Description = request.Description;
        asset.DepreciationStartDate = request.DepreciationStartDate;
        asset.ResidualValue = request.ResidualValue;
        asset.UsefulLifeMonths = request.UsefulLifeMonths;
        asset.Location = request.Location;
        asset.SerialNumber = request.SerialNumber;
        asset.Notes = request.Notes;

        _assetRepository.Update(asset);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return true;
    }
}
