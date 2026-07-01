using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using EnterpriseBillingSystem.Application.Auth.Queries;
using EnterpriseBillingSystem.Application.Auth.Commands;
using EnterpriseBillingSystem.Application.Common.Models;
using EnterpriseBillingSystem.WebApi.Authorization;

namespace EnterpriseBillingSystem.WebApi.Controllers;

[Route("api/v1/users")]
public class UsersController : ApiControllerBase
{
    [HttpGet]
    [HasPermission("users.view")]
    public async Task<ActionResult<PagedResult<UserDto>>> GetPaged(
        [FromQuery] string? searchTerm = null,
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 20)
    {
        var result = await Mediator.Send(new GetUsersQuery(searchTerm, pageNumber, pageSize));
        return Ok(result);
    }

    [HttpPost]
    [HasPermission("users.create")]
    public async Task<ActionResult<Guid>> Create([FromBody] CreateUserCommand command)
    {
        var id = await Mediator.Send(command);
        return Ok(id);
    }

    [HttpPut("{id:guid}")]
    [HasPermission("users.edit")]
    public async Task<ActionResult<bool>> Update(Guid id, [FromBody] UpdateUserCommand command)
    {
        if (id != command.Id) return BadRequest("El Id de la ruta no coincide con el cuerpo.");
        var success = await Mediator.Send(command);
        return Ok(success);
    }
}
