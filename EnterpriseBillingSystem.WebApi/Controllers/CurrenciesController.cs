using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using EnterpriseBillingSystem.WebApi.Authorization;
using EnterpriseBillingSystem.Application.Currencies;

namespace EnterpriseBillingSystem.WebApi.Controllers;

[Route("api/v1/[controller]")]
public class CurrenciesController : ApiControllerBase
{
    [HttpGet]
    public async Task<ActionResult<IEnumerable<CurrencyDto>>> GetAll()
    {
        var currencies = await Mediator.Send(new GetCurrenciesQuery());
        return Ok(currencies);
    }

    [HttpPost]
    [HasPermission("admin.view")]
    public async Task<ActionResult<Guid>> Create([FromBody] CreateCurrencyCommand command)
    {
        var id = await Mediator.Send(command);
        return Ok(id);
    }

    [HttpPut("{id}")]
    [HasPermission("admin.view")]
    public async Task<ActionResult> Update(Guid id, [FromBody] UpdateCurrencyCommand command)
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
        var result = await Mediator.Send(new DeleteCurrencyCommand(id));
        if (!result) return BadRequest(new { Message = "No se pudo eliminar la divisa. Verifique que no sea la predeterminada." });

        return NoContent();
    }
}
