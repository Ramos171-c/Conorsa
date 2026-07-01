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

public record ImpairFixedAssetCommand(
    Guid FixedAssetId,
    decimal ImpairmentAmount,
    DateTime ImpairmentDate,
    string? Notes
) : IRequest<Guid>;

public class ImpairFixedAssetCommandValidator : AbstractValidator<ImpairFixedAssetCommand>
{
    public ImpairFixedAssetCommandValidator()
    {
        RuleFor(x => x.FixedAssetId).NotEmpty();
        RuleFor(x => x.ImpairmentAmount).GreaterThan(0)
            .WithMessage("El monto de deterioro debe ser mayor a 0.");
    }
}

public class ImpairFixedAssetCommandHandler : IRequestHandler<ImpairFixedAssetCommand, Guid>
{
    private readonly IFixedAssetRepository _assetRepository;
    private readonly IFixedAssetTransactionRepository _transactionRepository;
    private readonly IMediator _mediator;
    private readonly IUnitOfWork _unitOfWork;

    public ImpairFixedAssetCommandHandler(
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

    public async Task<Guid> Handle(ImpairFixedAssetCommand request, CancellationToken cancellationToken)
    {
        var asset = await _assetRepository.GetByIdWithDetailsAsync(request.FixedAssetId, cancellationToken);
        if (asset == null)
            throw new ArgumentException("El activo especificado no existe.");

        if (asset.Status == FixedAssetStatus.Disposed)
            throw new InvalidOperationException("No se puede registrar deterioro de un activo dado de baja.");

        if (request.ImpairmentAmount > asset.CurrentBookValue)
            throw new InvalidOperationException(
                $"El monto de deterioro ({request.ImpairmentAmount}) no puede exceder el valor libro actual ({asset.CurrentBookValue}).");

        // Reducir valor libro
        asset.CurrentBookValue -= request.ImpairmentAmount;
        asset.Status = FixedAssetStatus.Impaired;

        _assetRepository.Update(asset);

        // Registrar transacción
        var tx = new FixedAssetTransaction
        {
            FixedAssetId = asset.Id,
            TransactionDate = request.ImpairmentDate,
            TransactionType = FixedAssetTransactionType.Impairment,
            Amount = request.ImpairmentAmount,
            Notes = request.Notes ?? $"Deterioro de activo {asset.AssetNumber}"
        };
        await _transactionRepository.AddAsync(tx);

        // Generar JournalEntry: Dr 6200 Pérdida Deterioro / Cr Cuenta Activo
        var jeDetails = new List<JournalEntryDetailInput>
        {
            new JournalEntryDetailInput("6200", request.ImpairmentAmount, 0,
                $"Deterioro {asset.AssetNumber} — {asset.Name}"),
            new JournalEntryDetailInput(asset.Category.AssetAccountCode, 0, request.ImpairmentAmount,
                $"Deterioro {asset.AssetNumber} — {asset.Name}")
        };

        var createJeCmd = new CreateJournalEntryCommand(
            EntryDate: request.ImpairmentDate,
            Description: $"Deterioro (Impairment) de Activo Fijo {asset.AssetNumber}",
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
