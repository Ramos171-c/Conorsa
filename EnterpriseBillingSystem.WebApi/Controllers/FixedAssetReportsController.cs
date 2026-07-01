using System;
using System.Threading.Tasks;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using EnterpriseBillingSystem.Application.FixedAssets.Queries;
using EnterpriseBillingSystem.WebApi.Authorization;

namespace EnterpriseBillingSystem.WebApi.Controllers;

[ApiController]
[Route("api/v1/fixed-assets/reports")]
[Authorize]
public class FixedAssetReportsController : ControllerBase
{
    private readonly ISender _mediator;

    public FixedAssetReportsController(ISender mediator)
    {
        _mediator = mediator;
    }

    /// <summary>Libro Maestro de Activos Fijos</summary>
    [HttpGet("register")]
    [HasPermission("assets.view")]
    public async Task<IActionResult> GetRegister(
        [FromQuery] Guid? categoryId = null,
        [FromQuery] Guid? branchId = null)
    {
        var result = await _mediator.Send(new GetFixedAssetRegisterQuery(categoryId, branchId));
        return Ok(result);
    }

    /// <summary>Reporte de Depreciación por Período</summary>
    [HttpGet("depreciation")]
    [HasPermission("assets.view")]
    public async Task<IActionResult> GetDepreciationReport(
        [FromQuery] int year,
        [FromQuery] int month)
    {
        var result = await _mediator.Send(new GetDepreciationReportQuery(year, month));
        return Ok(result);
    }

    /// <summary>Historial Completo de Movimientos de Activos</summary>
    [HttpGet("movements")]
    [HasPermission("assets.view")]
    public async Task<IActionResult> GetMovements(
        [FromQuery] Guid? fixedAssetId = null,
        [FromQuery] DateTime? startDate = null,
        [FromQuery] DateTime? endDate = null)
    {
        var result = await _mediator.Send(new GetFixedAssetMovementReportQuery(fixedAssetId, startDate, endDate));
        return Ok(result);
    }
}
