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

public class SupplierRepository : Repository<Supplier>, ISupplierRepository
{
    public SupplierRepository(ApplicationDbContext context) : base(context)
    {
    }

    public async Task<Supplier?> GetByCodeAsync(string supplierCode, CancellationToken cancellationToken = default)
    {
        return await _context.Suppliers
            .Include(s => s.SupplierCategory)
            .FirstOrDefaultAsync(s => s.SupplierCode == supplierCode, cancellationToken);
    }

    public async Task<Supplier?> GetByIdentificationAsync(string identificationNumber, CancellationToken cancellationToken = default)
    {
        return await _context.Suppliers
            .Include(s => s.SupplierCategory)
            .FirstOrDefaultAsync(s => s.IdentificationNumber == identificationNumber, cancellationToken);
    }

    public async Task<bool> ExistsCodeAsync(string supplierCode, Guid? excludeId = null, CancellationToken cancellationToken = default)
    {
        var query = _context.Suppliers.AsNoTracking();
        if (excludeId.HasValue)
            query = query.Where(s => s.Id != excludeId.Value);
        return await query.AnyAsync(s => s.SupplierCode == supplierCode, cancellationToken);
    }

    public async Task<string> GenerateSupplierCodeAsync(CancellationToken cancellationToken = default)
    {
        var maxCode = await _context.Suppliers
            .IgnoreQueryFilters()
            .Select(s => s.SupplierCode)
            .OrderByDescending(s => s)
            .FirstOrDefaultAsync(cancellationToken);

        int nextNumber = 1;
        if (!string.IsNullOrEmpty(maxCode) && maxCode.StartsWith("SUP-"))
        {
            if (int.TryParse(maxCode.Substring(4), out int lastNumber))
                nextNumber = lastNumber + 1;
        }

        return $"SUP-{nextNumber:D6}";
    }

    public async Task<(IEnumerable<Supplier> Items, int TotalCount)> GetPagedAsync(
        string? search,
        Guid? categoryId,
        string? status,
        int pageNumber,
        int pageSize,
        CancellationToken cancellationToken = default)
    {
        var query = _context.Suppliers
            .Include(s => s.SupplierCategory)
            .AsNoTracking();

        if (categoryId.HasValue)
            query = query.Where(s => s.SupplierCategoryId == categoryId.Value);

        if (!string.IsNullOrWhiteSpace(status))
            query = query.Where(s => s.Status.ToString() == status);

        if (!string.IsNullOrWhiteSpace(search))
            query = query.Where(s =>
                s.SupplierCode.Contains(search) ||
                s.Name.Contains(search) ||
                s.IdentificationNumber.Contains(search) ||
                (s.LegalName != null && s.LegalName.Contains(search)));

        int totalCount = await query.CountAsync(cancellationToken);

        var items = await query
            .OrderBy(s => s.SupplierCode)
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        return (items, totalCount);
    }
}
