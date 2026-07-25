using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using EnterpriseBillingSystem.Domain.Entities;

namespace EnterpriseBillingSystem.Domain.Repositories;

public interface IRouteLiquidationRepository : IRepository<RouteLiquidation>
{
    Task<RouteLiquidation?> GetByIdWithDetailsAsync(Guid id, CancellationToken cancellationToken = default);
    Task<(IEnumerable<RouteLiquidation> Items, int TotalCount)> GetPagedAsync(
        DateTime? fromDate,
        DateTime? toDate,
        Guid? routeId,
        string? status,
        int pageNumber,
        int pageSize,
        CancellationToken cancellationToken = default);
    Task<string> GenerateNextLiquidationNumberAsync(CancellationToken cancellationToken = default);
}
