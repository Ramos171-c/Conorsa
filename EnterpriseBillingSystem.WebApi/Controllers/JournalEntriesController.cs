using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using EnterpriseBillingSystem.WebApi.Authorization;
using EnterpriseBillingSystem.Application.JournalEntries.Commands;
using EnterpriseBillingSystem.Application.JournalEntries.Queries;
using EnterpriseBillingSystem.Application.Common.Models;
using MediatR;

namespace EnterpriseBillingSystem.WebApi.Controllers;

[Route("api/v1/journal-entries")]
public class JournalEntriesController : ApiControllerBase
{
    [HttpPost]
    [HasPermission("accounting.create")]
    public async Task<ActionResult<Guid>> Create([FromBody] CreateJournalEntryCommand command)
    {
        var id = await Mediator.Send(command);
        return Ok(id);
    }

    [HttpPost("{id:guid}/post")]
    [HasPermission("accounting.post")]
    public async Task<ActionResult> PostEntry(Guid id)
    {
        await Mediator.Send(new PostJournalEntryCommand(id));
        return NoContent();
    }

    [HttpPost("{id:guid}/reverse")]
    [HasPermission("accounting.post")]
    public async Task<ActionResult<Guid>> ReverseEntry(Guid id, [FromBody] ReverseJournalEntryCommandBody body)
    {
        var reversalId = await Mediator.Send(new ReverseJournalEntryCommand(id, body.ReversalReason));
        return Ok(reversalId);
    }

    [HttpPost("close-period")]
    [HasPermission("accounting.close-period")]
    public async Task<ActionResult> ClosePeriod([FromBody] CloseAccountingPeriodCommand command)
    {
        await Mediator.Send(command);
        return NoContent();
    }

    [HttpGet]
    [HasPermission("accounting.view")]
    public async Task<ActionResult<PagedResult<JournalEntryDto>>> GetPaged(
        [FromQuery] DateTime? startDate = null,
        [FromQuery] DateTime? endDate = null,
        [FromQuery] string? accountCode = null,
        [FromQuery] string? sourceModule = null,
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 20)
    {
        var result = await Mediator.Send(new GetJournalEntriesPagedQuery(
            startDate, endDate, accountCode, sourceModule, pageNumber, pageSize));
        return Ok(result);
    }

    [HttpGet("{id:guid}")]
    [HasPermission("accounting.view")]
    public async Task<ActionResult<JournalEntryDto>> GetById(Guid id)
    {
        var result = await Mediator.Send(new GetJournalEntryByIdQuery(id));
        if (result == null) return NotFound();
        return Ok(result);
    }
}

public class ReverseJournalEntryCommandBody
{
    public string? ReversalReason { get; set; }
}
