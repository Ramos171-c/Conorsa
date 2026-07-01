using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using EnterpriseBillingSystem.Domain.Entities;

namespace EnterpriseBillingSystem.Domain.Repositories;

public interface IWarehouseRepository : IRepository<Warehouse>
{
    Task<(IEnumerable<Warehouse> Items, int TotalCount)> GetPagedAsync(
        int pageNumber,
        int pageSize,
        string? searchTerm,
        CancellationToken cancellationToken = default);

    Task<IEnumerable<BranchWarehouse>> GetBranchWarehousesAsync(
        Guid branchId,
        CancellationToken cancellationToken = default);
}
