using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using EnterpriseBillingSystem.WebApi.Authorization;
using EnterpriseBillingSystem.Application.Banks.Commands;

namespace EnterpriseBillingSystem.WebApi.Controllers;

[Route("api/v1/bank-transactions")]
public class BankTransactionsController : ApiControllerBase
{
    /// <summary>Registra un depósito en una cuenta bancaria. Opcionalmente integra con sesión de caja activa.</summary>
    [HttpPost("deposit")]
    [HasPermission("bank.deposit")]
    public async Task<ActionResult<Guid>> Deposit([FromBody] CreateDepositCommand command)
    {
        var id = await Mediator.Send(command);
        return Ok(new { Id = id, Message = "Depósito registrado exitosamente." });
    }

    /// <summary>Registra un retiro o cargo bancario. Opcionalmente integra con sesión de caja activa.</summary>
    [HttpPost("withdrawal")]
    [HasPermission("bank.withdraw")]
    public async Task<ActionResult<Guid>> Withdrawal([FromBody] CreateWithdrawalCommand command)
    {
        var id = await Mediator.Send(command);
        return Ok(new { Id = id, Message = "Retiro/cargo registrado exitosamente." });
    }

    /// <summary>Realiza una transferencia atómica entre dos cuentas bancarias.</summary>
    [HttpPost("transfer")]
    [HasPermission("bank.transfer")]
    public async Task<ActionResult<Guid>> Transfer([FromBody] CreateTransferCommand command)
    {
        var id = await Mediator.Send(command);
        return Ok(new { Id = id, Message = "Transferencia realizada exitosamente." });
    }
}
