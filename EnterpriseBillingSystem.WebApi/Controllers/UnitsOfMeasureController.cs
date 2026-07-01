using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using EnterpriseBillingSystem.WebApi.Authorization;
using EnterpriseBillingSystem.Application.UnitsOfMeasure.Commands;
using EnterpriseBillingSystem.Application.UnitsOfMeasure.Queries;
using EnterpriseBillingSystem.Application.UnitsOfMeasure.DTOs;
using EnterpriseBillingSystem.Application.Common.Models;

namespace EnterpriseBillingSystem.WebApi.Controllers;

[Route("api/v1/[controller]")]
public class UnitsOfMeasureController : ApiControllerBase
{
    [HttpPost]
    [HasPermission("units.create")]
    public async Task<ActionResult<Guid>> Create([FromBody] CreateUnitOfMeasureCommand command)
    {
        var id = await Mediator.Send(command);
        return CreatedAtAction(nameof(GetById), new { id }, id);
    }

    [HttpPut("{id}")]
    [HasPermission("units.edit")]
    public async Task<ActionResult> Update(Guid id, [FromBody] UpdateUnitOfMeasureCommand command)
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
    [HasPermission("units.delete")]
    public async Task<ActionResult> Delete(Guid id)
    {
        var result = await Mediator.Send(new DeleteUnitOfMeasureCommand(id));
        if (!result) return NotFound();

        return NoContent();
    }

    [HttpGet("{id}")]
    [HasPermission("units.view")]
    public async Task<ActionResult<UnitOfMeasureDto>> GetById(Guid id)
    {
        var uom = await Mediator.Send(new GetUnitOfMeasureByIdQuery(id));
        if (uom == null) return NotFound();

        return Ok(uom);
    }

    [HttpGet]
    [HasPermission("units.view")]
    public async Task<ActionResult<PagedResult<UnitOfMeasureDto>>> GetPaged([FromQuery] int pageNumber = 1, [FromQuery] int pageSize = 10, [FromQuery] string? searchTerm = null)
    {
        var result = await Mediator.Send(new GetUnitsOfMeasurePagedQuery(pageNumber, pageSize, searchTerm));
        return Ok(result);
    }
}
