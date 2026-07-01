using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using EnterpriseBillingSystem.WebApi.Authorization;
using EnterpriseBillingSystem.Application.Banks.Commands;
using EnterpriseBillingSystem.Application.Banks.Queries;
using EnterpriseBillingSystem.Application.Banks.DTOs;
using EnterpriseBillingSystem.Application.Common.Models;

namespace EnterpriseBillingSystem.WebApi.Controllers;

[Route("api/v1/banks")]
public class BanksController : ApiControllerBase
{
    [HttpPost]
    [HasPermission("bank.create")]
    public async Task<ActionResult<Guid>> Create([FromBody] CreateBankCommand command)
    {
        var id = await Mediator.Send(command);
        return CreatedAtAction(nameof(GetById), new { id }, id);
    }

    [HttpPut("{id}")]
    [HasPermission("bank.edit")]
    public async Task<ActionResult> Update(Guid id, [FromBody] UpdateBankCommand command)
    {
        if (id != command.Id)
            return BadRequest(new { Message = "El Id en el cuerpo no coincide con el de la ruta." });

        var result = await Mediator.Send(command);
        if (!result) return NotFound();

        return NoContent();
    }

    [HttpGet("{id}")]
    [HasPermission("bank.view")]
    public async Task<ActionResult<BankDto>> GetById(Guid id)
    {
        var bank = await Mediator.Send(new GetBankByIdQuery(id));
        if (bank == null) return NotFound();
        return Ok(bank);
    }

    [HttpGet]
    [HasPermission("bank.view")]
    public async Task<ActionResult<PagedResult<BankDto>>> GetPaged(
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 10,
        [FromQuery] string? searchTerm = null)
    {
        var result = await Mediator.Send(new GetBanksPagedQuery(pageNumber, pageSize, searchTerm));
        return Ok(result);
    }
}
