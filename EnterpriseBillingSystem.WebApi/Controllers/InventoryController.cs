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
}
