using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using EnterpriseBillingSystem.WebApi.Authorization;
using EnterpriseBillingSystem.Application.Purchases.Commands;
using EnterpriseBillingSystem.Application.Purchases.Queries;

namespace EnterpriseBillingSystem.WebApi.Controllers;

[Route("api/v1/purchase-receipts")]
public class PurchaseReceiptsController : ApiControllerBase
{
    /// <summary>
    /// Registrar una recepción de mercancía. Soporta compra directa (sin PO) y recepción parcial.
    /// La recepción impacta el inventario de forma transaccional.
    /// </summary>
    [HttpPost]
    [HasPermission("purchases.receive")]
    public async Task<ActionResult<Guid>> Register([FromBody] RegisterPurchaseReceiptCommand command)
    {
        try
        {
            var id = await Mediator.Send(command);
            return CreatedAtAction(nameof(GetById), new { id }, id);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { Message = ex.Message });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { Message = ex.Message });
        }
        catch (Exception ex)
        {
            Serilog.Log.Error(ex, "Error al registrar la recepción de compra");
            return StatusCode(500, new { Message = ex.Message });
        }
    }

    /// <summary>
    /// Obtener recepción de compra por ID con detalles completos.
    /// </summary>
    [HttpGet("{id:guid}")]
    [HasPermission("purchases.view")]
    public async Task<ActionResult<PurchaseReceiptDetailDto>> GetById(Guid id)
    {
        var receipt = await Mediator.Send(new GetPurchaseReceiptByIdQuery(id));
        if (receipt == null) return NotFound();
        return Ok(receipt);
    }

    /// <summary>
    /// Listar recepciones de compra con paginación y filtros.
    /// </summary>
    [HttpGet]
    [HasPermission("purchases.view")]
    public async Task<ActionResult<PagedResult<PurchaseReceiptListItemDto>>> GetPaged(
        [FromQuery] Guid? supplierId = null,
        [FromQuery] Guid? purchaseOrderId = null,
        [FromQuery] string? status = null,
        [FromQuery] DateTime? fromDate = null,
        [FromQuery] DateTime? toDate = null,
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 20)
    {
        var result = await Mediator.Send(new GetPurchaseReceiptsQuery(
            supplierId, purchaseOrderId, status, fromDate, toDate, pageNumber, pageSize));
        return Ok(result);
    }
}
