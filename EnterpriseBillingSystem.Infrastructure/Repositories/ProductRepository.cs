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

public class ProductRepository : Repository<Product>, IProductRepository
{
    public ProductRepository(ApplicationDbContext context) : base(context)
    {
    }

    public async Task<Product?> GetByIdWithDetailsAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _context.Products
            .Include(p => p.Category)
            .Include(p => p.Brand)
            .Include(p => p.DefaultUnitOfMeasure)
            .Include(p => p.Tax)
            .Include(p => p.Presentations)
                .ThenInclude(pr => pr.UnitOfMeasure)
            .Include(p => p.BranchProducts)
                .ThenInclude(bp => bp.Branch)
            .FirstOrDefaultAsync(p => p.Id == id, cancellationToken);
    }

    public async Task<(IEnumerable<Product> Items, int TotalCount)> GetPagedAsync(
        int pageNumber,
        int pageSize,
        string? searchTerm,
        Guid? categoryId,
        Guid? brandId,
        bool? isForPos = null,
        CancellationToken cancellationToken = default)
    {
        var query = _context.Products
            .Include(p => p.Category)
            .Include(p => p.Brand)
            .Include(p => p.DefaultUnitOfMeasure)
            .Include(p => p.Tax)
            .Include(p => p.Presentations)
                .ThenInclude(pr => pr.UnitOfMeasure)
            .Include(p => p.BranchProducts)
                .ThenInclude(bp => bp.Branch)
            .AsNoTracking();

        if (isForPos == true)
        {
            query = query.Where(p => !p.IsSoldOut && p.IsCatalogVisible && p.IsActive);
        }

        if (categoryId.HasValue)
        {
            query = query.Where(p => p.CategoryId == categoryId.Value);
        }

        if (brandId.HasValue)
        {
            query = query.Where(p => p.BrandId == brandId.Value);
        }

        if (!string.IsNullOrWhiteSpace(searchTerm))
        {
            query = query.Where(p => p.Name.Contains(searchTerm) ||
                                     p.InternalCode.Contains(searchTerm) ||
                                     p.Presentations.Any(pr => pr.Barcode != null && pr.Barcode.Contains(searchTerm)) ||
                                     (p.Description != null && p.Description.Contains(searchTerm)));
        }

        int totalCount = await query.CountAsync(cancellationToken);

        var items = await query
            .OrderBy(p => p.InternalCode)
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        return (items, totalCount);
    }

    public async Task<bool> ExistsSkuAsync(string sku, Guid? excludeId = null, CancellationToken cancellationToken = default)
    {
        return await _context.Products.AnyAsync(p => p.InternalCode == sku && p.Id != excludeId, cancellationToken);
    }

    public async Task<bool> ExistsBarcodeAsync(string barcode, Guid? excludeId = null, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(barcode)) return false;
        return await _context.ProductPresentations.AnyAsync(pr => pr.Barcode == barcode && pr.ProductId != excludeId, cancellationToken);
    }

    public async Task<IEnumerable<Product>> GetCatalogProductsAsync(CancellationToken cancellationToken = default)
    {
        return await _context.Products
            .AsNoTracking()
            .Include(p => p.Category)
            .Include(p => p.Brand)
            .Include(p => p.DefaultUnitOfMeasure)
            .Include(p => p.Tax)
            .Include(p => p.Presentations)
                .ThenInclude(pr => pr.UnitOfMeasure)
            .Include(p => p.BranchProducts)
                .ThenInclude(bp => bp.Branch)
            .Where(p => p.IsCatalogVisible && p.IsActive)
            .OrderBy(p => p.InternalCode)
            .ToListAsync(cancellationToken);
    }

    public async Task<IEnumerable<Product>> GetTopProductsForCustomerAsync(Guid customerId, int limit, CancellationToken cancellationToken = default)
    {
        var topFromInvoices = await _context.SalesInvoiceDetails
            .Where(d => d.SalesInvoice.CustomerId == customerId && !d.SalesInvoice.IsDeleted)
            .GroupBy(d => d.ProductId)
            .Select(g => new { ProductId = g.Key, Qty = g.Sum(d => d.Quantity) })
            .ToListAsync(cancellationToken);

        var topFromOrders = await _context.SalesOrderDetails
            .Where(d => d.SalesOrder.CustomerId == customerId && !d.SalesOrder.IsDeleted)
            .GroupBy(d => d.ProductId)
            .Select(g => new { ProductId = g.Key, Qty = g.Sum(d => d.Quantity) })
            .ToListAsync(cancellationToken);

        var topProductIds = topFromInvoices.Concat(topFromOrders)
            .GroupBy(x => x.ProductId)
            .Select(g => new { ProductId = g.Key, TotalQty = g.Sum(x => x.Qty) })
            .OrderByDescending(x => x.TotalQty)
            .Take(limit)
            .Select(x => x.ProductId)
            .ToList();

        if (!topProductIds.Any())
        {
            return Enumerable.Empty<Product>();
        }

        var products = await _context.Products
            .AsNoTracking()
            .Include(p => p.Category)
            .Include(p => p.Brand)
            .Include(p => p.DefaultUnitOfMeasure)
            .Include(p => p.Tax)
            .Include(p => p.Presentations)
                .ThenInclude(pr => pr.UnitOfMeasure)
            .Include(p => p.BranchProducts)
                .ThenInclude(bp => bp.Branch)
            .Where(p => topProductIds.Contains(p.Id))
            .ToListAsync(cancellationToken);

        return products.OrderBy(p => topProductIds.IndexOf(p.Id));
    }
}
