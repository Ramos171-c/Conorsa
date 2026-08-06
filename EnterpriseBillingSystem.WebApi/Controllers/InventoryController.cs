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
        [FromServices] EnterpriseBillingSystem.Domain.Repositories.IWarehouseRepository warehouseRepository,
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

            var allWarehouses = await warehouseRepository.GetAllAsync();
            var targetWarehouse = allWarehouses.FirstOrDefault(w => w.Name.Contains("Exhibici")) ?? allWarehouses.FirstOrDefault();
            var branchWarehouseId = targetWarehouse?.BranchWarehouses?.FirstOrDefault()?.Id;

            if (!branchWarehouseId.HasValue)
            {
                return BadRequest("No se encontró la bodega activa para restaurar inventario.");
            }

            var restoredItems = new List<string>();
            decimal totalValueRestored = 0;

            foreach (var order in enProcesoOrders)
            {
                foreach (var detail in order.Details)
                {
                    if (detail.Quantity <= 0) continue;

                    var invRecord = await inventoryRepository.GetByWarehouseAndProductAsync(branchWarehouseId.Value, detail.ProductId);
                    var conversionFactor = detail.Product?.Presentations?.FirstOrDefault(p => p.UnitOfMeasureId == detail.UnitOfMeasureId)?.ConversionFactor ?? 1.0000m;
                    if (conversionFactor <= 0) conversionFactor = 1.0000m;
                    var baseQty = detail.Quantity * conversionFactor;

                    if (invRecord != null)
                    {
                        invRecord.PhysicalStock += baseQty;
                        invRecord.LastModifiedBy = "RecoverySystem";
                        invRecord.LastModifiedOnUtc = DateTime.UtcNow;
                    }
                    else
                    {
                        var newInv = new EnterpriseBillingSystem.Domain.Entities.Inventory
                        {
                            Id = Guid.NewGuid(),
                            BranchWarehouseId = branchWarehouseId.Value,
                            ProductId = detail.ProductId,
                            PhysicalStock = baseQty,
                            ReservedStock = 0,
                            CommittedStock = 0,
                            CreatedBy = "RecoverySystem",
                            CreatedOnUtc = DateTime.UtcNow
                        };
                        await inventoryRepository.AddAsync(newInv);
                    }

                    var cost = detail.Product?.CurrentCost ?? detail.UnitPrice;
                    var itemVal = baseQty * cost;
                    totalValueRestored += itemVal;
                    restoredItems.Add($"Restaurado {baseQty} unds del producto '{detail.Product?.Name ?? detail.ProductId.ToString()}' de pedido {order.OrderNumber}");
                }

                // Eliminar movimientos de salida asociados
                var movements = (await movementRepository.FindAsync(m => m.ReferenceDocument == order.OrderNumber && m.MovementType == EnterpriseBillingSystem.Domain.Enums.MovementType.Exit)).ToList();
                foreach (var mov in movements)
                {
                    dbContext.Set<EnterpriseBillingSystem.Domain.Entities.InventoryMovement>().Remove(mov);
                }
            }

            await unitOfWork.SaveChangesAsync(default);

            return Ok(new
            {
                Message = $"Inventario recuperado exitosamente para {enProcesoOrders.Count} pedidos en estado EnProceso.",
                RestoredOrdersCount = enProcesoOrders.Count,
                TotalValueRestored = totalValueRestored,
                RestoredItemsCount = restoredItems.Count,
                RestoredItems = restoredItems.Take(50)
            });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { Message = $"Error al recuperar inventario: {ex.Message}" });
        }
    }
}
