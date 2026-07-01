using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using EnterpriseBillingSystem.WebApi.Authorization;
using EnterpriseBillingSystem.Application.AccountsReceivable.Commands;
using EnterpriseBillingSystem.Application.AccountsReceivable.Queries;
using EnterpriseBillingSystem.Application.Common.Models;

namespace EnterpriseBillingSystem.WebApi.Controllers;

[Route("api/v1/accounts-receivable")]
public class AccountsReceivableController : ApiControllerBase
{
    [HttpGet]
    [HasPermission("ar.view")]
    public async Task<ActionResult<PagedResult<AccountsReceivableDto>>> GetPaged(
        [FromQuery] Guid? customerId = null,
        [FromQuery] string? status = null,
        [FromQuery] DateTime? startDate = null,
        [FromQuery] DateTime? endDate = null,
        [FromQuery] bool? isOverdue = null,
        [FromQuery] bool? isPending = null,
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 20)
    {
        var result = await Mediator.Send(new GetAccountsReceivablesPagedQuery(
            customerId, status, startDate, endDate, isOverdue, isPending, pageNumber, pageSize));
        return Ok(result);
    }

    [HttpGet("{id:guid}")]
    [HasPermission("ar.view")]
    public async Task<ActionResult<AccountsReceivableDetailDto>> GetById(Guid id)
    {
        var result = await Mediator.Send(new GetAccountsReceivableByIdQuery(id));
        if (result == null) return NotFound();
        return Ok(result);
    }

    [HttpPost("payment")]
    [HasPermission("ar.payment")]
    public async Task<ActionResult<Guid>> RegisterPayment([FromBody] RegisterAccountsReceivablePaymentCommand command)
    {
        var paymentId = await Mediator.Send(command);
        return Ok(paymentId);
    }

    [HttpGet("customer/{customerId:guid}/statement")]
    [HasPermission("ar.view")]
    public async Task<ActionResult<CustomerStatementDto>> GetCustomerStatement(Guid customerId)
    {
        var result = await Mediator.Send(new GetCustomerStatementQuery(customerId));
        if (result == null) return NotFound();
        return Ok(result);
    }

    [HttpGet("aging-report")]
    [HasPermission("ar.view")]
    public async Task<ActionResult<List<AgingCustomerRowDto>>> GetAgingReport()
    {
        var result = await Mediator.Send(new GetAgingReportQuery());
        return Ok(result);
    }

    [HttpPost("update-overdue")]
    [HasPermission("ar.manage")]
    public async Task<ActionResult<int>> UpdateOverdue()
    {
        var count = await Mediator.Send(new UpdateOverdueAccountsReceivableCommand());
        return Ok(count);
    }
}
