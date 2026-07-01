using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using EnterpriseBillingSystem.WebApi.Authorization;
using EnterpriseBillingSystem.Application.JournalEntries.Queries;

namespace EnterpriseBillingSystem.WebApi.Controllers;

[Route("api/v1/financial-reports")]
public class FinancialReportsController : ApiControllerBase
{
    [HttpGet("ledger")]
    [HasPermission("accounting.view")]
    public async Task<ActionResult<IEnumerable<GeneralLedgerItemDto>>> GetGeneralLedger(
        [FromQuery] DateTime startDate,
        [FromQuery] DateTime endDate,
        [FromQuery] string? accountCode = null)
    {
        var result = await Mediator.Send(new GetGeneralLedgerQuery(startDate, endDate, accountCode));
        return Ok(result);
    }

    [HttpGet("trial-balance")]
    [HasPermission("accounting.view")]
    public async Task<ActionResult<TrialBalanceResultDto>> GetTrialBalance(
        [FromQuery] DateTime startDate,
        [FromQuery] DateTime endDate)
    {
        var result = await Mediator.Send(new GetTrialBalanceQuery(startDate, endDate));
        return Ok(result);
    }

    [HttpGet("income-statement")]
    [HasPermission("accounting.view")]
    public async Task<ActionResult<IncomeStatementResultDto>> GetIncomeStatement(
        [FromQuery] DateTime startDate,
        [FromQuery] DateTime endDate)
    {
        var result = await Mediator.Send(new GetIncomeStatementQuery(startDate, endDate));
        return Ok(result);
    }

    [HttpGet("balance-sheet")]
    [HasPermission("accounting.view")]
    public async Task<ActionResult<BalanceSheetResultDto>> GetBalanceSheet([FromQuery] DateTime endDate)
    {
        var result = await Mediator.Send(new GetBalanceSheetQuery(endDate));
        return Ok(result);
    }
}
