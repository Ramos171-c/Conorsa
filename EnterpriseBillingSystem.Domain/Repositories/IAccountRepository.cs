using System;
using System.Threading;
using System.Threading.Tasks;
using EnterpriseBillingSystem.Domain.Entities;

namespace EnterpriseBillingSystem.Domain.Repositories;

public interface IAccountRepository : IRepository<Account>
{
    Task<Account?> GetByCodeAsync(string code, CancellationToken cancellationToken = default);
    Task<bool> HasChildrenAsync(Guid accountId, CancellationToken cancellationToken = default);
}
