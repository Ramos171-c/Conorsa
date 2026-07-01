using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using EnterpriseBillingSystem.WebApi.Authorization;
using EnterpriseBillingSystem.Application.PaymentMethods.Commands;
using EnterpriseBillingSystem.Application.PaymentMethods.Queries;
using EnterpriseBillingSystem.Application.Common.Models;

namespace EnterpriseBillingSystem.WebApi.Controllers;

[Route("api/v1/payment-methods")]
public class PaymentMethodsController : ApiControllerBase
{
    [HttpPost]
    [HasPermission("cash.manage")]
    public async Task<ActionResult<Guid>> Create([FromBody] CreatePaymentMethodCommand command)
    {
        var id = await Mediator.Send(command);
        return CreatedAtAction(nameof(GetById), new { id }, id);
    }

    [HttpGet("{id:guid}")]
    [HasPermission("cash.view")]
    public async Task<ActionResult<PaymentMethodDto>> GetById(Guid id)
    {
        var method = await Mediator.Send(new GetPaymentMethodByIdQuery(id));
        if (method == null) return NotFound();
        return Ok(method);
    }

    [HttpGet]
    [HasPermission("cash.view")]
    public async Task<ActionResult<PagedResult<PaymentMethodDto>>> GetPaged(
        [FromQuery] string? searchTerm = null,
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 20)
    {
        var result = await Mediator.Send(new GetPaymentMethodsQuery(searchTerm, pageNumber, pageSize));
        return Ok(result);
    }

    [HttpPut("{id:guid}")]
    [HasPermission("cash.manage")]
    public async Task<ActionResult> Update(Guid id, [FromBody] UpdatePaymentMethodCommand command)
    {
        if (id != command.Id)
            return BadRequest(new { Message = "El Id en la ruta no coincide con el del cuerpo." });

        await Mediator.Send(command);
        return NoContent();
    }

    [HttpDelete("{id:guid}")]
    [HasPermission("cash.manage")]
    public async Task<ActionResult> Delete(Guid id)
    {
        await Mediator.Send(new DeletePaymentMethodCommand(id));
        return NoContent();
    }
}
