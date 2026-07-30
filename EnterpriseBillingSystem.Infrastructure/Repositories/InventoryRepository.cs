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

public class InventoryRepository : Repository<Inventory>, IInventoryRepository
{
    public InventoryRepository(ApplicationDbContext context) : base(context)
    {
    }

    public async Task<Inventory?> GetByWarehouseAndProductAsync(Guid branchWarehouseId, Guid productId, CancellationToken cancellationToken = default)
    {
        return await _context.Inventories
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(i => i.BranchWarehouseId == branchWarehouseId && i.ProductId == productId, cancellationToken);
    }

    public async Task<(IEnumerable<Inventory> Items, int TotalCount)> GetStockInquiryAsync(
        Guid? branchWarehouseId,
        Guid? productId,
        Guid? categoryId,
        string? searchTerm,
        int pageNumber,
        int pageSize,
        CancellationToken cancellationToken = default)
    {
        var query = _context.Inventories
            .Include(i => i.BranchWarehouse)
                .ThenInclude(bw => bw.Warehouse)
            .Include(i => i.BranchWarehouse)
                .ThenInclude(bw => bw.Branch)
            .Include(i => i.Product)
                .ThenInclude(p => p.DefaultUnitOfMeasure)
            .AsNoTracking();

        if (branchWarehouseId.HasValue)
        {
            query = query.Where(i => i.BranchWarehouseId == branchWarehouseId.Value);
        }

        if (productId.HasValue)
        {
            query = query.Where(i => i.ProductId == productId.Value);
        }

        if (categoryId.HasValue)
        {
            query = query.Where(i => i.Product.CategoryId == categoryId.Value);
        }

        if (!string.IsNullOrWhiteSpace(searchTerm))
        {
            query = query.Where(i => i.Product.Name.Contains(searchTerm) ||
                                     i.Product.InternalCode.Contains(searchTerm) ||
                                     (i.Product.Description != null && i.Product.Description.Contains(searchTerm)));
        }

        int totalCount = await query.CountAsync(cancellationToken);

        var items = await query
            .OrderBy(i => i.Product.Name)
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        return (items, totalCount);
    }

    public async Task<IEnumerable<(Product Product, decimal CurrentStock)>> GetLowStockItemsAsync(CancellationToken cancellationToken = default)
    {
        var lowStock = await _context.Products
            .AsNoTracking()
            .Where(p => p.IsActive && p.TrackInventory && p.MinimumStock > 0)
            .Select(p => new
            {
                Product = p,
                CurrentStock = _context.Inventories.Where(i => i.ProductId == p.Id).Sum(i => (decimal?)i.PhysicalStock) ?? 0m
            })
            .Where(x => x.CurrentStock <= x.Product.MinimumStock)
            .ToListAsync(cancellationToken);

        return lowStock.Select(x => (x.Product, x.CurrentStock));
    }

    public async Task<Dictionary<Guid, decimal>> GetAvailableStockByProductIdsAsync(IEnumerable<Guid> productIds, CancellationToken cancellationToken = default)
    {
        var idsList = productIds.Distinct().ToList();
        if (!idsList.Any()) return new Dictionary<Guid, decimal>();

        var stockList = await _context.Inventories.AsNoTracking()
            .Where(i => idsList.Contains(i.ProductId))
            .GroupBy(i => i.ProductId)
            .Select(g => new
            {
                ProductId = g.Key,
                Available = g.Sum(i => (decimal?)i.PhysicalStock - i.ReservedStock - i.CommittedStock) ?? 0m
            })
            .ToListAsync(cancellationToken);

        return stockList.ToDictionary(x => x.ProductId, x => Math.Max(0, x.Available));
    }

    public async Task<Dictionary<string, decimal>> GetAvailableStockByProductNamesAsync(IEnumerable<string> productNames, CancellationToken cancellationToken = default)
    {
        var normalizedNames = productNames
            .Where(n => !string.IsNullOrWhiteSpace(n))
            .Select(n => n.Trim().ToUpper())
            .Distinct()
            .ToList();

        if (!normalizedNames.Any()) return new Dictionary<string, decimal>();

        var stockList = await _context.Inventories.AsNoTracking()
            .Include(i => i.Product)
            .Where(i => i.Product != null && normalizedNames.Contains(i.Product.Name.Trim().ToUpper()))
            .GroupBy(i => i.Product.Name.Trim().ToUpper())
            .Select(g => new
            {
                ProductName = g.Key,
                Available = g.Sum(i => (decimal?)i.PhysicalStock - i.ReservedStock - i.CommittedStock) ?? 0m
            })
            .ToListAsync(cancellationToken);

        return stockList.ToDictionary(x => x.ProductName, x => Math.Max(0, x.Available), StringComparer.OrdinalIgnoreCase);
    }

    public async Task<InventoryDashboardKpis> GetDashboardKpisAsync(Guid branchId, CancellationToken cancellationToken = default)
    {
        var totalProducts = await _context.Products.CountAsync(cancellationToken);
        var activeProducts = await _context.Products.CountAsync(p => p.IsActive, cancellationToken);
        var soldOutProducts = await _context.Products.CountAsync(p => p.IsSoldOut, cancellationToken);
        var hiddenProducts = await _context.Products.CountAsync(p => !p.IsCatalogVisible, cancellationToken);
        var favoriteProducts = await _context.Products.CountAsync(p => p.IsFavorite, cancellationToken);

        // Low stock products count
        var lowStockCount = await _context.Products
            .CountAsync(p => p.IsActive && p.TrackInventory && p.MinimumStock > 0 &&
                (_context.Inventories.Where(i => i.ProductId == p.Id).Sum(i => (decimal?)i.PhysicalStock) ?? 0m) <= p.MinimumStock,
                cancellationToken);

        // Inventory Value for the branch
        var inventoryValue = await (from i in _context.Inventories
                                    join pr in _context.ProductPresentations on i.ProductId equals pr.ProductId
                                    where i.BranchWarehouse.BranchId == branchId && pr.IsBaseUnit && !pr.IsDeleted
                                    select i.PhysicalStock * pr.Cost)
                                   .SumAsync(cancellationToken);

        // Today's adjustments and transfers count (UTC date comparison)
        var today = DateTime.UtcNow.Date;
        var todayAdjustments = await _context.InventoryMovements
            .CountAsync(m => m.MovementDate >= today && 
                (m.MovementType == Domain.Enums.MovementType.PositiveAdjustment || m.MovementType == Domain.Enums.MovementType.NegativeAdjustment) &&
                ((m.FromBranchWarehouse != null && m.FromBranchWarehouse.BranchId == branchId) || 
                 (m.ToBranchWarehouse != null && m.ToBranchWarehouse.BranchId == branchId)), 
                cancellationToken);

        var todayTransfers = await _context.InventoryMovements
            .CountAsync(m => m.MovementDate >= today && 
                (m.MovementType == Domain.Enums.MovementType.TransferOut || m.MovementType == Domain.Enums.MovementType.TransferIn) &&
                ((m.FromBranchWarehouse != null && m.FromBranchWarehouse.BranchId == branchId) || 
                 (m.ToBranchWarehouse != null && m.ToBranchWarehouse.BranchId == branchId)), 
                cancellationToken);

        return new InventoryDashboardKpis(
            totalProducts,
            activeProducts,
            soldOutProducts,
            hiddenProducts,
            favoriteProducts,
            lowStockCount,
            inventoryValue,
            todayAdjustments,
            todayTransfers
        );
    }
}
