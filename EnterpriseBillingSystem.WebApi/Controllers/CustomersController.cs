using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using EnterpriseBillingSystem.WebApi.Authorization;
using EnterpriseBillingSystem.Application.Customers.Commands;
using EnterpriseBillingSystem.Application.Customers.Queries;
using EnterpriseBillingSystem.Application.Customers.DTOs;
using EnterpriseBillingSystem.Application.Products.DTOs;
using EnterpriseBillingSystem.Application.Common.Models;
using EnterpriseBillingSystem.Domain.Enums;

namespace EnterpriseBillingSystem.WebApi.Controllers;

[Route("api/v1/[controller]")]
public class CustomersController : ApiControllerBase
{
    [HttpPost]
    [HasPermission("customers.create")]
    public async Task<ActionResult<Guid>> Create([FromBody] CreateCustomerCommand command)
    {
        var id = await Mediator.Send(command);
        return CreatedAtAction(nameof(GetById), new { id }, id);
    }

    [HttpPut("{id}")]
    [HasPermission("customers.edit")]
    public async Task<ActionResult> Update(Guid id, [FromBody] UpdateCustomerCommand command)
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
        var result = await Mediator.Send(new DeleteCustomerCommand(id));
        if (!result) return NotFound();

        return NoContent();
    }

    [HttpGet("{id}")]
    [HasPermission("customers.view")]
    public async Task<ActionResult<CustomerDto>> GetById(Guid id)
    {
        var customer = await Mediator.Send(new GetCustomerByIdQuery(id));
        if (customer == null) return NotFound();

        return Ok(customer);
    }

    [HttpGet]
    [HasPermission("customers.view")]
    public async Task<ActionResult<PagedResult<CustomerDto>>> GetPaged(
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 10,
        [FromQuery] string? searchTerm = null,
        [FromQuery] Guid? categoryId = null,
        [FromQuery] CustomerStatus? status = null)
    {
        var result = await Mediator.Send(new GetCustomersPagedQuery(pageNumber, pageSize, searchTerm, categoryId, status));
        return Ok(result);
    }

    [HttpPost("{id}/block")]
    [HasPermission("customers.edit")]
    public async Task<ActionResult> Block(Guid id)
    {
        var result = await Mediator.Send(new BlockCustomerCommand(id));
        if (!result) return NotFound();

        return NoContent();
    }

    [HttpPost("{id}/activate")]
    [HasPermission("customers.edit")]
    public async Task<ActionResult> Activate(Guid id)
    {
        var result = await Mediator.Send(new ActivateCustomerCommand(id));
        if (!result) return NotFound();

        return NoContent();
    }

    [HttpGet("pricing-profiles")]
    [HasPermission("customers.view")]
    public async Task<ActionResult<System.Collections.Generic.IEnumerable<CustomerPricingProfileDto>>> GetPricingProfiles()
    {
        var result = await Mediator.Send(new GetCustomerPricingProfilesQuery());
        return Ok(result);
    }

    [HttpPut("{id}/pricing-profile")]
    [HasPermission("customers.edit")]
    public async Task<ActionResult> UpdatePricingProfile(Guid id, [FromBody] UpdateCustomerPricingProfileCommand command)
    {
        if (id != command.CustomerId)
        {
            return BadRequest(new { Message = "El Id en el cuerpo no coincide con el de la ruta." });
        }

        var result = await Mediator.Send(command);
        if (!result) return NotFound();

        return NoContent();
    }

    [HttpGet("{id}/top-products")]
    [HasPermission("customers.view")]
    public async Task<ActionResult<System.Collections.Generic.IEnumerable<ProductDto>>> GetTopProducts(Guid id, [FromQuery] int limit = 10)
    {
        var result = await Mediator.Send(new GetCustomerTopProductsQuery(id, limit));
        return Ok(result);
    }
}
