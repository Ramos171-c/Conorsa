using System;
using System.Threading;
using System.Threading.Tasks;
using EnterpriseBillingSystem.Domain.Entities;

namespace EnterpriseBillingSystem.Domain.Repositories;

public interface IBankAccountRepository : IRepository<BankAccount>
{
    Task<BankAccount?> GetByAccountNumberAsync(string accountNumber, CancellationToken cancellationToken = default);
    Task<bool> ExistsAccountNumberInBankAsync(Guid bankId, string accountNumber, CancellationToken cancellationToken = default);
}
