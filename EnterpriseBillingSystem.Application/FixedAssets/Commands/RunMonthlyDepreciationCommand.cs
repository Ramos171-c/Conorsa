using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using FluentValidation;
using EnterpriseBillingSystem.Application.JournalEntries.Commands;
using EnterpriseBillingSystem.Domain.Entities;
using EnterpriseBillingSystem.Domain.Enums;
using EnterpriseBillingSystem.Domain.Repositories;

namespace EnterpriseBillingSystem.Application.FixedAssets.Commands;

public record RunMonthlyDepreciationResult(int AssetsProcessed, decimal TotalDepreciation);

public record RunMonthlyDepreciationCommand(int Year, int Month) : IRequest<RunMonthlyDepreciationResult>;

public class RunMonthlyDepreciationCommandValidator : AbstractValidator<RunMonthlyDepreciationCommand>
{
    public RunMonthlyDepreciationCommandValidator()
    {
        RuleFor(x => x.Year)
            .GreaterThan(2000).WithMessage("El año debe ser mayor a 2000.")
            .LessThanOrEqualTo(DateTime.UtcNow.Year + 1);

        RuleFor(x => x.Month)
            .InclusiveBetween(1, 12).WithMessage("El mes debe estar entre 1 y 12.");
    }
}

public class RunMonthlyDepreciationCommandHandler : IRequestHandler<RunMonthlyDepreciationCommand, RunMonthlyDepreciationResult>
{
    private readonly IFixedAssetRepository _assetRepository;
    private readonly IFixedAssetTransactionRepository _transactionRepository;
    private readonly IMediator _mediator;
    private readonly IUnitOfWork _unitOfWork;

    public RunMonthlyDepreciationCommandHandler(
        IFixedAssetRepository assetRepository,
        IFixedAssetTransactionRepository transactionRepository,
        IMediator mediator,
        IUnitOfWork unitOfWork)
    {
        _assetRepository = assetRepository;
        _transactionRepository = transactionRepository;
        _mediator = mediator;
        _unitOfWork = unitOfWork;
    }

    public async Task<RunMonthlyDepreciationResult> Handle(RunMonthlyDepreciationCommand request, CancellationToken cancellationToken)
    {
        var assetsPending = await _assetRepository.GetPendingDepreciationAsync(
            request.Year, request.Month, cancellationToken);

        var periodDate = new DateTime(request.Year, request.Month,
            DateTime.DaysInMonth(request.Year, request.Month), 0, 0, 0, DateTimeKind.Utc);

        int assetsProcessed = 0;
        decimal totalDepreciation = 0m;

        foreach (var asset in assetsPending)
        {
            // Calcular depreciación mensual (Línea Recta)
            var monthlyDepreciation = Math.Round(
                (asset.AcquisitionCost - asset.ResidualValue) / asset.UsefulLifeMonths, 4);

            // No depreciar más allá del valor residual
            var maxDepreciation = asset.CurrentBookValue - asset.ResidualValue;
            if (maxDepreciation <= 0) continue;

            var actualDepreciation = Math.Min(monthlyDepreciation, maxDepreciation);
            if (actualDepreciation <= 0) continue;

            // Actualizar activo
            asset.AccumulatedDepreciation += actualDepreciation;
            asset.CurrentBookValue -= actualDepreciation;
            asset.LastDepreciationDate = periodDate;

            if (asset.CurrentBookValue <= asset.ResidualValue)
                asset.Status = FixedAssetStatus.FullyDepreciated;

            _assetRepository.Update(asset);

            // Registrar transacción de depreciación
            var tx = new FixedAssetTransaction
            {
                FixedAssetId = asset.Id,
                TransactionDate = periodDate,
                TransactionType = FixedAssetTransactionType.Depreciation,
                Amount = actualDepreciation,
                Notes = $"Depreciación mensual {request.Month:D2}/{request.Year}"
            };
            await _transactionRepository.AddAsync(tx);

            // Generar JournalEntry: Dr Gasto Depreciación / Cr Dep. Acumulada
            var jeDetails = new List<JournalEntryDetailInput>
            {
                new JournalEntryDetailInput(asset.Category.DepreciationExpenseAccountCode, actualDepreciation, 0,
                    $"Depreciación {request.Month:D2}/{request.Year} — {asset.AssetNumber} {asset.Name}"),
                new JournalEntryDetailInput(asset.Category.AccumulatedDepreciationAccountCode, 0, actualDepreciation,
                    $"Depreciación {request.Month:D2}/{request.Year} — {asset.AssetNumber} {asset.Name}")
            };

            var createJeCmd = new CreateJournalEntryCommand(
                EntryDate: periodDate,
                Description: $"Depreciación Mensual {request.Month:D2}/{request.Year} — {asset.AssetNumber}",
                ReferenceDocument: asset.AssetNumber,
                ReferenceId: asset.Id,
                SourceModule: "FixedAssets",
                Details: jeDetails,
                PostImmediately: true
            );

            await _mediator.Send(createJeCmd, cancellationToken);

            assetsProcessed++;
            totalDepreciation += actualDepreciation;
        }

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return new RunMonthlyDepreciationResult(assetsProcessed, totalDepreciation);
    }
}
