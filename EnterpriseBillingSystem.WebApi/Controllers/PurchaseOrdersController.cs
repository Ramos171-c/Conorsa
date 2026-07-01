using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using EnterpriseBillingSystem.WebApi.Authorization;
using EnterpriseBillingSystem.Application.Purchases.Commands;
using EnterpriseBillingSystem.Application.Purchases.Queries;

namespace EnterpriseBillingSystem.WebApi.Controllers;

[Route("api/v1/purchase-orders")]
public class PurchaseOrdersController : ApiControllerBase
{
    /// <summary>
    /// Crear una orden de compra en estado Borrador.
    /// </summary>
    [HttpPost]
    [HasPermission("purchases.create")]
    public async Task<ActionResult<Guid>> Create([FromBody] CreatePurchaseOrderCommand command)
    {
        var id = await Mediator.Send(command);
        return CreatedAtAction(nameof(GetById), new { id }, id);
    }

    /// <summary>
    /// Obtener orden de compra por ID con detalles completos.
    /// </summary>
    [HttpGet("{id:guid}")]
    [HasPermission("purchases.view")]
    public async Task<ActionResult<PurchaseOrderDetailDto>> GetById(Guid id)
    {
        var order = await Mediator.Send(new GetPurchaseOrderByIdQuery(id));
        if (order == null) return NotFound();
        return Ok(order);
    }

    /// <summary>
    /// Listar órdenes de compra con paginación y filtros.
    /// </summary>
    [HttpGet]
    [HasPermission("purchases.view")]
    public async Task<ActionResult<PagedResult<PurchaseOrderListItemDto>>> GetPaged(
        [FromQuery] Guid? supplierId = null,
        [FromQuery] string? status = null,
        [FromQuery] DateTime? fromDate = null,
        [FromQuery] DateTime? toDate = null,
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 20)
    {
        var result = await Mediator.Send(new GetPurchaseOrdersQuery(supplierId, status, fromDate, toDate, pageNumber, pageSize));
        return Ok(result);
    }

    /// <summary>
    /// Aprobar una orden de compra (cambia de Borrador a Aprobada).
    /// </summary>
    [HttpPost("{id:guid}/approve")]
    [HasPermission("purchases.approve")]
    public async Task<ActionResult> Approve(Guid id)
    {
        await Mediator.Send(new ApprovePurchaseOrderCommand(id));
        return NoContent();
    }

    /// <summary>
    /// Anular una orden de compra.
    /// </summary>
    [HttpPost("{id:guid}/cancel")]
    [HasPermission("purchases.approve")]
    public async Task<ActionResult> Cancel(Guid id, [FromBody] CancelPurchaseOrderCommand command)
    {
        if (id != command.PurchaseOrderId)
            return BadRequest(new { Message = "El Id en la ruta no coincide con el del cuerpo." });

        await Mediator.Send(command);
        return NoContent();
    }
}
