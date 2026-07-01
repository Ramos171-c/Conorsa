using System;
using System.Threading.Tasks;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using EnterpriseBillingSystem.Application.FixedAssets.Commands;
using EnterpriseBillingSystem.Application.FixedAssets.Queries;
using EnterpriseBillingSystem.WebApi.Authorization;

namespace EnterpriseBillingSystem.WebApi.Controllers;

[ApiController]
[Route("api/v1/fixed-asset-categories")]
[Authorize]
public class FixedAssetCategoriesController : ControllerBase
{
    private readonly ISender _mediator;

    public FixedAssetCategoriesController(ISender mediator)
    {
        _mediator = mediator;
    }

    [HttpGet]
    [HasPermission("assets.view")]
    public async Task<IActionResult> GetAll(
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 20,
        [FromQuery] string? searchTerm = null)
    {
        var result = await _mediator.Send(new GetFixedAssetCategoriesQuery(pageNumber, pageSize, searchTerm));
        return Ok(result);
    }

    [HttpGet("{id:guid}")]
    [HasPermission("assets.view")]
    public async Task<IActionResult> GetById(Guid id)
    {
        var result = await _mediator.Send(new GetFixedAssetCategoryByIdQuery(id));
        return result == null ? NotFound() : Ok(result);
    }

    [HttpPost]
    [HasPermission("assets.create")]
    public async Task<IActionResult> Create([FromBody] CreateFixedAssetCategoryCommand command)
    {
        var id = await _mediator.Send(command);
        return CreatedAtAction(nameof(GetById), new { id }, id);
    }

    [HttpPut("{id:guid}")]
    [HasPermission("assets.edit")]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdateFixedAssetCategoryCommand command)
    {
        if (id != command.Id) return BadRequest("El Id de la ruta no coincide con el del cuerpo.");
        var success = await _mediator.Send(command);
        return success ? NoContent() : NotFound();
    }
}
