using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using EnterpriseBillingSystem.WebApi.Authorization;
using EnterpriseBillingSystem.Application.Accounts.Commands;
using EnterpriseBillingSystem.Application.Accounts.Queries;

namespace EnterpriseBillingSystem.WebApi.Controllers;

[Route("api/v1/accounts")]
public class AccountsController : ApiControllerBase
{
    [HttpPost]
    [HasPermission("accounting.create")]
    public async Task<ActionResult<Guid>> Create([FromBody] CreateAccountCommand command)
    {
        var accountId = await Mediator.Send(command);
        return Ok(accountId);
    }

    [HttpPut("{id:guid}")]
    [HasPermission("accounting.edit")]
    public async Task<ActionResult> Update(Guid id, [FromBody] UpdateAccountCommand command)
    {
        if (id != command.Id)
        {
            return BadRequest("El ID del comando no coincide con el ID de la URL.");
        }

        await Mediator.Send(command);
        return NoContent();
    }

    [HttpGet("chart")]
    [HasPermission("accounting.view")]
    public async Task<ActionResult<IEnumerable<AccountDto>>> GetChart()
    {
        var chart = await Mediator.Send(new GetChartOfAccountsQuery());
        return Ok(chart);
    }

    [HttpGet("{id:guid}")]
    [HasPermission("accounting.view")]
    public async Task<ActionResult<AccountDto>> GetById(Guid id)
    {
        var account = await Mediator.Send(new GetAccountByIdQuery(id));
        if (account == null)
        {
            return NotFound();
        }
        return Ok(account);
    }
}
