using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using EnterpriseBillingSystem.WebApi.Authorization;
using EnterpriseBillingSystem.Application.SalesGoals;

namespace EnterpriseBillingSystem.WebApi.Controllers;

[Route("api/v1/sales-goals")]
public class SalesGoalsController : ApiControllerBase
{
    [HttpGet]
    [HasPermission("admin.view")]
    public async Task<ActionResult<IEnumerable<SalesGoalDto>>> GetAll([FromQuery] Guid? userId)
    {
        var goals = await Mediator.Send(new GetSalesGoalsQuery(userId));
        return Ok(goals);
    }

    [HttpGet("my-goals")]
    public async Task<ActionResult<IEnumerable<SalesGoalDto>>> GetMyGoals()
    {
        var goals = await Mediator.Send(new GetMySalesGoalsQuery());
        return Ok(goals);
    }

    [HttpPost]
    [HasPermission("admin.view")]
    public async Task<ActionResult<Guid>> Create([FromBody] CreateSalesGoalCommand command)
    {
        var id = await Mediator.Send(command);
        return Ok(id);
    }

    [HttpPut("{id}")]
    [HasPermission("admin.view")]
    public async Task<ActionResult> Update(Guid id, [FromBody] UpdateSalesGoalCommand command)
    {
        if (id != command.Id)
        {
            return BadRequest(new { Message = "El Id en el cuerpo no coincide con el de la ruta." });
        }

        var result = await Mediator.Send(command);
        if (!result) return NotFound();

        return NoContent();
    }

    [HttpDelete("{id}")]
    [HasPermission("admin.view")]
    public async Task<ActionResult> Delete(Guid id)
    {
        var result = await Mediator.Send(new DeleteSalesGoalCommand(id));
        if (!result) return NotFound();

        return NoContent();
    }
}
