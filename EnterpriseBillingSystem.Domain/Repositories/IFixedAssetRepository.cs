using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using EnterpriseBillingSystem.Domain.Entities;
using EnterpriseBillingSystem.Domain.Enums;

namespace EnterpriseBillingSystem.Domain.Repositories;

public interface IFixedAssetRepository : IRepository<FixedAsset>
{
    Task<FixedAsset?> GetByIdWithDetailsAsync(Guid id, CancellationToken cancellationToken = default);

    Task<FixedAsset?> GetByAssetNumberAsync(string assetNumber, CancellationToken cancellationToken = default);

    Task<(IEnumerable<FixedAsset> Items, int TotalCount)> GetPagedAsync(
        int pageNumber,
        int pageSize,
        Guid? categoryId,
        FixedAssetStatus? status,
        Guid? branchId,
        CancellationToken cancellationToken = default);

    /// <summary>Devuelve activos activos o deteriorados que no hayan depreciado en el período indicado.</summary>
    Task<IEnumerable<FixedAsset>> GetPendingDepreciationAsync(
        int year,
        int month,
        CancellationToken cancellationToken = default);

    Task<string> GenerateAssetNumberAsync(CancellationToken cancellationToken = default);
}
