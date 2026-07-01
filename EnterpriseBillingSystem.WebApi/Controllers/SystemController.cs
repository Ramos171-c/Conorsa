using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using EnterpriseBillingSystem.Application.System.Queries;
using EnterpriseBillingSystem.Domain.Entities;
using EnterpriseBillingSystem.Domain.Repositories;

namespace EnterpriseBillingSystem.WebApi.Controllers;

public record UpdateSystemParameterDto(string Value);

[Route("api/v1/system")]
public class SystemController : ApiControllerBase
{
    [HttpGet("status")]
    public async Task<ActionResult<SystemStatusDto>> GetStatus()
    {
        return await Mediator.Send(new GetSystemStatusQuery());
    }

    [HttpGet("branches")]
    public async Task<ActionResult<IEnumerable<BranchDto>>> GetBranches()
    {
        var result = await Mediator.Send(new GetBranchesQuery());
        return Ok(result);
    }

    [HttpGet("parameters")]
    public async Task<ActionResult<Dictionary<string, string>>> GetParameters([FromServices] IRepository<SystemParameter> repository)
    {
        var list = await repository.GetAllAsync();
        return Ok(list.ToDictionary(p => p.Key, p => p.Value));
    }

    [HttpGet("parameters/{key}")]
    public async Task<ActionResult<string>> GetParameter(string key, [FromServices] IRepository<SystemParameter> repository)
    {
        var param = (await repository.FindAsync(p => p.Key == key)).FirstOrDefault();
        if (param == null) return NotFound();
        return Ok(param.Value);
    }

    [HttpPut("parameters/{key}")]
    public async Task<ActionResult> UpdateParameter(string key, [FromBody] UpdateSystemParameterDto dto, [FromServices] IRepository<SystemParameter> repository, [FromServices] IUnitOfWork unitOfWork)
    {
        var param = (await repository.FindAsync(p => p.Key == key)).FirstOrDefault();
        if (param == null) return NotFound();
        
        param.Value = dto.Value;
        repository.Update(param);
        await unitOfWork.SaveChangesAsync(default);
        return NoContent();
    }

    [HttpGet("test-error")]
    public IActionResult TestError()
    {
        throw new InvalidOperationException("Esta es una excepción de prueba lanzada intencionalmente.");
    }
}
