using System.Threading.Tasks;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using EnterpriseBillingSystem.Application.FixedAssets.Commands;
using EnterpriseBillingSystem.WebApi.Authorization;

namespace EnterpriseBillingSystem.WebApi.Controllers;

[ApiController]
[Route("api/v1/fixed-assets/depreciation")]
[Authorize]
public class FixedAssetDepreciationController : ControllerBase
{
    private readonly ISender _mediator;

    public FixedAssetDepreciationController(ISender mediator)
    {
        _mediator = mediator;
    }

    /// <summary>
    /// Ejecuta la depreciación mensual por lotes para todos los activos activos pendientes.
    /// </summary>
    [HttpPost("run")]
    [HasPermission("assets.depreciate")]
    public async Task<IActionResult> RunMonthlyDepreciation([FromBody] RunMonthlyDepreciationCommand command)
    {
        var result = await _mediator.Send(command);
        return Ok(result);
    }
}
