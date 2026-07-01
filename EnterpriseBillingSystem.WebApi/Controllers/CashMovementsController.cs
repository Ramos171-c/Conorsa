using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using EnterpriseBillingSystem.WebApi.Authorization;
using EnterpriseBillingSystem.Application.CashMovements.Commands;

namespace EnterpriseBillingSystem.WebApi.Controllers;

[Route("api/v1/cash-movements")]
public class CashMovementsController : ApiControllerBase
{
    /// <summary>
    /// Registrar un ingreso manual a la caja (CashIn).
    /// </summary>
    [HttpPost("in")]
    [HasPermission("cash.movement")]
    public async Task<ActionResult<Guid>> CashIn([FromBody] CreateCashInCommand command)
    {
        var id = await Mediator.Send(command);
        return Ok(id);
    }

    /// <summary>
    /// Registrar un egreso manual de la caja (CashOut) con motivo obligatorio.
    /// </summary>
    [HttpPost("out")]
    [HasPermission("cash.movement")]
    public async Task<ActionResult<Guid>> CashOut([FromBody] CreateCashOutCommand command)
    {
        var id = await Mediator.Send(command);
        return Ok(id);
    }
}
