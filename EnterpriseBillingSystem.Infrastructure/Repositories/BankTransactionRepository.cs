using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using EnterpriseBillingSystem.Domain.Entities;
using EnterpriseBillingSystem.Domain.Repositories;
using EnterpriseBillingSystem.Infrastructure.Data;

namespace EnterpriseBillingSystem.Infrastructure.Repositories;

public class BankTransactionRepository : Repository<BankTransaction>, IBankTransactionRepository
{
    public BankTransactionRepository(ApplicationDbContext context) : base(context)
    {
    }

    public async Task<IEnumerable<BankTransaction>> GetTransactionsByAccountAndPeriodAsync(
        Guid bankAccountId,
        DateTime startDate,
        DateTime endDate,
        CancellationToken cancellationToken = default)
    {
        return await _context.BankTransactions
            .Where(bt => bt.BankAccountId == bankAccountId &&
                         bt.TransactionDate >= startDate &&
                         bt.TransactionDate <= endDate)
            .OrderBy(bt => bt.TransactionDate)
            .ToListAsync(cancellationToken);
    }
}
