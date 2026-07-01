using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using EnterpriseBillingSystem.WebApi.Authorization;
using EnterpriseBillingSystem.Application.Sales.Commands;
using EnterpriseBillingSystem.Application.Sales.Queries;
using EnterpriseBillingSystem.Application.Common.Models;

namespace EnterpriseBillingSystem.WebApi.Controllers;

[Route("api/v1/sales-invoices")]
public class SalesInvoicesController : ApiControllerBase
{
    /// <summary>
    /// Crear una factura de venta en estado Borrador (Draft).
    /// </summary>
    [HttpPost]
    [HasPermission("sales.create")]
    public async Task<ActionResult<Guid>> Create([FromBody] CreateSalesInvoiceCommand command)
    {
        var id = await Mediator.Send(command);
        return CreatedAtAction(nameof(GetById), new { id }, id);
    }

    /// <summary>
    /// Obtener factura de venta por ID con detalles e información histórica.
    /// </summary>
    [HttpGet("{id:guid}")]
    [HasPermission("sales.view")]
    public async Task<ActionResult<SalesInvoiceDetailDto>> GetById(Guid id)
    {
        var invoice = await Mediator.Send(new GetSalesInvoiceByIdQuery(id));
        if (invoice == null) return NotFound();
        return Ok(invoice);
    }

    /// <summary>
    /// Listar facturas de venta con paginación y filtros.
    /// </summary>
    [HttpGet]
    [HasPermission("sales.view")]
    public async Task<ActionResult<PagedResult<SalesInvoiceListItemDto>>> GetPaged(
        [FromQuery] Guid? customerId = null,
        [FromQuery] string? status = null,
        [FromQuery] bool? isCreditSale = null,
        [FromQuery] DateTime? fromDate = null,
        [FromQuery] DateTime? toDate = null,
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 20)
    {
        var result = await Mediator.Send(new GetSalesInvoicesQuery(customerId, status, isCreditSale, fromDate, toDate, pageNumber, pageSize));
        return Ok(result);
    }

    /// <summary>
    /// Confirmar/Publicar una factura de venta (Draft -> Posted).
    /// Mueve inventario y registra el movimiento en Kardex.
    /// </summary>
    [HttpPost("{id:guid}/post")]
    [HasPermission("sales.post")]
    public async Task<ActionResult> PostInvoice(Guid id)
    {
        await Mediator.Send(new PostSalesInvoiceCommand(id));
        return NoContent();
    }

    /// <summary>
    /// Anular una factura de venta confirmada (Posted -> Cancelled).
    /// Revierte el inventario movido.
    /// </summary>
    [HttpPost("{id:guid}/cancel")]
    [HasPermission("sales.cancel")]
    public async Task<ActionResult> Cancel(Guid id, [FromBody] CancelSalesInvoiceCommand command)
    {
        if (id != command.SalesInvoiceId)
            return BadRequest(new { Message = "El Id en la ruta no coincide con el del cuerpo." });

        await Mediator.Send(command);
        return NoContent();
    }
}
