using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using EnterpriseBillingSystem.WebApi.Authorization;
using EnterpriseBillingSystem.Application.CashSessions.Commands;
using EnterpriseBillingSystem.Application.CashSessions.Queries;
using EnterpriseBillingSystem.Application.Common.Models;

namespace EnterpriseBillingSystem.WebApi.Controllers;

[Route("api/v1/cash-sessions")]
public class CashSessionsController : ApiControllerBase
{
    [HttpPost("open")]
    [HasPermission("cash.open")]
    public async Task<ActionResult<Guid>> Open([FromBody] OpenCashSessionCommand command)
    {
        var id = await Mediator.Send(command);
        return Ok(id);
    }

    [HttpPost("{id:guid}/close")]
    [HasPermission("cash.close")]
    public async Task<ActionResult> Close(Guid id, [FromBody] CloseCashSessionCommand command)
    {
        if (id != command.CashSessionId)
            return BadRequest(new { Message = "El Id en la ruta no coincide con el del cuerpo." });

        await Mediator.Send(command);
        return NoContent();
    }

    [HttpGet("{id:guid}")]
    [HasPermission("cash.view")]
    public async Task<ActionResult<CashSessionDetailDto>> GetById(Guid id)
    {
        var session = await Mediator.Send(new GetCashSessionByIdQuery(id));
        if (session == null) return NotFound();
        return Ok(session);
    }

    [HttpGet("{id:guid}/movements")]
    [HasPermission("cash.view")]
    public async Task<ActionResult<List<CashMovementDto>>> GetMovements(Guid id)
    {
        var movements = await Mediator.Send(new GetCashMovementsQuery(id));
        return Ok(movements);
    }

    [HttpGet]
    [HasPermission("cash.view")]
    public async Task<ActionResult<PagedResult<CashSessionListItemDto>>> GetPaged(
        [FromQuery] Guid? cashRegisterId = null,
        [FromQuery] string? status = null,
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 20)
    {
        var result = await Mediator.Send(new GetCashSessionsQuery(cashRegisterId, status, pageNumber, pageSize));
        return Ok(result);
    }
}
