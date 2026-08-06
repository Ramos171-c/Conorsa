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
        await Task.CompletedTask;
        return 0;
    }
}
