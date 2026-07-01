using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using EnterpriseBillingSystem.WebApi.Authorization;
using EnterpriseBillingSystem.Application.Brands.Commands;
using EnterpriseBillingSystem.Application.Brands.Queries;
using EnterpriseBillingSystem.Application.Brands.DTOs;
using EnterpriseBillingSystem.Application.Common.Models;

namespace EnterpriseBillingSystem.WebApi.Controllers;

[Route("api/v1/[controller]")]
public class BrandsController : ApiControllerBase
{
    [HttpPost]
    [HasPermission("brands.create")]
    public async Task<ActionResult<Guid>> Create([FromBody] CreateBrandCommand command)
    {
        var id = await Mediator.Send(command);
        return CreatedAtAction(nameof(GetById), new { id }, id);
    }

    [HttpPut("{id}")]
    [HasPermission("brands.edit")]
    public async Task<ActionResult> Update(Guid id, [FromBody] UpdateBrandCommand command)
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
    [HasPermission("brands.delete")]
    public async Task<ActionResult> Delete(Guid id)
    {
        var result = await Mediator.Send(new DeleteBrandCommand(id));
        if (!result) return NotFound();

        return NoContent();
    }

    [HttpGet("{id}")]
    [HasPermission("brands.view")]
    public async Task<ActionResult<BrandDto>> GetById(Guid id)
    {
        var brand = await Mediator.Send(new GetBrandByIdQuery(id));
        if (brand == null) return NotFound();

        return Ok(brand);
    }

    [HttpGet]
    [HasPermission("brands.view")]
    public async Task<ActionResult<PagedResult<BrandDto>>> GetPaged([FromQuery] int pageNumber = 1, [FromQuery] int pageSize = 10, [FromQuery] string? searchTerm = null)
    {
        var result = await Mediator.Send(new GetBrandsPagedQuery(pageNumber, pageSize, searchTerm));
        return Ok(result);
    }
}
