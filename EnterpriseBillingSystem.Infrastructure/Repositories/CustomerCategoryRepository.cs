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

public class CustomerCategoryRepository : Repository<CustomerCategory>, ICustomerCategoryRepository
{
    public CustomerCategoryRepository(ApplicationDbContext context) : base(context)
    {
    }

    public async Task<(IEnumerable<CustomerCategory> Items, int TotalCount)> GetPagedAsync(
        int pageNumber,
        int pageSize,
        string? searchTerm,
        CancellationToken cancellationToken = default)
    {
        var query = _context.CustomerCategories.AsNoTracking();

        if (!string.IsNullOrWhiteSpace(searchTerm))
        {
            query = query.Where(cc => cc.Name.Contains(searchTerm) || 
                                      (cc.Description != null && cc.Description.Contains(searchTerm)));
        }

        int totalCount = await query.CountAsync(cancellationToken);

        var items = await query
            .OrderBy(cc => cc.Name)
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        return (items, totalCount);
    }

    public async Task<bool> ExistsByNameAsync(string name, Guid? excludeId = null, CancellationToken cancellationToken = default)
    {
        var query = _context.CustomerCategories.AsNoTracking();
        
        if (excludeId.HasValue)
        {
            query = query.Where(cc => cc.Id != excludeId.Value);
        }

        return await query.AnyAsync(cc => cc.Name == name, cancellationToken);
    }
}
