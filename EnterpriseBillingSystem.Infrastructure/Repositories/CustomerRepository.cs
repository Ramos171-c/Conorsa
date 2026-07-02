using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using EnterpriseBillingSystem.Domain.Entities;
using EnterpriseBillingSystem.Domain.Enums;
using EnterpriseBillingSystem.Domain.Repositories;
using EnterpriseBillingSystem.Infrastructure.Data;

namespace EnterpriseBillingSystem.Infrastructure.Repositories;

public class CustomerRepository : Repository<Customer>, ICustomerRepository
{
    public CustomerRepository(ApplicationDbContext context) : base(context)
    {
    }

    public async Task<Customer?> GetByIdWithDetailsAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _context.Customers
            .Include(c => c.CustomerCategory)
            .Include(c => c.CustomerPricingProfile)
            .Include(c => c.Route)
            .Include(c => c.Addresses)
            .Include(c => c.Phones)
            .Include(c => c.Emails)
            .Include(c => c.Contacts)
            .FirstOrDefaultAsync(c => c.Id == id, cancellationToken);
    }

    public async Task<(IEnumerable<Customer> Items, int TotalCount)> GetPagedAsync(
        int pageNumber,
        int pageSize,
        string? searchTerm,
        Guid? categoryId,
        CustomerStatus? status,
        Guid? routeId = null,
        CancellationToken cancellationToken = default)
    {
        var query = _context.Customers
            .Include(c => c.CustomerCategory)
            .Include(c => c.CustomerPricingProfile)
            .Include(c => c.Route)
            .AsNoTracking();

        if (categoryId.HasValue)
        {
            query = query.Where(c => c.CustomerCategoryId == categoryId.Value);
        }

        if (status.HasValue)
        {
            query = query.Where(c => c.Status == status.Value);
        }

        if (routeId.HasValue)
        {
            query = query.Where(c => c.RouteId == routeId.Value || c.RouteId == null);
        }

        if (!string.IsNullOrWhiteSpace(searchTerm))
        {
            query = query.Where(c => c.CustomerCode.Contains(searchTerm) ||
                                     c.IdentificationNumber.Contains(searchTerm) ||
                                     c.Name.Contains(searchTerm) ||
                                     (c.LegalName != null && c.LegalName.Contains(searchTerm)));
        }

        int totalCount = await query.CountAsync(cancellationToken);

        var items = await query
            .OrderBy(c => c.CustomerCode)
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        return (items, totalCount);
    }

    public async Task<bool> ExistsByIdentificationAsync(string identificationNumber, Guid? excludeId = null, CancellationToken cancellationToken = default)
    {
        var query = _context.Customers.AsNoTracking();

        if (excludeId.HasValue)
        {
            query = query.Where(c => c.Id != excludeId.Value);
        }

        return await query.AnyAsync(c => c.IdentificationNumber == identificationNumber, cancellationToken);
    }

    public async Task<string> GenerateCustomerCodeAsync(CancellationToken cancellationToken = default)
    {
        var maxCode = await _context.Customers
            .IgnoreQueryFilters()
            .Select(c => c.CustomerCode)
            .OrderByDescending(c => c)
            .FirstOrDefaultAsync(cancellationToken);

        int nextNumber = 1;
        if (!string.IsNullOrEmpty(maxCode) && maxCode.StartsWith("CUS-"))
        {
            if (int.TryParse(maxCode.Substring(4), out int lastNumber))
            {
                nextNumber = lastNumber + 1;
            }
        }

        return $"CUS-{nextNumber:D6}";
    }
}
