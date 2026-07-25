using System;
using System.Threading.Tasks;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using EnterpriseBillingSystem.Application.Sales.Commands;
using EnterpriseBillingSystem.Application.Sales.Queries;

namespace EnterpriseBillingSystem.WebApi.Controllers;

[ApiController]
[Route("api/v1/route-liquidations")]
[Authorize]
public class RouteLiquidationsController : ControllerBase
{
    private readonly IMediator _mediator;

    public RouteLiquidationsController(IMediator mediator)
    {
        _mediator = mediator;
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateRouteLiquidationCommand command)
    {
        try
        {
            var liquidationId = await _mediator.Send(command);
            return Ok(new { Id = liquidationId, Message = "Liquidación de ruta procesada y confirmada exitosamente." });
        }
        catch (Exception ex)
        {
            return BadRequest(new { Message = ex.Message });
        }
    }

    [HttpGet]
    public async Task<IActionResult> GetPaged(
        [FromQuery] DateTime? fromDate,
        [FromQuery] DateTime? toDate,
        [FromQuery] Guid? routeId,
        [FromQuery] string? status,
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 20)
    {
        var result = await _mediator.Send(new GetRouteLiquidationsQuery(fromDate, toDate, routeId, status, pageNumber, pageSize));
        return Ok(result);
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(Guid id)
    {
        var result = await _mediator.Send(new GetRouteLiquidationByIdQuery(id));
        if (result == null) return NotFound(new { Message = "Liquidación de ruta no encontrada." });
        return Ok(result);
    }
}
