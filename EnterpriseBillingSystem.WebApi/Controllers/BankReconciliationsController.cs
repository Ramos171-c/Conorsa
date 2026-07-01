using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using EnterpriseBillingSystem.WebApi.Authorization;
using EnterpriseBillingSystem.Application.Banks.Commands;
using EnterpriseBillingSystem.Application.Banks.DTOs;
using EnterpriseBillingSystem.Application.Banks.Queries;

namespace EnterpriseBillingSystem.WebApi.Controllers;

[Route("api/v1/bank-reconciliations")]
public class BankReconciliationsController : ApiControllerBase
{
    /// <summary>Crea una nueva conciliación bancaria calculando el saldo del sistema automáticamente.</summary>
    [HttpPost]
    [HasPermission("bank.reconcile")]
    public async Task<ActionResult<BankReconciliationDto>> Create([FromBody] CreateBankReconciliationCommand command)
    {
        var result = await Mediator.Send(command);
        return Ok(result);
    }

    /// <summary>Obtiene todas las conciliaciones de una cuenta bancaria.</summary>
    [HttpGet]
    [HasPermission("bank.view")]
    public async Task<ActionResult<IEnumerable<BankReconciliationDto>>> GetByAccount([FromQuery] Guid bankAccountId)
    {
        var result = await Mediator.Send(new GetBankReconciliationsQuery(bankAccountId));
        return Ok(result);
    }
}
