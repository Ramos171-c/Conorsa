using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using EnterpriseBillingSystem.Domain.Entities;
using EnterpriseBillingSystem.Domain.Repositories;
using EnterpriseBillingSystem.Infrastructure.Data;

namespace EnterpriseBillingSystem.Infrastructure.Repositories;

public class BankRepository : Repository<Bank>, IBankRepository
{
    public BankRepository(ApplicationDbContext context) : base(context)
    {
    }

    public async Task<Bank?> GetByCodeAsync(string code, CancellationToken cancellationToken = default)
    {
        return await _context.Banks
            .FirstOrDefaultAsync(b => b.Code == code, cancellationToken);
    }

    public async Task<bool> ExistsCodeAsync(string code, CancellationToken cancellationToken = default)
    {
        return await _context.Banks
            .AnyAsync(b => b.Code == code, cancellationToken);
    }
}
