using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using EnterpriseBillingSystem.WebApi.Authorization;
using EnterpriseBillingSystem.Application.AccountsPayable.Commands;
using EnterpriseBillingSystem.Application.AccountsPayable.Queries;
using EnterpriseBillingSystem.Application.Common.Models;

namespace EnterpriseBillingSystem.WebApi.Controllers;

[Route("api/v1/accounts-payable")]
public class AccountsPayableController : ApiControllerBase
{
    [HttpPost("payments")]
    [HasPermission("ap.payment")]
    public async Task<ActionResult<Guid>> RegisterPayment([FromBody] RegisterAccountsPayablePaymentCommand command)
    {
        var paymentId = await Mediator.Send(command);
        return Ok(paymentId);
    }

    [HttpGet]
    [HasPermission("ap.view")]
    public async Task<ActionResult<PagedResult<AccountsPayableDto>>> GetPaged(
        [FromQuery] Guid? supplierId = null,
        [FromQuery] string? status = null,
        [FromQuery] DateTime? startDate = null,
        [FromQuery] DateTime? endDate = null,
        [FromQuery] bool? isOverdue = null,
        [FromQuery] bool? isPending = null,
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 20)
    {
        var result = await Mediator.Send(new GetAccountsPayablesPagedQuery(
            supplierId, status, startDate, endDate, isOverdue, isPending, pageNumber, pageSize));
        return Ok(result);
    }

    [HttpGet("{id:guid}")]
    [HasPermission("ap.view")]
    public async Task<ActionResult<AccountsPayableDetailDto>> GetById(Guid id)
    {
        var result = await Mediator.Send(new GetAccountsPayableByIdQuery(id));
        if (result == null) return NotFound();
        return Ok(result);
    }

    [HttpGet("supplier-statement/{supplierId:guid}")]
    [HasPermission("ap.view")]
    public async Task<ActionResult<SupplierStatementDto>> GetSupplierStatement(Guid supplierId)
    {
        var result = await Mediator.Send(new GetSupplierStatementQuery(supplierId));
        if (result == null) return NotFound();
        return Ok(result);
    }

    [HttpGet("aging-report")]
    [HasPermission("ap.view")]
    public async Task<ActionResult<List<AgingSupplierRowDto>>> GetAgingReport()
    {
        var result = await Mediator.Send(new GetAccountsPayableAgingReportQuery());
        return Ok(result);
    }

    [HttpPost("update-overdue")]
    [HasPermission("ap.manage")]
    public async Task<ActionResult<int>> UpdateOverdue()
    {
        var count = await Mediator.Send(new UpdateOverdueAccountsPayableCommand());
        return Ok(count);
    }
}
