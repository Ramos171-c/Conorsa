using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using EnterpriseBillingSystem.Application.FixedAssets.DTOs;
using EnterpriseBillingSystem.Domain.Repositories;

namespace EnterpriseBillingSystem.Application.FixedAssets.Queries;

public record GetFixedAssetMovementReportQuery(
    Guid? FixedAssetId = null,
    DateTime? StartDate = null,
    DateTime? EndDate = null
) : IRequest<IEnumerable<FixedAssetTransactionDto>>;

public class GetFixedAssetMovementReportQueryHandler
    : IRequestHandler<GetFixedAssetMovementReportQuery, IEnumerable<FixedAssetTransactionDto>>
{
    private readonly IFixedAssetTransactionRepository _transactionRepository;
    private readonly IFixedAssetRepository _assetRepository;

    public GetFixedAssetMovementReportQueryHandler(
        IFixedAssetTransactionRepository transactionRepository,
        IFixedAssetRepository assetRepository)
    {
        _transactionRepository = transactionRepository;
        _assetRepository = assetRepository;
    }

    public async Task<IEnumerable<FixedAssetTransactionDto>> Handle(
        GetFixedAssetMovementReportQuery request,
        CancellationToken cancellationToken)
    {
        IEnumerable<Domain.Entities.FixedAssetTransaction> transactions;

        if (request.FixedAssetId.HasValue)
        {
            transactions = await _transactionRepository.GetByAssetIdAsync(
                request.FixedAssetId.Value, cancellationToken);
        }
        else
        {
            // Traer por períodos definidos o todo
            var start = request.StartDate ?? DateTime.UtcNow.AddYears(-1);
            var end = request.EndDate ?? DateTime.UtcNow;
            transactions = await _transactionRepository.GetByPeriodAsync(
                start.Year, start.Month, null, cancellationToken);
        }

        var result = new List<FixedAssetTransactionDto>();
        foreach (var tx in transactions)
        {
            var asset = tx.FixedAsset ??
                await _assetRepository.GetByIdAsync(tx.FixedAssetId);

            result.Add(new FixedAssetTransactionDto(
                tx.Id,
                tx.FixedAssetId,
                asset?.AssetNumber ?? string.Empty,
                asset?.Name ?? string.Empty,
                tx.TransactionDate,
                tx.TransactionType,
                tx.TransactionType.ToString(),
                tx.Amount,
                tx.JournalEntryId,
                tx.Notes
            ));
        }

        return result.OrderByDescending(r => r.TransactionDate);
    }
}
