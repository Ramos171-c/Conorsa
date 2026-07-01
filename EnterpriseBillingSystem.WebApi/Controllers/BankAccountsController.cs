using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using EnterpriseBillingSystem.WebApi.Authorization;
using EnterpriseBillingSystem.Application.Banks.Commands;
using EnterpriseBillingSystem.Application.Banks.Queries;
using EnterpriseBillingSystem.Application.Banks.DTOs;
using EnterpriseBillingSystem.Application.Common.Models;

namespace EnterpriseBillingSystem.WebApi.Controllers;

[Route("api/v1/bank-accounts")]
public class BankAccountsController : ApiControllerBase
{
    [HttpPost]
    [HasPermission("bank.create")]
    public async Task<ActionResult<Guid>> Create([FromBody] CreateBankAccountCommand command)
    {
        var id = await Mediator.Send(command);
        return CreatedAtAction(nameof(GetById), new { id }, id);
    }

    [HttpPut("{id}")]
    [HasPermission("bank.edit")]
    public async Task<ActionResult> Update(Guid id, [FromBody] UpdateBankAccountCommand command)
    {
        if (id != command.Id)
            return BadRequest(new { Message = "El Id en el cuerpo no coincide con el de la ruta." });

        var result = await Mediator.Send(command);
        if (!result) return NotFound();

        return NoContent();
    }

    [HttpGet("{id}")]
    [HasPermission("bank.view")]
    public async Task<ActionResult<BankAccountDto>> GetById(Guid id)
    {
        var accounts = await Mediator.Send(new GetBankAccountsPagedQuery(1, 1));
        // Simple fallback: search by id in all
        var all = await Mediator.Send(new GetBankAccountsPagedQuery(1, int.MaxValue));
        var account = all.Items.FirstOrDefault(a => a.Id == id);
        if (account == null) return NotFound();
        return Ok(account);
    }

    [HttpGet]
    [HasPermission("bank.view")]
    public async Task<ActionResult<PagedResult<BankAccountDto>>> GetPaged(
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 10,
        [FromQuery] Guid? bankId = null,
        [FromQuery] string? searchTerm = null)
    {
        var result = await Mediator.Send(new GetBankAccountsPagedQuery(pageNumber, pageSize, bankId, searchTerm));
        return Ok(result);
    }

    [HttpGet("{id}/statement")]
    [HasPermission("bank.view")]
    public async Task<ActionResult<BankAccountStatementDto>> GetStatement(
        Guid id,
        [FromQuery] DateTime startDate,
        [FromQuery] DateTime endDate)
    {
        var result = await Mediator.Send(new GetBankAccountStatementQuery(id, startDate, endDate));
        return Ok(result);
    }

    [HttpGet("{id}/book")]
    [HasPermission("bank.view")]
    public async Task<ActionResult> GetBankBook(
        Guid id,
        [FromQuery] DateTime startDate,
        [FromQuery] DateTime endDate,
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 50)
    {
        var result = await Mediator.Send(new GetBankBookQuery(id, startDate, endDate, pageNumber, pageSize));
        return Ok(result);
    }
}
