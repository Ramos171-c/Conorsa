using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using EnterpriseBillingSystem.WebApi.Authorization;
using EnterpriseBillingSystem.Application.Sales.Commands;
using EnterpriseBillingSystem.Application.Sales.Queries;
using EnterpriseBillingSystem.Application.Common.Models;

namespace EnterpriseBillingSystem.WebApi.Controllers;

[Route("api/v1/sales-orders")]
public class SalesOrdersController : ApiControllerBase
{
    /// <summary>
    /// Crear un pedido de venta en estado Borrador.
    /// </summary>
    [HttpPost]
    [HasPermission("sales.create")]
    public async Task<ActionResult<Guid>> Create([FromBody] CreateSalesOrderCommand command)
    {
        var id = await Mediator.Send(command);
        return CreatedAtAction(nameof(GetById), new { id }, id);
    }

    /// <summary>
    /// Obtener pedido de venta por ID con detalles.
    /// </summary>
    [HttpGet("{id:guid}")]
    [HasPermission("sales.view")]
    public async Task<ActionResult<SalesOrderDetailDto>> GetById(Guid id)
    {
        var order = await Mediator.Send(new GetSalesOrderByIdQuery(id));
        if (order == null) return NotFound();
        return Ok(order);
    }

    /// <summary>
    /// Listar pedidos de venta con paginación y filtros.
    /// </summary>
    [HttpGet]
    [HasPermission("sales.view")]
    public async Task<ActionResult<PagedResult<SalesOrderListItemDto>>> GetPaged(
        [FromQuery] Guid? customerId = null,
        [FromQuery] string? status = null,
        [FromQuery] DateTime? fromDate = null,
        [FromQuery] DateTime? toDate = null,
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 20)
    {
        // Extract role claim using multiple possible claim types (standard and raw JWT)
        var roleClaim = User.FindFirst(System.Security.Claims.ClaimTypes.Role)?.Value 
                        ?? User.FindFirst("role")?.Value 
                        ?? User.FindFirst("http://schemas.microsoft.com/ws/2008/06/identity/claims/role")?.Value;

        var isAdmin = string.Equals(roleClaim, "SUPER_ADMIN", StringComparison.OrdinalIgnoreCase) || 
                      string.Equals(roleClaim, "ADMINISTRADOR", StringComparison.OrdinalIgnoreCase) ||
                      User.IsInRole("SUPER_ADMIN") || 
                      User.IsInRole("ADMINISTRADOR");

        string? createdByFilter = null;
        if (!isAdmin)
        {
            // Extract user ID using multiple possible claim types
            createdByFilter = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value
                              ?? User.FindFirst("sub")?.Value
                              ?? User.FindFirst("http://schemas.xmlsoap.org/ws/2005/05/identity/claims/nameidentifier")?.Value;
        }

        Console.WriteLine($"[DEBUG-ORDERS] User: '{User.Identity?.Name}', RoleClaim: '{roleClaim}', IsAdmin: {isAdmin}, Filter: '{createdByFilter}'");

        var result = await Mediator.Send(new GetSalesOrdersQuery(customerId, status, fromDate, toDate, pageNumber, pageSize, createdByFilter));
        return Ok(result);
    }

    /// <summary>
    /// Confirmar un pedido de venta (Draft -> Confirmed).
    /// </summary>
    [HttpPost("{id:guid}/confirm")]
    [HasPermission("sales.edit")]
    public async Task<ActionResult> Confirm(Guid id)
    {
        await Mediator.Send(new ConfirmSalesOrderCommand(id));
        return NoContent();
    }

    /// <summary>
    /// Anular un pedido de venta.
    /// </summary>
    [HttpPost("{id:guid}/cancel")]
    [HasPermission("sales.cancel")]
    public async Task<ActionResult> Cancel(Guid id, [FromBody] CancelSalesOrderCommand command)
    {
        if (id != command.SalesOrderId)
            return BadRequest(new { Message = "El Id en la ruta no coincide con el del cuerpo." });

        await Mediator.Send(command);
        return NoContent();
    }

    /// <summary>
    /// Solicitar la anulación de un pedido de venta.
    /// </summary>
    [HttpPost("{id:guid}/request-cancellation")]
    [HasPermission("sales.edit")]
    public async Task<ActionResult> RequestCancellation(Guid id, [FromBody] RequestSalesOrderCancellationCommand command)
    {
        if (id != command.SalesOrderId)
            return BadRequest(new { Message = "El Id en la ruta no coincide con el del cuerpo." });

        await Mediator.Send(command);
        return NoContent();
    }

    /// <summary>
    /// Actualizar un pedido de venta completo.
    /// </summary>
    [HttpPut("{id:guid}")]
    [HasPermission("sales.edit")]
    public async Task<ActionResult> Update(Guid id, [FromBody] UpdateSalesOrderCommand command)
    {
        if (id != command.Id)
            return BadRequest(new { Message = "El Id en la ruta no coincide con el del cuerpo." });

        await Mediator.Send(command);
        return NoContent();
    }

    /// <summary>
    /// Registrar una devolución total o parcial de un pedido.
    /// </summary>
    [HttpPost("{id:guid}/return")]
    [HasPermission("sales.edit")]
    public async Task<ActionResult> ReturnOrder(Guid id, [FromBody] ReturnSalesOrderCommand command)
    {
        if (id != command.SalesOrderId)
            return BadRequest(new { Message = "El Id en la ruta no coincide con el del cuerpo." });

        await Mediator.Send(command);
        return NoContent();
    }

    /// <summary>
    /// Actualizar el estado de un pedido de venta.
    /// </summary>
    [HttpPut("{id:guid}/status")]
    [HasPermission("sales.edit")]
    public async Task<ActionResult> UpdateStatus(Guid id, [FromBody] EnterpriseBillingSystem.Domain.Enums.SalesOrderStatus status)
    {
        await Mediator.Send(new UpdateSalesOrderStatusCommand(id, status));
        return NoContent();
    }

    /// <summary>
    /// Obtener el consolidado de productos solicitados en pedidos.
    /// </summary>
    [HttpGet("consolidated-products")]
    [HasPermission("sales.view")]
    public async Task<ActionResult<System.Collections.Generic.IEnumerable<ConsolidatedProductDto>>> GetConsolidatedProducts(
        [FromQuery] Guid? customerId = null,
        [FromQuery] string? status = null,
        [FromQuery] DateTime? fromDate = null,
        [FromQuery] DateTime? toDate = null)
    {
        var result = await Mediator.Send(new GetSalesOrderConsolidatedProductsQuery(customerId, status, fromDate, toDate));
        return Ok(result);
    }
}
