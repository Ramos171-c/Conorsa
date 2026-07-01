using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using EnterpriseBillingSystem.WebApi.Authorization;
using EnterpriseBillingSystem.Application.CustomerCategories.Commands;
using EnterpriseBillingSystem.Application.CustomerCategories.Queries;
using EnterpriseBillingSystem.Application.CustomerCategories.DTOs;
using EnterpriseBillingSystem.Application.Common.Models;

namespace EnterpriseBillingSystem.WebApi.Controllers;

[Route("api/v1/[controller]")]
public class CustomerCategoriesController : ApiControllerBase
{
    [HttpPost]
    [HasPermission("customers.create")]
    public async Task<ActionResult<Guid>> Create([FromBody] CreateCustomerCategoryCommand command)
    {
        var id = await Mediator.Send(command);
        return CreatedAtAction(nameof(GetById), new { id }, id);
    }

    [HttpPut("{id}")]
    [HasPermission("customers.edit")]
    public async Task<ActionResult> Update(Guid id, [FromBody] UpdateCustomerCategoryCommand command)
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
    [HasPermission("customers.delete")]
    public async Task<ActionResult> Delete(Guid id)
    {
        var result = await Mediator.Send(new DeleteCustomerCategoryCommand(id));
        if (!result) return NotFound();

        return NoContent();
    }

    [HttpGet("{id}")]
    [HasPermission("customers.view")]
    public async Task<ActionResult<CustomerCategoryDto>> GetById(Guid id)
    {
        var category = await Mediator.Send(new GetCustomerCategoryByIdQuery(id));
        if (category == null) return NotFound();

        return Ok(category);
    }

    [HttpGet]
    [HasPermission("customers.view")]
    public async Task<ActionResult<PagedResult<CustomerCategoryDto>>> GetPaged([FromQuery] int pageNumber = 1, [FromQuery] int pageSize = 10, [FromQuery] string? searchTerm = null)
    {
        var result = await Mediator.Send(new GetCustomerCategoriesPagedQuery(pageNumber, pageSize, searchTerm));
        return Ok(result);
    }
}
