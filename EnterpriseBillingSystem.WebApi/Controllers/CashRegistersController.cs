using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using EnterpriseBillingSystem.WebApi.Authorization;
using EnterpriseBillingSystem.Application.CashRegisters.Commands;
using EnterpriseBillingSystem.Application.CashRegisters.Queries;
using EnterpriseBillingSystem.Application.Common.Models;

namespace EnterpriseBillingSystem.WebApi.Controllers;

[Route("api/v1/cash-registers")]
public class CashRegistersController : ApiControllerBase
{
    [HttpPost]
    [HasPermission("cash.manage")]
    public async Task<ActionResult<Guid>> Create([FromBody] CreateCashRegisterCommand command)
    {
        var id = await Mediator.Send(command);
        return CreatedAtAction(nameof(GetById), new { id }, id);
    }

    [HttpGet("{id:guid}")]
    [HasPermission("cash.view")]
    public async Task<ActionResult<CashRegisterDto>> GetById(Guid id)
    {
        var register = await Mediator.Send(new GetCashRegisterByIdQuery(id));
        if (register == null) return NotFound();
        return Ok(register);
    }

    [HttpGet]
    [HasPermission("cash.view")]
    public async Task<ActionResult<PagedResult<CashRegisterDto>>> GetPaged(
        [FromQuery] Guid? branchId = null,
        [FromQuery] string? searchTerm = null,
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 20)
    {
        var result = await Mediator.Send(new GetCashRegistersQuery(branchId, searchTerm, pageNumber, pageSize));
        return Ok(result);
    }

    [HttpPut("{id:guid}")]
    [HasPermission("cash.manage")]
    public async Task<ActionResult> Update(Guid id, [FromBody] UpdateCashRegisterCommand command)
    {
        if (id != command.Id)
            return BadRequest(new { Message = "El Id en la ruta no coincide con el del cuerpo." });

        await Mediator.Send(command);
        return NoContent();
    }
}
