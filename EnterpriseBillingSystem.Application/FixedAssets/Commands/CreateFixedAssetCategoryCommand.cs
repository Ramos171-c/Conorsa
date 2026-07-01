using System;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using FluentValidation;
using EnterpriseBillingSystem.Domain.Entities;
using EnterpriseBillingSystem.Domain.Repositories;

namespace EnterpriseBillingSystem.Application.FixedAssets.Commands;

public record CreateFixedAssetCategoryCommand(
    string Code,
    string Name,
    string AssetAccountCode,
    string AccumulatedDepreciationAccountCode,
    string DepreciationExpenseAccountCode,
    int UsefulLifeMonths
) : IRequest<Guid>;

public class CreateFixedAssetCategoryCommandValidator : AbstractValidator<CreateFixedAssetCategoryCommand>
{
    private readonly IFixedAssetCategoryRepository _categoryRepository;

    public CreateFixedAssetCategoryCommandValidator(IFixedAssetCategoryRepository categoryRepository)
    {
        _categoryRepository = categoryRepository;

        RuleFor(x => x.Code)
            .NotEmpty().WithMessage("El código es requerido.")
            .MaximumLength(20).WithMessage("El código no puede exceder 20 caracteres.")
            .MustAsync(async (code, cancellation) => !await _categoryRepository.ExistsCodeAsync(code, cancellation))
            .WithMessage("Ya existe una categoría con este código.");

        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("El nombre es requerido.")
            .MaximumLength(200).WithMessage("El nombre no puede exceder 200 caracteres.");

        RuleFor(x => x.AssetAccountCode)
            .NotEmpty().WithMessage("El código de cuenta de activo es requerido.")
            .MaximumLength(50);

        RuleFor(x => x.AccumulatedDepreciationAccountCode)
            .NotEmpty().WithMessage("El código de depreciación acumulada es requerido.")
            .MaximumLength(50);

        RuleFor(x => x.DepreciationExpenseAccountCode)
            .NotEmpty().WithMessage("El código de gasto de depreciación es requerido.")
            .MaximumLength(50);

        RuleFor(x => x.UsefulLifeMonths)
            .GreaterThan(0).WithMessage("La vida útil debe ser mayor a 0 meses.");
    }
}

public class CreateFixedAssetCategoryCommandHandler : IRequestHandler<CreateFixedAssetCategoryCommand, Guid>
{
    private readonly IFixedAssetCategoryRepository _categoryRepository;
    private readonly IUnitOfWork _unitOfWork;

    public CreateFixedAssetCategoryCommandHandler(
        IFixedAssetCategoryRepository categoryRepository,
        IUnitOfWork unitOfWork)
    {
        _categoryRepository = categoryRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<Guid> Handle(CreateFixedAssetCategoryCommand request, CancellationToken cancellationToken)
    {
        var category = new FixedAssetCategory
        {
            Code = request.Code.ToUpper(),
            Name = request.Name,
            AssetAccountCode = request.AssetAccountCode,
            AccumulatedDepreciationAccountCode = request.AccumulatedDepreciationAccountCode,
            DepreciationExpenseAccountCode = request.DepreciationExpenseAccountCode,
            UsefulLifeMonths = request.UsefulLifeMonths,
            IsActive = true
        };

        await _categoryRepository.AddAsync(category);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return category.Id;
    }
}
