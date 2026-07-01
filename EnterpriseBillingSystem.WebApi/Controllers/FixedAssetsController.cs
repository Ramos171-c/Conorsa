using System;
using System.Threading.Tasks;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using EnterpriseBillingSystem.Application.FixedAssets.Commands;
using EnterpriseBillingSystem.Application.FixedAssets.Queries;
using EnterpriseBillingSystem.Domain.Enums;
using EnterpriseBillingSystem.WebApi.Authorization;

namespace EnterpriseBillingSystem.WebApi.Controllers;

[ApiController]
[Route("api/v1/fixed-assets")]
[Authorize]
public class FixedAssetsController : ControllerBase
{
    private readonly ISender _mediator;

    public FixedAssetsController(ISender mediator)
    {
        _mediator = mediator;
    }

    [HttpGet]
    [HasPermission("assets.view")]
    public async Task<IActionResult> GetAll(
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 20,
        [FromQuery] Guid? categoryId = null,
        [FromQuery] FixedAssetStatus? status = null,
        [FromQuery] Guid? branchId = null)
    {
        var result = await _mediator.Send(new GetFixedAssetsPagedQuery(pageNumber, pageSize, categoryId, status, branchId));
        return Ok(result);
    }

    [HttpGet("{id:guid}")]
    [HasPermission("assets.view")]
    public async Task<IActionResult> GetById(Guid id)
    {
        var result = await _mediator.Send(new GetFixedAssetByIdQuery(id));
        return result == null ? NotFound() : Ok(result);
    }

    [HttpPost]
    [HasPermission("assets.create")]
    public async Task<IActionResult> Create([FromBody] CreateFixedAssetCommand command)
    {
        var id = await _mediator.Send(command);
        return CreatedAtAction(nameof(GetById), new { id }, id);
    }

    [HttpPut("{id:guid}")]
    [HasPermission("assets.edit")]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdateFixedAssetCommand command)
    {
        if (id != command.Id) return BadRequest("El Id de la ruta no coincide con el del cuerpo.");
        var success = await _mediator.Send(command);
        return success ? NoContent() : NotFound();
    }

    [HttpPost("{id:guid}/dispose")]
    [HasPermission("assets.dispose")]
    public async Task<IActionResult> Dispose(Guid id, [FromBody] DisposeRequest request)
    {
        var txId = await _mediator.Send(new DisposeFixedAssetCommand(
            id, request.DisposalDate, request.ProceedsFromSale, request.Notes));
        return Ok(new { transactionId = txId });
    }

    [HttpPost("{id:guid}/revalue")]
    [HasPermission("assets.edit")]
    public async Task<IActionResult> Revalue(Guid id, [FromBody] RevalueRequest request)
    {
        var txId = await _mediator.Send(new RevalueFixedAssetCommand(
            id, request.RevaluationAmount, request.RevaluationDate, request.Notes));
        return Ok(new { transactionId = txId });
    }

    [HttpPost("{id:guid}/impair")]
    [HasPermission("assets.edit")]
    public async Task<IActionResult> Impair(Guid id, [FromBody] ImpairRequest request)
    {
        var txId = await _mediator.Send(new ImpairFixedAssetCommand(
            id, request.ImpairmentAmount, request.ImpairmentDate, request.Notes));
        return Ok(new { transactionId = txId });
    }
}

public record DisposeRequest(DateTime DisposalDate, decimal ProceedsFromSale, string? Notes);
public record RevalueRequest(decimal RevaluationAmount, DateTime RevaluationDate, string? Notes);
public record ImpairRequest(decimal ImpairmentAmount, DateTime ImpairmentDate, string? Notes);
