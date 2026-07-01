using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using EnterpriseBillingSystem.Domain.Entities;
using EnterpriseBillingSystem.Domain.Repositories;
using EnterpriseBillingSystem.Infrastructure.Data;

namespace EnterpriseBillingSystem.Infrastructure.Repositories;

public class BankReconciliationRepository : Repository<BankReconciliation>, IBankReconciliationRepository
{
    public BankReconciliationRepository(ApplicationDbContext context) : base(context)
    {
    }

    public async Task<BankReconciliation?> GetLatestReconciliationAsync(Guid bankAccountId, CancellationToken cancellationToken = default)
    {
        return await _context.BankReconciliations
            .Where(br => br.BankAccountId == bankAccountId)
            .OrderByDescending(br => br.StatementDate)
            .FirstOrDefaultAsync(cancellationToken);
    }
}
