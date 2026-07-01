using System;
using System.Threading;
using System.Threading.Tasks;
using EnterpriseBillingSystem.Domain.Entities;

namespace EnterpriseBillingSystem.Domain.Repositories;

public interface IBankReconciliationRepository : IRepository<BankReconciliation>
{
    Task<BankReconciliation?> GetLatestReconciliationAsync(Guid bankAccountId, CancellationToken cancellationToken = default);
}
