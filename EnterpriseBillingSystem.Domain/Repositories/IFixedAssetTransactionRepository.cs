using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using EnterpriseBillingSystem.Domain.Entities;
using EnterpriseBillingSystem.Domain.Enums;

namespace EnterpriseBillingSystem.Domain.Repositories;

public interface IFixedAssetTransactionRepository : IRepository<FixedAssetTransaction>
{
    Task<IEnumerable<FixedAssetTransaction>> GetByAssetIdAsync(
        Guid fixedAssetId,
        CancellationToken cancellationToken = default);

    Task<IEnumerable<FixedAssetTransaction>> GetByAssetIdAndTypeAsync(
        Guid fixedAssetId,
        FixedAssetTransactionType transactionType,
        CancellationToken cancellationToken = default);

    Task<IEnumerable<FixedAssetTransaction>> GetByPeriodAsync(
        int year,
        int month,
        FixedAssetTransactionType? transactionType = null,
        CancellationToken cancellationToken = default);
}
