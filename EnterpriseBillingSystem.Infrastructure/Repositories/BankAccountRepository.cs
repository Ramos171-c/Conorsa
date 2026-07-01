using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using EnterpriseBillingSystem.Domain.Entities;
using EnterpriseBillingSystem.Domain.Repositories;
using EnterpriseBillingSystem.Infrastructure.Data;

namespace EnterpriseBillingSystem.Infrastructure.Repositories;

public class BankAccountRepository : Repository<BankAccount>, IBankAccountRepository
{
    public BankAccountRepository(ApplicationDbContext context) : base(context)
    {
    }

    public async Task<BankAccount?> GetByAccountNumberAsync(string accountNumber, CancellationToken cancellationToken = default)
    {
        return await _context.BankAccounts
            .Include(ba => ba.Bank)
            .FirstOrDefaultAsync(ba => ba.AccountNumber == accountNumber, cancellationToken);
    }

    public async Task<bool> ExistsAccountNumberInBankAsync(Guid bankId, string accountNumber, CancellationToken cancellationToken = default)
    {
        return await _context.BankAccounts
            .AnyAsync(ba => ba.BankId == bankId && ba.AccountNumber == accountNumber, cancellationToken);
    }
}
