using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using EnterpriseBillingSystem.Domain.Entities;

namespace EnterpriseBillingSystem.Domain.Repositories;

public interface IInventoryMovementRepository : IRepository<InventoryMovement>
{
    Task<InventoryMovement?> GetByIdWithDetailsAsync(Guid id, CancellationToken cancellationToken = default);
    Task<(IEnumerable<InventoryMovementDetail> Items, int TotalCount)> GetKardexAsync(
        Guid branchWarehouseId,
        Guid productId,
        DateTime? startDate,
        DateTime? endDate,
        int pageNumber,
        int pageSize,
        CancellationToken cancellationToken = default);
    Task<string> GenerateMovementNumberAsync(CancellationToken cancellationToken = default);
}
