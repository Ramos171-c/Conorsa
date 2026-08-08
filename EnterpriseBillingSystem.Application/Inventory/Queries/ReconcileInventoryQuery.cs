using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using EnterpriseBillingSystem.Domain.Enums;
using EnterpriseBillingSystem.Domain.Repositories;

namespace EnterpriseBillingSystem.Application.Inventory.Queries;

public record InventoryReconciliationItemDto(
    Guid ProductId,
    string ProductCode,
    string ProductName,
    Guid BranchWarehouseId,
    string WarehouseName,
    decimal StoredStock,
    decimal CalculatedKardexStock,
    decimal Difference,
    bool HasDiscrepancy,
    string DiscrepancyReason
);

public record ReconcileInventoryQuery(
    Guid? BranchWarehouseId = null,
    Guid? ProductId = null
) : IRequest<List<InventoryReconciliationItemDto>>;

public class ReconcileInventoryQueryHandler : IRequestHandler<ReconcileInventoryQuery, List<InventoryReconciliationItemDto>>
{
    private readonly IInventoryRepository _inventoryRepository;
    private readonly IInventoryMovementRepository _movementRepository;
    private readonly IProductRepository _productRepository;
    private readonly IRepository<Domain.Entities.BranchWarehouse> _branchWarehouseRepository;
    private readonly IWarehouseRepository _warehouseRepository;

    public ReconcileInventoryQueryHandler(
        IInventoryRepository inventoryRepository,
        IInventoryMovementRepository movementRepository,
        IProductRepository productRepository,
        IRepository<Domain.Entities.BranchWarehouse> branchWarehouseRepository,
        IWarehouseRepository warehouseRepository)
    {
        _inventoryRepository = inventoryRepository;
        _movementRepository = movementRepository;
        _productRepository = productRepository;
        _branchWarehouseRepository = branchWarehouseRepository;
        _warehouseRepository = warehouseRepository;
    }

    public async Task<List<InventoryReconciliationItemDto>> Handle(ReconcileInventoryQuery request, CancellationToken cancellationToken)
    {
        var inventories = (await _inventoryRepository.GetAllAsync()).Where(i => !i.IsDeleted).ToList();
        var movements = (await _movementRepository.FindAsync(m => true)).ToList();
        var products = (await _productRepository.GetAllAsync()).ToDictionary(p => p.Id, p => p);
        var branchWarehouses = (await _branchWarehouseRepository.GetAllAsync()).ToDictionary(w => w.Id, w => w);
        var warehouses = (await _warehouseRepository.GetAllAsync()).ToDictionary(w => w.Id, w => w);

        if (request.BranchWarehouseId.HasValue)
        {
            inventories = inventories.Where(i => i.BranchWarehouseId == request.BranchWarehouseId.Value).ToList();
        }

        if (request.ProductId.HasValue)
        {
            inventories = inventories.Where(i => i.ProductId == request.ProductId.Value).ToList();
        }

        var result = new List<InventoryReconciliationItemDto>();

        foreach (var inv in inventories)
        {
            products.TryGetValue(inv.ProductId, out var product);
            branchWarehouses.TryGetValue(inv.BranchWarehouseId, out var bw);
            warehouses.TryGetValue(bw?.WarehouseId ?? Guid.Empty, out var warehouse);

            if (product == null || product.IsDeleted) continue;

            // Compute Kardex sum for this product and warehouse
            decimal totalEntries = 0;
            decimal totalExits = 0;

            foreach (var mov in movements)
            {
                if (mov.FromBranchWarehouseId == inv.BranchWarehouseId)
                {
                    // Exit or TransferOut from this warehouse
                    foreach (var d in mov.Details.Where(d => d.ProductId == inv.ProductId))
                    {
                        totalExits += d.QuantityInBaseUnit;
                    }
                }

                if (mov.ToBranchWarehouseId == inv.BranchWarehouseId)
                {
                    // Entry, Purchase, TransferIn, or Return into this warehouse
                    foreach (var d in mov.Details.Where(d => d.ProductId == inv.ProductId))
                    {
                        totalEntries += d.QuantityInBaseUnit;
                    }
                }
            }

            decimal calculatedStock = totalEntries - totalExits;
            decimal diff = inv.PhysicalStock - calculatedStock;
            bool hasDiscrepancy = Math.Abs(diff) > 0.0001m;

            string reason = "Stock coincide con Kardex.";
            if (hasDiscrepancy)
            {
                if (diff < 0)
                    reason = $"Stock registrado ({inv.PhysicalStock}) es menor que el calculado por Kardex ({calculatedStock}). Faltante de {Math.Abs(diff)} un.";
                else
                    reason = $"Stock registrado ({inv.PhysicalStock}) es mayor que el calculado por Kardex ({calculatedStock}). Sobrante de {diff} un.";
            }

            result.Add(new InventoryReconciliationItemDto(
                ProductId: inv.ProductId,
                ProductCode: product?.InternalCode ?? "N/A",
                ProductName: product?.Name ?? "N/A",
                BranchWarehouseId: inv.BranchWarehouseId,
                WarehouseName: warehouse?.Name ?? "N/A",
                StoredStock: inv.PhysicalStock,
                CalculatedKardexStock: calculatedStock,
                Difference: diff,
                HasDiscrepancy: hasDiscrepancy,
                DiscrepancyReason: reason
            ));
        }

        return result;
    }
}
