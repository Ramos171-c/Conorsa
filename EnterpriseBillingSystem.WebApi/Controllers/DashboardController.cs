using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using EnterpriseBillingSystem.Application.Sales.Queries;

namespace EnterpriseBillingSystem.WebApi.Controllers;

[Route("api/v1/dashboard")]
public class DashboardController : ApiControllerBase
{
    /// <summary>
    /// Obtener métricas consolidadas del Dashboard para analítica visual y gráficas.
    /// </summary>
    [HttpGet("analytics")]
    [Microsoft.AspNetCore.Authorization.AllowAnonymous]
    public async Task<ActionResult<DashboardAnalyticsDto>> GetAnalytics(
        [FromQuery] DateTime? fromDate = null,
        [FromQuery] DateTime? toDate = null,
        [FromQuery] Guid? routeId = null)
    {
        var result = await Mediator.Send(new GetDashboardAnalyticsQuery(fromDate, toDate, routeId));
        return Ok(result);
    }

    /// <summary>
    /// Obtener resumen ultra-rápido consolidado del Dashboard (Ventas de hoy, Ganancia Bruta real y Ranking).
    /// </summary>
    [HttpGet("summary")]
    [Microsoft.AspNetCore.Authorization.AllowAnonymous]
    public async Task<ActionResult<DashboardSummaryDto>> GetSummary(
        [FromQuery] DateTime? fromDate = null,
        [FromQuery] DateTime? toDate = null)
    {
        var result = await Mediator.Send(new GetDashboardSummaryQuery(fromDate, toDate));
        return Ok(result);
    }
}
