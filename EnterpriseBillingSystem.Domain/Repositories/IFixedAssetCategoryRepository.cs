using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using EnterpriseBillingSystem.Domain.Entities;

namespace EnterpriseBillingSystem.Domain.Repositories;

public interface IFixedAssetCategoryRepository : IRepository<FixedAssetCategory>
{
    Task<FixedAssetCategory?> GetByCodeAsync(string code, CancellationToken cancellationToken = default);

    Task<bool> ExistsCodeAsync(string code, CancellationToken cancellationToken = default);

    Task<(IEnumerable<FixedAssetCategory> Items, int TotalCount)> GetPagedAsync(
        int pageNumber,
        int pageSize,
        string? searchTerm,
        CancellationToken cancellationToken = default);
}
