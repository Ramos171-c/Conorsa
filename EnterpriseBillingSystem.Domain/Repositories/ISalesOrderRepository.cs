using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using EnterpriseBillingSystem.Domain.Entities;

namespace EnterpriseBillingSystem.Domain.Repositories;

public interface ISalesOrderRepository : IRepository<SalesOrder>
{
    Task<SalesOrder?> GetByIdWithDetailsAsync(Guid id, CancellationToken cancellationToken = default);
    Task<SalesOrder?> GetByOrderNumberAsync(string orderNumber, CancellationToken cancellationToken = default);

    /// <summary>Genera el próximo número de pedido en formato SO-yyyyMMdd-NNNNN.</summary>
    Task<string> GenerateOrderNumberAsync(CancellationToken cancellationToken = default);

    Task<(IEnumerable<SalesOrder> Items, int TotalCount)> GetPagedAsync(
        Guid? customerId,
        string? status,
        DateTime? fromDate,
        DateTime? toDate,
        int pageNumber,
        int pageSize,
        string? createdBy = null,
        Guid? routeId = null,
        CancellationToken cancellationToken = default);

    Task<IEnumerable<SalesOrder>> GetFilteredWithDetailsAsync(
        Guid? customerId,
        string? status,
        DateTime? fromDate,
        DateTime? toDate,
        Guid? routeId = null,
        CancellationToken cancellationToken = default);
}
