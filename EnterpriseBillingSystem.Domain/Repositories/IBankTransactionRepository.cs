using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using EnterpriseBillingSystem.Domain.Entities;

namespace EnterpriseBillingSystem.Domain.Repositories;

public interface IBankTransactionRepository : IRepository<BankTransaction>
{
    Task<IEnumerable<BankTransaction>> GetTransactionsByAccountAndPeriodAsync(
        Guid bankAccountId,
        DateTime startDate,
        DateTime endDate,
        CancellationToken cancellationToken = default);
}
