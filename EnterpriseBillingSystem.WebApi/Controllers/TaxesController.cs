using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using EnterpriseBillingSystem.WebApi.Authorization;
using EnterpriseBillingSystem.Application.Taxes.Commands;
using EnterpriseBillingSystem.Application.Taxes.Queries;
using EnterpriseBillingSystem.Application.Taxes.DTOs;
using EnterpriseBillingSystem.Application.Common.Models;

namespace EnterpriseBillingSystem.WebApi.Controllers;

[Route("api/v1/[controller]")]
public class TaxesController : ApiControllerBase
{
    [HttpPost]
    [HasPermission("taxes.create")]
    public async Task<ActionResult<Guid>> Create([FromBody] CreateTaxCommand command)
    {
        var id = await Mediator.Send(command);
        return CreatedAtAction(nameof(GetById), new { id }, id);
    }

    [HttpPut("{id}")]
    [HasPermission("taxes.edit")]
    public async Task<ActionResult> Update(Guid id, [FromBody] UpdateTaxCommand command)
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
    [HasPermission("taxes.delete")]
    public async Task<ActionResult> Delete(Guid id)
    {
        var result = await Mediator.Send(new DeleteTaxCommand(id));
        if (!result) return NotFound();

        return NoContent();
    }

    [HttpGet("{id}")]
    [HasPermission("taxes.view")]
    public async Task<ActionResult<TaxDto>> GetById(Guid id)
    {
        var tax = await Mediator.Send(new GetTaxByIdQuery(id));
        if (tax == null) return NotFound();

        return Ok(tax);
    }

    [HttpGet]
    [HasPermission("taxes.view")]
    public async Task<ActionResult<PagedResult<TaxDto>>> GetPaged([FromQuery] int pageNumber = 1, [FromQuery] int pageSize = 10, [FromQuery] string? searchTerm = null)
    {
        var result = await Mediator.Send(new GetTaxesPagedQuery(pageNumber, pageSize, searchTerm));
        return Ok(result);
    }
}
