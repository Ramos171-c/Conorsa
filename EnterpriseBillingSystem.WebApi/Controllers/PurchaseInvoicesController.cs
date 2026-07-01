using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using EnterpriseBillingSystem.WebApi.Authorization;
using EnterpriseBillingSystem.Application.Purchases.Commands;
using EnterpriseBillingSystem.Application.Purchases.Queries;
using EnterpriseBillingSystem.Application.Common.Models;

namespace EnterpriseBillingSystem.WebApi.Controllers;

[Route("api/v1/purchase-invoices")]
public class PurchaseInvoicesController : ApiControllerBase
{
    [HttpPost]
    [HasPermission("purchases.create")]
    public async Task<ActionResult<Guid>> Create([FromBody] CreatePurchaseInvoiceCommand command)
    {
        var id = await Mediator.Send(command);
        return CreatedAtAction(nameof(GetById), new { id }, id);
    }

    [HttpPost("{id:guid}/post")]
    [HasPermission("purchases.approve")]
    public async Task<ActionResult> Post(Guid id)
    {
        await Mediator.Send(new PostPurchaseInvoiceCommand(id));
        return NoContent();
    }

    [HttpPost("{id:guid}/cancel")]
    [HasPermission("purchases.approve")]
    public async Task<ActionResult> Cancel(Guid id, [FromBody] CancelPurchaseInvoiceCommand command)
    {
        if (id != command.PurchaseInvoiceId)
            return BadRequest(new { Message = "El Id en la ruta no coincide con el del cuerpo." });

        await Mediator.Send(command);
        return NoContent();
    }

    [HttpGet("{id:guid}")]
    [HasPermission("purchases.view")]
    public async Task<ActionResult<PurchaseInvoiceDetailDto>> GetById(Guid id)
    {
        var result = await Mediator.Send(new GetPurchaseInvoiceByIdQuery(id));
        if (result == null) return NotFound();
        return Ok(result);
    }

    [HttpGet]
    [HasPermission("purchases.view")]
    public async Task<ActionResult<EnterpriseBillingSystem.Application.Common.Models.PagedResult<PurchaseInvoiceListItemDto>>> GetPaged(
        [FromQuery] Guid? supplierId = null,
        [FromQuery] string? status = null,
        [FromQuery] DateTime? fromDate = null,
        [FromQuery] DateTime? toDate = null,
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 20)
    {
        var result = await Mediator.Send(new GetPurchaseInvoicesPagedQuery(
            supplierId, status, fromDate, toDate, pageNumber, pageSize));
        return Ok(result);
    }
}
