using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using EnterpriseBillingSystem.Domain.Entities;
using EnterpriseBillingSystem.Domain.Enums;
using EnterpriseBillingSystem.Domain.Repositories;

namespace EnterpriseBillingSystem.Application.Sales.Commands;

public record ResetInventoryAndOrdersCommand() : IRequest<string>;

public class ResetInventoryAndOrdersCommandHandler : IRequestHandler<ResetInventoryAndOrdersCommand, string>
{
    private readonly ISalesOrderRepository _salesOrderRepository;
    private readonly IInventoryRepository _inventoryRepository;
    private readonly IUnitOfWork _unitOfWork;

    public ResetInventoryAndOrdersCommandHandler(
        ISalesOrderRepository salesOrderRepository,
        IInventoryRepository inventoryRepository,
        IUnitOfWork unitOfWork)
    {
        _salesOrderRepository = salesOrderRepository;
        _inventoryRepository = inventoryRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<string> Handle(ResetInventoryAndOrdersCommand request, CancellationToken cancellationToken)
    {
        // 1. Resetear todas las existencias de inventario a 0.00 (sin eliminar los productos del catálogo)
        var inventories = await _inventoryRepository.GetAllAsync();
        var inventoryList = inventories.ToList();
        foreach (var inv in inventoryList)
        {
            inv.PhysicalStock = 0;
            inv.ReservedStock = 0;
            inv.CommittedStock = 0;
            inv.LastModifiedBy = "SystemReset";
            inv.LastModifiedOnUtc = DateTime.UtcNow;
            _inventoryRepository.Update(inv);
        }

        // 2. Identificar pedidos del 18 de julio hacia atrás (<= 2026-07-18 23:59:59)
        var cutoffDate = new DateTime(2026, 7, 18, 23, 59, 59, DateTimeKind.Utc);
        var allOrders = await _salesOrderRepository.GetFilteredWithDetailsAsync(null, null, null, null, null, cancellationToken);
        var orderList = allOrders.ToList();

        var oldOrders = orderList.Where(so => so.OrderDate <= cutoffDate).ToList();
        int oldOrdersCount = oldOrders.Count;
        foreach (var order in oldOrders)
        {
            _salesOrderRepository.Remove(order);
        }

        // 3. Poner todos los pedidos del 19-20 de julio hacia adelante en estado "Recibido" (1)
        var recentOrders = orderList.Where(so => so.OrderDate > cutoffDate).ToList();
        int recentOrdersCount = recentOrders.Count;
        foreach (var order in recentOrders)
        {
            order.Status = SalesOrderStatus.Recibido;
            order.LastModifiedBy = "SystemReset";
            order.LastModifiedOnUtc = DateTime.UtcNow;
            _salesOrderRepository.Update(order);
        }

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return $"Reinicio completado con éxito: " +
               $"- Stock de {inventoryList.Count} productos reseteado a 0.00. " +
               $"- {oldOrdersCount} pedidos anteriores al 18 de Julio eliminados. " +
               $"- {recentOrdersCount} pedidos recientes puestos en estado 'Recibido'.";
    }
}
