using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using FluentValidation;
using EnterpriseBillingSystem.Application.Common.Interfaces;
using EnterpriseBillingSystem.Application.JournalEntries.Commands;
using EnterpriseBillingSystem.Domain.Entities;
using EnterpriseBillingSystem.Domain.Enums;
using EnterpriseBillingSystem.Domain.Repositories;

namespace EnterpriseBillingSystem.Application.FixedAssets.Commands;

public record CreateFixedAssetCommand(
    string Name,
    string? Description,
    Guid FixedAssetCategoryId,
    Guid? PurchaseInvoiceId,
    DateTime AcquisitionDate,
    decimal AcquisitionCost,
    decimal ResidualValue,
    int UsefulLifeMonths,
    DateTime DepreciationStartDate,
    /// <summary>Código contable del origen del pago (ej: 1110=Caja, 1121=Banco, 2100=CxP)</summary>
    string SourceAccountCode,
    string? Location,
    string? SerialNumber,
    string? Notes
) : IRequest<Guid>;

public class CreateFixedAssetCommandValidator : AbstractValidator<CreateFixedAssetCommand>
{
    public CreateFixedAssetCommandValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("El nombre del activo es requerido.")
            .MaximumLength(200);

        RuleFor(x => x.FixedAssetCategoryId)
            .NotEmpty().WithMessage("La categoría del activo es requerida.");

        RuleFor(x => x.AcquisitionCost)
            .GreaterThan(0).WithMessage("El costo de adquisición debe ser mayor a 0.");

        RuleFor(x => x.ResidualValue)
            .GreaterThanOrEqualTo(0).WithMessage("El valor residual no puede ser negativo.")
            .LessThan(x => x.AcquisitionCost).WithMessage("El valor residual debe ser menor al costo de adquisición.");

        RuleFor(x => x.UsefulLifeMonths)
            .GreaterThan(0).WithMessage("La vida útil debe ser mayor a 0 meses.");

        RuleFor(x => x.SourceAccountCode)
            .NotEmpty().WithMessage("El código de cuenta origen es requerido.")
            .MaximumLength(50);

        RuleFor(x => x.AcquisitionDate)
            .NotEmpty().WithMessage("La fecha de adquisición es requerida.");

        RuleFor(x => x.DepreciationStartDate)
            .NotEmpty().WithMessage("La fecha de inicio de depreciación es requerida.")
            .GreaterThanOrEqualTo(x => x.AcquisitionDate)
            .WithMessage("La fecha de inicio de depreciación debe ser igual o posterior a la de adquisición.");
    }
}

public class CreateFixedAssetCommandHandler : IRequestHandler<CreateFixedAssetCommand, Guid>
{
    private readonly IFixedAssetRepository _assetRepository;
    private readonly IFixedAssetCategoryRepository _categoryRepository;
    private readonly IFixedAssetTransactionRepository _transactionRepository;
    private readonly ICurrentUserService _currentUserService;
    private readonly IMediator _mediator;
    private readonly IUnitOfWork _unitOfWork;

    public CreateFixedAssetCommandHandler(
        IFixedAssetRepository assetRepository,
        IFixedAssetCategoryRepository categoryRepository,
        IFixedAssetTransactionRepository transactionRepository,
        ICurrentUserService currentUserService,
        IMediator mediator,
        IUnitOfWork unitOfWork)
    {
        _assetRepository = assetRepository;
        _categoryRepository = categoryRepository;
        _transactionRepository = transactionRepository;
        _currentUserService = currentUserService;
        _mediator = mediator;
        _unitOfWork = unitOfWork;
    }

    public async Task<Guid> Handle(CreateFixedAssetCommand request, CancellationToken cancellationToken)
    {
        // 1. Validar categoría activa
        var category = await _categoryRepository.GetByIdAsync(request.FixedAssetCategoryId);
        if (category == null || !category.IsActive)
            throw new ArgumentException("La categoría especificada no existe o no está activa.");

        // 2. Generar número de activo correlativo
        var assetNumber = await _assetRepository.GenerateAssetNumberAsync(cancellationToken);

        // 3. Crear activo
        var asset = new FixedAsset
        {
            AssetNumber = assetNumber,
            Name = request.Name,
            Description = request.Description,
            FixedAssetCategoryId = request.FixedAssetCategoryId,
            PurchaseInvoiceId = request.PurchaseInvoiceId,
            AcquisitionDate = request.AcquisitionDate,
            AcquisitionCost = request.AcquisitionCost,
            ResidualValue = request.ResidualValue,
            UsefulLifeMonths = request.UsefulLifeMonths,
            DepreciationStartDate = request.DepreciationStartDate,
            AccumulatedDepreciation = 0m,
            CurrentBookValue = request.AcquisitionCost,
            Status = FixedAssetStatus.Active,
            Location = request.Location,
            SerialNumber = request.SerialNumber,
            Notes = request.Notes
        };

        await _assetRepository.AddAsync(asset);

        // 4. Registrar transacción de adquisición
        var acquisitionTx = new FixedAssetTransaction
        {
            FixedAssetId = asset.Id,
            TransactionDate = request.AcquisitionDate,
            TransactionType = FixedAssetTransactionType.Acquisition,
            Amount = request.AcquisitionCost,
            Notes = $"Adquisición de activo {assetNumber}"
        };

        await _transactionRepository.AddAsync(acquisitionTx);

        // 5. Generar JournalEntry: Dr Cuenta Activo / Cr Cuenta Origen
        var jeDetails = new List<JournalEntryDetailInput>
        {
            new JournalEntryDetailInput(category.AssetAccountCode, request.AcquisitionCost, 0,
                $"Adquisición Activo {assetNumber} — {request.Name}"),
            new JournalEntryDetailInput(request.SourceAccountCode, 0, request.AcquisitionCost,
                $"Adquisición Activo {assetNumber} — {request.Name}")
        };

        var createJeCmd = new CreateJournalEntryCommand(
            EntryDate: request.AcquisitionDate,
            Description: $"Adquisición de Activo Fijo {assetNumber} — {request.Name}",
            ReferenceDocument: assetNumber,
            ReferenceId: asset.Id,
            SourceModule: "FixedAssets",
            Details: jeDetails,
            PostImmediately: true
        );

        await _mediator.Send(createJeCmd, cancellationToken);

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return asset.Id;
    }
}
