using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using EnterpriseBillingSystem.Domain.Entities;
using EnterpriseBillingSystem.Domain.Repositories;
using EnterpriseBillingSystem.Infrastructure.Data;

namespace EnterpriseBillingSystem.Infrastructure.Repositories;

public class AccountRepository : Repository<Account>, IAccountRepository
{
    public AccountRepository(ApplicationDbContext context) : base(context)
    {
    }

    public async Task<Account?> GetByCodeAsync(string code, CancellationToken cancellationToken = default)
    {
        return await _context.Accounts
            .Include(a => a.SubAccounts)
            .FirstOrDefaultAsync(a => a.Code == code, cancellationToken);
    }

    public async Task<bool> HasChildrenAsync(Guid accountId, CancellationToken cancellationToken = default)
    {
        return await _context.Accounts
            .AnyAsync(a => a.ParentAccountId == accountId, cancellationToken);
    }
}
