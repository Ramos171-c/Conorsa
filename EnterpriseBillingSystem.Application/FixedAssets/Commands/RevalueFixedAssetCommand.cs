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

public record RevalueFixedAssetCommand(
    Guid FixedAssetId,
    decimal RevaluationAmount,
    DateTime RevaluationDate,
    string? Notes
) : IRequest<Guid>;

public class RevalueFixedAssetCommandValidator : AbstractValidator<RevalueFixedAssetCommand>
{
    public RevalueFixedAssetCommandValidator()
    {
        RuleFor(x => x.FixedAssetId).NotEmpty();
        RuleFor(x => x.RevaluationAmount).GreaterThan(0)
            .WithMessage("El monto de revalorización debe ser mayor a 0.");
    }
}

public class RevalueFixedAssetCommandHandler : IRequestHandler<RevalueFixedAssetCommand, Guid>
{
    private readonly IFixedAssetRepository _assetRepository;
    private readonly IFixedAssetTransactionRepository _transactionRepository;
    private readonly IMediator _mediator;
    private readonly IUnitOfWork _unitOfWork;

    public RevalueFixedAssetCommandHandler(
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

    public async Task<Guid> Handle(RevalueFixedAssetCommand request, CancellationToken cancellationToken)
    {
        var asset = await _assetRepository.GetByIdWithDetailsAsync(request.FixedAssetId, cancellationToken);
        if (asset == null)
            throw new ArgumentException("El activo especificado no existe.");

        if (asset.Status == FixedAssetStatus.Disposed)
            throw new InvalidOperationException("No se puede revalorizar un activo dado de baja.");

        // Incrementar valor libro
        asset.CurrentBookValue += request.RevaluationAmount;
        if (asset.Status == FixedAssetStatus.FullyDepreciated)
            asset.Status = FixedAssetStatus.Active;

        _assetRepository.Update(asset);

        // Registrar transacción
        var tx = new FixedAssetTransaction
        {
            FixedAssetId = asset.Id,
            TransactionDate = request.RevaluationDate,
            TransactionType = FixedAssetTransactionType.Revaluation,
            Amount = request.RevaluationAmount,
            Notes = request.Notes ?? $"Revalorización de activo {asset.AssetNumber}"
        };
        await _transactionRepository.AddAsync(tx);

        // Generar JournalEntry: Dr Cuenta Activo / Cr 3200 Superávit Revalorización
        var jeDetails = new List<JournalEntryDetailInput>
        {
            new JournalEntryDetailInput(asset.Category.AssetAccountCode, request.RevaluationAmount, 0,
                $"Revalorización {asset.AssetNumber} — {asset.Name}"),
            new JournalEntryDetailInput("3200", 0, request.RevaluationAmount,
                $"Revalorización {asset.AssetNumber} — {asset.Name}")
        };

        var createJeCmd = new CreateJournalEntryCommand(
            EntryDate: request.RevaluationDate,
            Description: $"Revalorización de Activo Fijo {asset.AssetNumber}",
            ReferenceDocument: asset.AssetNumber,
            ReferenceId: asset.Id,
            SourceModule: "FixedAssets",
            Details: jeDetails,
            PostImmediately: true
        );

        await _mediator.Send(createJeCmd, cancellationToken);

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return tx.Id;
    }
}
