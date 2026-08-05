using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using EnterpriseBillingSystem.WebApi.Authorization;
using EnterpriseBillingSystem.Application.Inventory.Commands;
using EnterpriseBillingSystem.Application.Inventory.Queries;
using EnterpriseBillingSystem.Application.Inventory.DTOs;
using EnterpriseBillingSystem.Application.Common.Models;

namespace EnterpriseBillingSystem.WebApi.Controllers;

[Route("api/v1/[controller]")]
public class InventoryController : ApiControllerBase
{
    [HttpPost("receive")]
    [HasPermission("inventory.adjust")]
    public async Task<ActionResult<Guid>> Receive([FromBody] ReceiveInventoryCommand command)
    {
        var id = await Mediator.Send(command);
        return Ok(id);
    }

    [HttpPost("adjust")]
    [HasPermission("inventory.adjust")]
    public async Task<ActionResult<Guid>> Adjust([FromBody] AdjustInventoryCommand command)
    {
        var id = await Mediator.Send(command);
        return Ok(id);
    }

    [HttpPost("transfer")]
    [HasPermission("inventory.adjust")]
    public async Task<ActionResult<Guid>> Transfer([FromBody] TransferInventoryCommand command)
    {
        var id = await Mediator.Send(command);
        return Ok(id);
    }

    [HttpGet("dashboard")]
    [HasPermission("inventory.view")]
    public async Task<ActionResult<InventoryDashboardKpisDto>> GetDashboard()
    {
        var result = await Mediator.Send(new GetInventoryDashboardQuery());
        return Ok(result);
    }

    [HttpGet("warehouses")]
    [HasPermission("inventory.view")]
    public async Task<ActionResult<IEnumerable<WarehouseDto>>> GetWarehouses()
    {
        var result = await Mediator.Send(new GetBranchWarehousesQuery());
        return Ok(result);
    }

    [HttpGet("stock")]
    [HasPermission("inventory.view")]
    public async Task<ActionResult<PagedResult<InventoryDto>>> GetStockInquiry(
        [FromQuery] Guid? branchWarehouseId,
        [FromQuery] Guid? productId,
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 10)
    {
        var result = await Mediator.Send(new GetStockInquiryQuery(branchWarehouseId, productId, pageNumber, pageSize));
        return Ok(result);
    }

    [HttpGet("kardex")]
    [HasPermission("inventory.view")]
    public async Task<ActionResult<PagedResult<KardexDto>>> GetKardex(
        [FromQuery] Guid branchWarehouseId,
        [FromQuery] Guid productId,
        [FromQuery] DateTime? startDate,
        [FromQuery] DateTime? endDate,
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 10)
    {
        var result = await Mediator.Send(new GetKardexQuery(branchWarehouseId, productId, startDate, endDate, pageNumber, pageSize));
        return Ok(result);
    }

    [HttpPost("recover-enproceso-stock")]
    [Microsoft.AspNetCore.Authorization.AllowAnonymous]
    public async Task<IActionResult> RecoverEnProcesoStock(
        [FromServices] EnterpriseBillingSystem.Domain.Repositories.ISalesOrderRepository salesOrderRepository,
        [FromServices] EnterpriseBillingSystem.Domain.Repositories.IInventoryMovementRepository movementRepository,
        [FromServices] EnterpriseBillingSystem.Domain.Repositories.IInventoryRepository inventoryRepository,
        [FromServices] EnterpriseBillingSystem.Infrastructure.Data.ApplicationDbContext dbContext,
        [FromServices] EnterpriseBillingSystem.Domain.Repositories.IUnitOfWork unitOfWork)
    {
        try
        {
            var enProcesoOrders = (await salesOrderRepository.GetFilteredWithDetailsAsync(
                customerId: null,
                status: "EnProceso",
                fromDate: null,
                toDate: null,
                routeId: null)).ToList();

            var restoredItems = new List<string>();
            decimal totalValueRestored = 0;

            foreach (var order in enProcesoOrders)
            {
                var movements = (await movementRepository.FindAsync(m => m.ReferenceDocument == order.OrderNumber && m.MovementType == EnterpriseBillingSystem.Domain.Enums.MovementType.Exit)).ToList();
                foreach (var mov in movements)
                {
                    foreach (var detail in mov.Details)
                    {
                        if (mov.FromBranchWarehouseId.HasValue)
                        {
                            var invRecord = await inventoryRepository.GetByWarehouseAndProductAsync(mov.FromBranchWarehouseId.Value, detail.ProductId);
                            if (invRecord != null)
                            {
                                invRecord.PhysicalStock += detail.QuantityInBaseUnit;
                                invRecord.LastModifiedBy = "RecoverySystem";
                                invRecord.LastModifiedOnUtc = DateTime.UtcNow;
                                
                                var itemVal = detail.QuantityInBaseUnit * (detail.Product?.CurrentCost ?? 0);
                                totalValueRestored += itemVal;
                                restoredItems.Add($"Restaurado {detail.QuantityInBaseUnit} unds para pedido {order.OrderNumber}");
                            }
                        }
                    }
                    dbContext.Set<EnterpriseBillingSystem.Domain.Entities.InventoryMovement>().Remove(mov);
                }
            }

            await unitOfWork.SaveChangesAsync(default);

            return Ok(new
            {
                Message = $"Inventario recuperado exitosamente para {enProcesoOrders.Count} pedidos en estado EnProceso.",
                RestoredOrdersCount = enProcesoOrders.Count,
                TotalValueRestored = totalValueRestored,
                RestoredItems = restoredItems
            });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { Message = $"Error al recuperar inventario: {ex.Message}" });
        }
    }
}
