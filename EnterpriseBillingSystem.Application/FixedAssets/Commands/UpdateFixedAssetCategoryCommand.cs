using System;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using FluentValidation;
using EnterpriseBillingSystem.Domain.Repositories;

namespace EnterpriseBillingSystem.Application.FixedAssets.Commands;

public record UpdateFixedAssetCategoryCommand(
    Guid Id,
    string Name,
    string AssetAccountCode,
    string AccumulatedDepreciationAccountCode,
    string DepreciationExpenseAccountCode,
    int UsefulLifeMonths,
    bool IsActive
) : IRequest<bool>;

public class UpdateFixedAssetCategoryCommandValidator : AbstractValidator<UpdateFixedAssetCategoryCommand>
{
    public UpdateFixedAssetCategoryCommandValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("El nombre es requerido.")
            .MaximumLength(200);

        RuleFor(x => x.AssetAccountCode)
            .NotEmpty().MaximumLength(50);

        RuleFor(x => x.AccumulatedDepreciationAccountCode)
            .NotEmpty().MaximumLength(50);

        RuleFor(x => x.DepreciationExpenseAccountCode)
            .NotEmpty().MaximumLength(50);

        RuleFor(x => x.UsefulLifeMonths)
            .GreaterThan(0).WithMessage("La vida útil debe ser mayor a 0 meses.");
    }
}

public class UpdateFixedAssetCategoryCommandHandler : IRequestHandler<UpdateFixedAssetCategoryCommand, bool>
{
    private readonly IFixedAssetCategoryRepository _categoryRepository;
    private readonly IUnitOfWork _unitOfWork;

    public UpdateFixedAssetCategoryCommandHandler(
        IFixedAssetCategoryRepository categoryRepository,
        IUnitOfWork unitOfWork)
    {
        _categoryRepository = categoryRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<bool> Handle(UpdateFixedAssetCategoryCommand request, CancellationToken cancellationToken)
    {
        var category = await _categoryRepository.GetByIdAsync(request.Id);
        if (category == null) return false;

        category.Name = request.Name;
        category.AssetAccountCode = request.AssetAccountCode;
        category.AccumulatedDepreciationAccountCode = request.AccumulatedDepreciationAccountCode;
        category.DepreciationExpenseAccountCode = request.DepreciationExpenseAccountCode;
        category.UsefulLifeMonths = request.UsefulLifeMonths;
        category.IsActive = request.IsActive;

        _categoryRepository.Update(category);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return true;
    }
}
