using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using EnterpriseBillingSystem.WebApi.Authorization;
using EnterpriseBillingSystem.Application.PricingThresholds;

namespace EnterpriseBillingSystem.WebApi.Controllers;

[Route("api/v1/pricing-thresholds")]
public class PricingThresholdsController : ApiControllerBase
{
    [HttpGet]
    public async Task<ActionResult<IEnumerable<PricingThresholdDto>>> GetAll()
    {
        var thresholds = await Mediator.Send(new GetPricingThresholdsQuery());
        return Ok(thresholds);
    }

    [HttpPut]
    [HasPermission("admin.view")]
    public async Task<ActionResult> UpdateBulk([FromBody] UpdatePricingThresholdsCommand command)
    {
        var result = await Mediator.Send(command);
        if (!result) return BadRequest();

        return NoContent();
    }
}
