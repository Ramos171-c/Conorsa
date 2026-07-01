using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using EnterpriseBillingSystem.Domain.Entities;

namespace EnterpriseBillingSystem.Domain.Repositories;

public interface IInventoryRepository : IRepository<Inventory>
{
    Task<Inventory?> GetByWarehouseAndProductAsync(Guid branchWarehouseId, Guid productId, CancellationToken cancellationToken = default);
    Task<(IEnumerable<Inventory> Items, int TotalCount)> GetStockInquiryAsync(
        Guid? branchWarehouseId,
        Guid? productId,
        int pageNumber,
        int pageSize,
        CancellationToken cancellationToken = default);

    Task<IEnumerable<(Product Product, decimal CurrentStock)>> GetLowStockItemsAsync(CancellationToken cancellationToken = default);

    Task<InventoryDashboardKpis> GetDashboardKpisAsync(Guid branchId, CancellationToken cancellationToken = default);
}

public record InventoryDashboardKpis(
    int TotalProducts,
    int ActiveProducts,
    int SoldOutProducts,
    int HiddenProducts,
    int FavoriteProducts,
    int LowStockProducts,
    decimal InventoryValue,
    int TodayAdjustments,
    int TodayTransfers
);
