using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using EnterpriseBillingSystem.Domain.Entities;
using EnterpriseBillingSystem.Domain.Enums;
using EnterpriseBillingSystem.Domain.Repositories;
using InventoryEntity = EnterpriseBillingSystem.Domain.Entities.Inventory;

namespace EnterpriseBillingSystem.Application.Sales.Commands;

public record CleanupZeroStockEnCaminoOrdersCommand() : IRequest<int>;

public class CleanupZeroStockEnCaminoOrdersCommandHandler : IRequestHandler<CleanupZeroStockEnCaminoOrdersCommand, int>
{
    private readonly ISalesOrderRepository _salesOrderRepository;
    private readonly IWarehouseRepository _warehouseRepository;
    private readonly IInventoryRepository _inventoryRepository;
    private readonly IRepository<SalesOrderDetail> _salesOrderDetailRepository;
    private readonly IUnitOfWork _unitOfWork;

    public CleanupZeroStockEnCaminoOrdersCommandHandler(
        ISalesOrderRepository salesOrderRepository,
        IWarehouseRepository warehouseRepository,
        IInventoryRepository inventoryRepository,
        IRepository<SalesOrderDetail> salesOrderDetailRepository,
        IUnitOfWork unitOfWork)
    {
        _salesOrderRepository = salesOrderRepository;
        _warehouseRepository = warehouseRepository;
        _inventoryRepository = inventoryRepository;
        _salesOrderDetailRepository = salesOrderDetailRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<int> Handle(CleanupZeroStockEnCaminoOrdersCommand request, CancellationToken cancellationToken)
    {
        // 1. Locate Bodega Exhibición
        var warehouses = await _warehouseRepository.GetAllAsync();
        var mainWarehouse = warehouses.FirstOrDefault(w => w.Name.Contains("Exhibici", StringComparison.OrdinalIgnoreCase)) 
                         ?? warehouses.FirstOrDefault();

        if (mainWarehouse == null) return 0;

        // 2. Fetch all orders in status EnCamino
        var orders = await _salesOrderRepository.GetAllAsync();
        var enCaminoOrders = orders.Where(o => o.Status == SalesOrderStatus.EnCamino).ToList();

        int cleanedCount = 0;

        foreach (var orderHeader in enCaminoOrders)
        {
            var order = await _salesOrderRepository.GetByIdWithDetailsAsync(orderHeader.Id, cancellationToken);
            if (order == null || order.Details == null || !order.Details.Any()) continue;

            var detailsToRemove = new List<SalesOrderDetail>();
            decimal subTotal = 0;
            decimal totalDiscount = 0;
            decimal totalTax = 0;

            foreach (var detail in order.Details.ToList())
            {
                var inventory = await _inventoryRepository.GetByWarehouseAndProductAsync(mainWarehouse.Id, detail.ProductId, cancellationToken);

                if (inventory == null || inventory.PhysicalStock <= 0 || detail.Quantity <= 0)
                {
                    detailsToRemove.Add(detail);
                }
                else
                {
                    decimal quantityToKeep = Math.Min(detail.Quantity, inventory.PhysicalStock);
                    if (quantityToKeep <= 0)
                    {
                        detailsToRemove.Add(detail);
                    }
                    else
                    {
                        detail.Quantity = quantityToKeep;
                        var lineDiscount = quantityToKeep * detail.UnitPrice * (detail.DiscountPercentage / 100m);
                        var lineBase = quantityToKeep * detail.UnitPrice - lineDiscount;
                        var lineTax = lineBase * (detail.TaxPercentage / 100m);
                        
                        detail.DiscountAmount = lineDiscount;
                        detail.TaxAmount = lineTax;
                        detail.NetAmount = lineBase + lineTax;

                        subTotal += quantityToKeep * detail.UnitPrice;
                        totalDiscount += lineDiscount;
                        totalTax += lineTax;
                    }
                }
            }

            if (detailsToRemove.Any())
            {
                foreach (var detail in detailsToRemove)
                {
                    _salesOrderDetailRepository.Remove(detail);
                    order.Details.Remove(detail);
                }

                order.SubTotal = subTotal;
                order.DiscountAmount = totalDiscount;
                order.TaxAmount = totalTax;
                order.TotalAmount = subTotal - totalDiscount + totalTax;

                order.LastModifiedBy = "System-Cleanup";
                order.LastModifiedOnUtc = DateTime.UtcNow;

                cleanedCount++;
            }
        }

        if (cleanedCount > 0)
        {
            try
            {
                await _unitOfWork.SaveChangesAsync(cancellationToken);
            }
            catch (Microsoft.EntityFrameworkCore.DbUpdateConcurrencyException ex)
            {
                foreach (var entry in ex.Entries)
                {
                    var databaseValues = await entry.GetDatabaseValuesAsync(cancellationToken);
                    if (databaseValues == null)
                    {
                        entry.State = Microsoft.EntityFrameworkCore.EntityState.Detached;
                    }
                    else
                    {
                        entry.OriginalValues.SetValues(databaseValues);
                    }
                }
                await _unitOfWork.SaveChangesAsync(cancellationToken);
            }
        }

        return cleanedCount;
    }
}
