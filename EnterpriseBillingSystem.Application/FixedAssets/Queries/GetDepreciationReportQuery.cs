using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using EnterpriseBillingSystem.Domain.Enums;
using EnterpriseBillingSystem.Domain.Repositories;

namespace EnterpriseBillingSystem.Application.FixedAssets.Queries;

public record DepreciationReportItem(
    string AssetNumber,
    string AssetName,
    string CategoryName,
    decimal AcquisitionCost,
    decimal MonthlyDepreciation,
    decimal AccumulatedDepreciation,
    decimal CurrentBookValue,
    DateTime PeriodDate
);

public record GetDepreciationReportQuery(int Year, int Month) : IRequest<IEnumerable<DepreciationReportItem>>;

public class GetDepreciationReportQueryHandler : IRequestHandler<GetDepreciationReportQuery, IEnumerable<DepreciationReportItem>>
{
    private readonly IFixedAssetTransactionRepository _transactionRepository;
    private readonly IFixedAssetRepository _assetRepository;

    public GetDepreciationReportQueryHandler(
        IFixedAssetTransactionRepository transactionRepository,
        IFixedAssetRepository assetRepository)
    {
        _transactionRepository = transactionRepository;
        _assetRepository = assetRepository;
    }

    public async Task<IEnumerable<DepreciationReportItem>> Handle(
        GetDepreciationReportQuery request,
        CancellationToken cancellationToken)
    {
        var transactions = await _transactionRepository.GetByPeriodAsync(
            request.Year, request.Month, FixedAssetTransactionType.Depreciation, cancellationToken);

        var result = new List<DepreciationReportItem>();
        foreach (var tx in transactions)
        {
            var asset = await _assetRepository.GetByIdWithDetailsAsync(tx.FixedAssetId, cancellationToken);
            if (asset == null) continue;

            result.Add(new DepreciationReportItem(
                asset.AssetNumber,
                asset.Name,
                asset.Category?.Name ?? string.Empty,
                asset.AcquisitionCost,
                tx.Amount,
                asset.AccumulatedDepreciation,
                asset.CurrentBookValue,
                tx.TransactionDate
            ));
        }

        return result.OrderBy(r => r.AssetNumber);
    }
}
