using System;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using EnterpriseBillingSystem.Application.Common.Interfaces;
using EnterpriseBillingSystem.Domain.Repositories;

namespace EnterpriseBillingSystem.Application.Inventory.Queries;

public record InventoryDashboardKpisDto(
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

public record GetInventoryDashboardQuery() : IRequest<InventoryDashboardKpisDto>;

public class GetInventoryDashboardQueryHandler : IRequestHandler<GetInventoryDashboardQuery, InventoryDashboardKpisDto>
{
    private readonly IInventoryRepository _inventoryRepository;
    private readonly ICurrentUserService _currentUserService;

    public GetInventoryDashboardQueryHandler(
        IInventoryRepository inventoryRepository,
        ICurrentUserService currentUserService)
    {
        _inventoryRepository = inventoryRepository;
        _currentUserService = currentUserService;
    }

    public async Task<InventoryDashboardKpisDto> Handle(GetInventoryDashboardQuery request, CancellationToken cancellationToken)
    {
        var branchId = _currentUserService.BranchId;
        if (branchId == null)
        {
            return new InventoryDashboardKpisDto(0, 0, 0, 0, 0, 0, 0, 0, 0);
        }

        var kpis = await _inventoryRepository.GetDashboardKpisAsync(branchId.Value, cancellationToken);

        return new InventoryDashboardKpisDto(
            TotalProducts: kpis.TotalProducts,
            ActiveProducts: kpis.ActiveProducts,
            SoldOutProducts: kpis.SoldOutProducts,
            HiddenProducts: kpis.HiddenProducts,
            FavoriteProducts: kpis.FavoriteProducts,
            LowStockProducts: kpis.LowStockProducts,
            InventoryValue: kpis.InventoryValue,
            TodayAdjustments: kpis.TodayAdjustments,
            TodayTransfers: kpis.TodayTransfers
        );
    }
}
