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

public class CashRegisterRepository : Repository<CashRegister>, ICashRegisterRepository
{
    public CashRegisterRepository(ApplicationDbContext context) : base(context)
    {
    }

    public async Task<CashRegister?> GetDefaultRegisterByBranchAsync(Guid branchId, CancellationToken cancellationToken = default)
    {
        return await _context.CashRegisters
            .FirstOrDefaultAsync(r => r.BranchId == branchId && r.IsDefault && r.IsActive, cancellationToken);
    }

    public async Task<(IEnumerable<CashRegister> Items, int TotalCount)> GetPagedAsync(
        Guid? branchId,
        string? searchTerm,
        int pageNumber,
        int pageSize,
        CancellationToken cancellationToken = default)
    {
        var query = _context.CashRegisters.AsQueryable();

        if (branchId.HasValue)
        {
            query = query.Where(r => r.BranchId == branchId.Value);
        }

        if (!string.IsNullOrWhiteSpace(searchTerm))
        {
            query = query.Where(r => r.Name.Contains(searchTerm) || r.Code.Contains(searchTerm));
        }

        int totalCount = await query.CountAsync(cancellationToken);

        var items = await query
            .OrderBy(r => r.Code)
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        return (items, totalCount);
    }
}
