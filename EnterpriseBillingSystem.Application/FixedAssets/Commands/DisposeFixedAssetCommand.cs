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

public record DisposeFixedAssetCommand(
    Guid FixedAssetId,
    DateTime DisposalDate,
    decimal ProceedsFromSale,
    string? Notes
) : IRequest<Guid>;

public class DisposeFixedAssetCommandValidator : AbstractValidator<DisposeFixedAssetCommand>
{
    public DisposeFixedAssetCommandValidator()
    {
        RuleFor(x => x.FixedAssetId).NotEmpty();
        RuleFor(x => x.ProceedsFromSale).GreaterThanOrEqualTo(0)
            .WithMessage("El ingreso por venta no puede ser negativo.");
    }
}

public class DisposeFixedAssetCommandHandler : IRequestHandler<DisposeFixedAssetCommand, Guid>
{
    private readonly IFixedAssetRepository _assetRepository;
    private readonly IFixedAssetTransactionRepository _transactionRepository;
    private readonly IMediator _mediator;
    private readonly IUnitOfWork _unitOfWork;

    public DisposeFixedAssetCommandHandler(
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

    public async Task<Guid> Handle(DisposeFixedAssetCommand request, CancellationToken cancellationToken)
    {
        var asset = await _assetRepository.GetByIdWithDetailsAsync(request.FixedAssetId, cancellationToken);
        if (asset == null)
            throw new ArgumentException("El activo especificado no existe.");

        if (asset.Status == FixedAssetStatus.Disposed)
            throw new InvalidOperationException("El activo ya fue dado de baja anteriormente.");

        var bookValue = asset.CurrentBookValue;
        var proceeds = request.ProceedsFromSale;
        var lossOrGain = proceeds - bookValue; // positivo = ganancia, negativo = pérdida

        // Registrar transacción de baja
        var tx = new FixedAssetTransaction
        {
            FixedAssetId = asset.Id,
            TransactionDate = request.DisposalDate,
            TransactionType = FixedAssetTransactionType.Disposal,
            Amount = bookValue > 0 ? bookValue : proceeds > 0 ? proceeds : 1m,
            Notes = request.Notes ?? $"Baja de activo {asset.AssetNumber}"
        };
        await _transactionRepository.AddAsync(tx);

        // Generar JournalEntry de baja
        // Dr Dep. Acumulada (por el total depreciado)
        // Dr Pérdida (si hay pérdida) / Cr Ingreso (si hay ganancia)
        // Cr Cuenta Activo (por costo original)
        var jeDetails = new List<JournalEntryDetailInput>();

        // Eliminar depreciación acumulada
        if (asset.AccumulatedDepreciation > 0)
        {
            jeDetails.Add(new JournalEntryDetailInput(
                asset.Category.AccumulatedDepreciationAccountCode,
                asset.AccumulatedDepreciation, 0,
                $"Baja activo {asset.AssetNumber} — Eliminación dep. acumulada"));
        }

        // Si hay ingreso por venta, acreditarlo a Caja (1110 por defecto) o dejar como anotación
        if (proceeds > 0)
        {
            jeDetails.Add(new JournalEntryDetailInput("1110", proceeds, 0,
                $"Baja activo {asset.AssetNumber} — Ingreso por venta"));
        }

        // Si hay pérdida (valor libro > ingreso)
        if (lossOrGain < 0)
        {
            jeDetails.Add(new JournalEntryDetailInput("6210", Math.Abs(lossOrGain), 0,
                $"Baja activo {asset.AssetNumber} — Pérdida en baja"));
        }

        // Reversar cuenta de activo por costo original
        jeDetails.Add(new JournalEntryDetailInput(
            asset.Category.AssetAccountCode,
            0, asset.AcquisitionCost,
            $"Baja activo {asset.AssetNumber} — Eliminación costo original"));

        // Si hay ganancia (ingreso > valor libro), acreditarla a ingresos
        if (lossOrGain > 0)
        {
            jeDetails.Add(new JournalEntryDetailInput("4200", 0, lossOrGain,
                $"Baja activo {asset.AssetNumber} — Ganancia en venta"));
        }

        var createJeCmd = new CreateJournalEntryCommand(
            EntryDate: request.DisposalDate,
            Description: $"Baja de Activo Fijo {asset.AssetNumber} — {asset.Name}",
            ReferenceDocument: asset.AssetNumber,
            ReferenceId: asset.Id,
            SourceModule: "FixedAssets",
            Details: jeDetails,
            PostImmediately: true
        );

        await _mediator.Send(createJeCmd, cancellationToken);

        // Actualizar estado del activo
        asset.Status = FixedAssetStatus.Disposed;
        asset.CurrentBookValue = 0;
        _assetRepository.Update(asset);

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return tx.Id;
    }
}
