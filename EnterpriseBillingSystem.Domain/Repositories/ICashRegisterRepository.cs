using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using EnterpriseBillingSystem.Domain.Entities;

namespace EnterpriseBillingSystem.Domain.Repositories;

public interface ICashRegisterRepository : IRepository<CashRegister>
{
    Task<CashRegister?> GetDefaultRegisterByBranchAsync(Guid branchId, CancellationToken cancellationToken = default);
    Task<(IEnumerable<CashRegister> Items, int TotalCount)> GetPagedAsync(
        Guid? branchId,
        string? searchTerm,
        int pageNumber,
        int pageSize,
        CancellationToken cancellationToken = default);
}
