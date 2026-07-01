using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using EnterpriseBillingSystem.Domain.Entities;

namespace EnterpriseBillingSystem.Domain.Repositories;

public interface ICashSessionRepository : IRepository<CashSession>
{
    Task<CashSession?> GetByIdWithDetailsAsync(Guid id, CancellationToken cancellationToken = default);
    Task<CashSession?> GetOpenSessionByUserAsync(Guid userId, CancellationToken cancellationToken = default);
    Task<CashSession?> GetOpenSessionByRegisterAsync(Guid registerId, CancellationToken cancellationToken = default);
    Task<string> GenerateSessionNumberAsync(CancellationToken cancellationToken = default);
    Task<(IEnumerable<CashSession> Items, int TotalCount)> GetPagedAsync(
        Guid? registerId,
        string? status,
        int pageNumber,
        int pageSize,
        CancellationToken cancellationToken = default);
}
