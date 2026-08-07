using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using EnterpriseBillingSystem.WebApi.Authorization;
using EnterpriseBillingSystem.Application.Sales.Commands;
using EnterpriseBillingSystem.Application.Sales.Queries;
using EnterpriseBillingSystem.Application.Common.Models;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using Serilog;

namespace EnterpriseBillingSystem.WebApi.Controllers;

[Route("api/v1/sales-orders")]
public class SalesOrdersController : ApiControllerBase
{
    /// <summary>
    /// Crear un pedido de venta en estado Borrador.
    /// </summary>
    [HttpPost]
    [HasPermission("sales.create")]
    public async Task<ActionResult<Guid>> Create([FromBody] CreateSalesOrderCommand command)
    {
        var id = await Mediator.Send(command);
        return CreatedAtAction(nameof(GetById), new { id }, id);
    }

    /// <summary>
    /// Obtener pedido de venta por ID con detalles.
    /// </summary>
    [HttpGet("{id:guid}")]
    [HasPermission("sales.view")]
    public async Task<ActionResult<SalesOrderDetailDto>> GetById(Guid id)
    {
        var order = await Mediator.Send(new GetSalesOrderByIdQuery(id));
        if (order == null) return NotFound();
        return Ok(order);
    }

    /// <summary>
    /// Listar pedidos de venta con paginación y filtros.
    /// </summary>
    [HttpGet]
    [Microsoft.AspNetCore.Authorization.AllowAnonymous]
    public async Task<ActionResult<PagedResult<SalesOrderListItemDto>>> GetPaged(
        [FromQuery] Guid? customerId = null,
        [FromQuery] string? status = null,
        [FromQuery] DateTime? fromDate = null,
        [FromQuery] DateTime? toDate = null,
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 20,
        [FromQuery] Guid? routeId = null)
    {
        try
        {
            string? createdByFilter = null;

            if (User?.Identity?.IsAuthenticated == true)
            {
                var roleClaim = User.FindFirst(System.Security.Claims.ClaimTypes.Role)?.Value 
                                ?? User.FindFirst("role")?.Value 
                                ?? User.FindFirst("http://schemas.microsoft.com/ws/2008/06/identity/claims/role")?.Value;

                var isAdmin = string.Equals(roleClaim, "SUPER_ADMIN", StringComparison.OrdinalIgnoreCase) || 
                              string.Equals(roleClaim, "ADMINISTRADOR", StringComparison.OrdinalIgnoreCase) ||
                              string.Equals(roleClaim, "ADMIN", StringComparison.OrdinalIgnoreCase);

                if (!isAdmin)
                {
                    createdByFilter = User.Identity.Name;
                }
            }

            Log.Information("[DEBUG-ORDERS] User: '{User}', IsAdminFilter: '{Filter}', RouteId: '{RouteId}', Status: '{Status}'", 
                User?.Identity?.Name, createdByFilter, routeId, status);

            var result = await Mediator.Send(new GetSalesOrdersQuery(customerId, status, fromDate, toDate, pageNumber, pageSize, createdByFilter, routeId));
            return Ok(result);
        }
        catch (Exception ex)
        {
            Log.Error(ex, "[ERROR-ORDERS] Error al listar pedidos de venta: {Message}", ex.Message);
            return StatusCode(500, new { Message = $"Error al obtener pedidos: {ex.Message} | Detalle: {ex.InnerException?.Message ?? ex.Message}" });
        }
    }

    /// <summary>
    /// Confirmar un pedido de venta (Draft -> Confirmed).
    /// </summary>
    [HttpPost("{id:guid}/confirm")]
    [HasPermission("sales.edit")]
    public async Task<ActionResult> Confirm(Guid id)
    {
        await Mediator.Send(new ConfirmSalesOrderCommand(id));
        return NoContent();
    }

    /// <summary>
    /// Anular un pedido de venta.
    /// </summary>
    [HttpPost("{id:guid}/cancel")]
    [HasPermission("sales.cancel")]
    public async Task<ActionResult> Cancel(Guid id, [FromBody] CancelSalesOrderCommand command)
    {
        if (id != command.SalesOrderId)
            return BadRequest(new { Message = "El Id en la ruta no coincide con el del cuerpo." });

        await Mediator.Send(command);
        return NoContent();
    }

    /// <summary>
    /// Solicitar la anulación de un pedido de venta.
    /// </summary>
    [HttpPost("{id:guid}/request-cancellation")]
    [HasPermission("sales.edit")]
    public async Task<ActionResult> RequestCancellation(Guid id, [FromBody] RequestSalesOrderCancellationCommand command)
    {
        if (id != command.SalesOrderId)
            return BadRequest(new { Message = "El Id en la ruta no coincide con el del cuerpo." });

        await Mediator.Send(command);
        return NoContent();
    }

    /// <summary>
    /// Actualizar un pedido de venta completo.
    /// </summary>
    [HttpPut("{id:guid}")]
    [HasPermission("sales.edit")]
    public async Task<ActionResult> Update(Guid id, [FromBody] UpdateSalesOrderCommand command)
    {
        if (id != command.Id)
            return BadRequest(new { Message = "El Id en la ruta no coincide con el del cuerpo." });

        await Mediator.Send(command);
        return NoContent();
    }

    /// <summary>
    /// Registrar una devolución total o parcial de un pedido.
    /// </summary>
    [HttpPost("{id:guid}/return")]
    [HasPermission("sales.edit")]
    public async Task<ActionResult> ReturnOrder(Guid id, [FromBody] ReturnSalesOrderCommand command)
    {
        if (id != command.SalesOrderId)
            return BadRequest(new { Message = "El Id en la ruta no coincide con el del cuerpo." });

        await Mediator.Send(command);
        return NoContent();
    }

    /// <summary>
    /// Actualizar el estado de un pedido de venta.
    /// </summary>
    [HttpPut("{id:guid}/status")]
    [HasPermission("sales.edit")]
    public async Task<ActionResult> UpdateStatus(Guid id, [FromBody] EnterpriseBillingSystem.Domain.Enums.SalesOrderStatus status)
    {
        await Mediator.Send(new UpdateSalesOrderStatusCommand(id, status));
        return NoContent();
    }

    /// <summary>
    /// Obtener el consolidado de productos solicitados en pedidos.
    /// </summary>
    [HttpGet("consolidated-products")]
    [HasPermission("sales.view")]
    public async Task<ActionResult<System.Collections.Generic.IEnumerable<ConsolidatedProductDto>>> GetConsolidatedProducts(
        [FromQuery] Guid? customerId = null,
        [FromQuery] string? status = null,
        [FromQuery] DateTime? fromDate = null,
        [FromQuery] DateTime? toDate = null,
        [FromQuery] Guid? routeId = null)
    {
        var result = await Mediator.Send(new GetSalesOrderConsolidatedProductsQuery(customerId, status, fromDate, toDate, routeId));
        return Ok(result);
    }
<<<<<<< HEAD
=======
    /// <summary>
    /// Limpiar productos sin existencias en pedidos En Camino.
    /// </summary>
    [HttpPost("cleanup-encamino")]
    [HasPermission("sales.edit")]
    public async Task<ActionResult<int>> CleanupEnCaminoOrders()
    {
        var count = await Mediator.Send(new CleanupZeroStockEnCaminoOrdersCommand());
        return Ok(count);
    }

    /// <summary>
    /// Reiniciar existencias de inventario a 0.00, eliminar pedidos <= 18 Julio y poner recientes en Recibido.
    /// </summary>
    [HttpPost("admin-reset-and-cleanup")]
    [Microsoft.AspNetCore.Authorization.AllowAnonymous]
    public async Task<ActionResult<object>> ResetInventoryAndOrders()
    {
        var result = await Mediator.Send(new ResetInventoryAndOrdersCommand());
        return Ok(new { Message = result });
    }

    /// <summary>
    /// Obtener reporte de ventas por vendedor (Preventa vs Entrega Efectiva y Devoluciones).
    /// </summary>
    [HttpGet("seller-report")]
    [Microsoft.AspNetCore.Authorization.AllowAnonymous]
    public async Task<ActionResult<SellerSalesReportDto>> GetSellerReport(
        [FromQuery] DateTime? fromDate = null,
        [FromQuery] DateTime? toDate = null,
        [FromQuery] Guid? routeId = null,
        [FromQuery] string? sellerName = null)
    {
        var result = await Mediator.Send(new GetSellerSalesReportQuery(fromDate, toDate, routeId, sellerName));
        return Ok(result);
    }

    /// <summary>
    /// Generar reporte PDF de ventas por vendedor (Preventa vs Entrega Efectiva).
    /// </summary>
    [HttpGet("seller-report/pdf")]
    [Microsoft.AspNetCore.Authorization.AllowAnonymous]
    public async Task<IActionResult> GetSellerReportPdf(
        [FromQuery] DateTime? fromDate = null,
        [FromQuery] DateTime? toDate = null,
        [FromQuery] Guid? routeId = null,
        [FromQuery] string? sellerName = null)
    {
        QuestPDF.Settings.License = LicenseType.Community;
        var reportData = await Mediator.Send(new GetSellerSalesReportQuery(fromDate, toDate, routeId, sellerName));

        var pdfBytes = Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A4.Landscape());
                page.Margin(20);
                page.PageColor(Colors.White);

                page.Header().Row(row =>
                {
                    row.RelativeItem().Column(col =>
                    {
                        col.Item().Text("REPORTE DE VENTAS POR VENDEDOR").FontSize(18).Bold().FontColor(Colors.Blue.Darken3);
                        col.Item().Text("Preventa vs Entrega Efectiva y Devoluciones").FontSize(11).Italic().FontColor(Colors.Grey.Medium);
                        col.Item().Text($"Rango: {(fromDate.HasValue ? fromDate.Value.ToString("dd/MM/yyyy") : "Inicio")} - {(toDate.HasValue ? toDate.Value.ToString("dd/MM/yyyy") : "Hoy")}").FontSize(10);
                    });
                    row.ConstantItem(120).AlignRight().Text($"Fecha: {DateTime.Now:dd/MM/yyyy HH:mm}").FontSize(9);
                });

                page.Content().PaddingVertical(10).Column(col =>
                {
                    col.Item().Table(table =>
                    {
                        table.ColumnsDefinition(cols =>
                        {
                            cols.RelativeColumn(3); // Vendedor
                            cols.RelativeColumn(2); // Preventa
                            cols.RelativeColumn(2); // Llegó al Cliente
                            cols.RelativeColumn(2); // Devuelto / Faltante
                            cols.RelativeColumn(2); // % Efectividad
                        });

                        table.Header(header =>
                        {
                            header.Cell().Background(Colors.Blue.Darken2).Padding(5).Text("VENDEDOR").Bold().FontColor(Colors.White).FontSize(10);
                            header.Cell().Background(Colors.Blue.Darken2).Padding(5).AlignRight().Text("TOTAL PREVENTA").Bold().FontColor(Colors.White).FontSize(10);
                            header.Cell().Background(Colors.Blue.Darken2).Padding(5).AlignRight().Text("LLEGÓ AL CLIENTE").Bold().FontColor(Colors.White).FontSize(10);
                            header.Cell().Background(Colors.Blue.Darken2).Padding(5).AlignRight().Text("DEVUELTO / FALTÓ").Bold().FontColor(Colors.White).FontSize(10);
                            header.Cell().Background(Colors.Blue.Darken2).Padding(5).AlignRight().Text("% EFECTIVIDAD").Bold().FontColor(Colors.White).FontSize(10);
                        });

                        foreach (var item in reportData.Sellers)
                        {
                            table.Cell().BorderBottom(1).BorderColor(Colors.Grey.Lighten2).Padding(5).Text(item.SellerName).FontSize(10);
                            table.Cell().BorderBottom(1).BorderColor(Colors.Grey.Lighten2).Padding(5).AlignRight().Text($"C$ {item.TotalPresaleAmount:N2}").FontSize(10);
                            table.Cell().BorderBottom(1).BorderColor(Colors.Grey.Lighten2).Padding(5).AlignRight().Text($"C$ {item.TotalDeliveredAmount:N2}").Bold().FontColor(Colors.Green.Darken2).FontSize(10);
                            table.Cell().BorderBottom(1).BorderColor(Colors.Grey.Lighten2).Padding(5).AlignRight().Text($"C$ {item.TotalReturnedAmount:N2}").FontColor(Colors.Red.Medium).FontSize(10);
                            table.Cell().BorderBottom(1).BorderColor(Colors.Grey.Lighten2).Padding(5).AlignRight().Text($"{item.DeliveryEffectivenessPercentage:N1}%").Bold().FontSize(10);
                        }
                    });
                });

                page.Footer().AlignCenter().Text(x =>
                {
                    x.Span("Página ");
                    x.CurrentPageNumber();
                    x.Span(" de ");
                    x.TotalPages();
                });
            });
        }).GeneratePdf();

        return File(pdfBytes, "application/pdf", "Reporte_Vendedor_Preventa_vs_Entrega.pdf");
    }
<<<<<<< HEAD
>>>>>>> 821aed7 (feat: agregar endpoints seller-report y seller-report/pdf en SalesOrdersController)
=======

    /// <summary>
    /// Obtener el reporte de faltantes y perdida por productos no entregados en preventa.
    /// </summary>
    [HttpGet("shortages-report")]
    [Microsoft.AspNetCore.Authorization.AllowAnonymous]
    public async Task<ActionResult<PresaleShortagesReportDto>> GetShortagesReport(
        [FromQuery] DateTime? fromDate = null,
        [FromQuery] DateTime? toDate = null,
        [FromQuery] Guid? routeId = null)
    {
        var result = await Mediator.Send(new GetPresaleShortagesReportQuery(fromDate, toDate, routeId));
        return Ok(result);
    }

    /// <summary>
    /// Generar PDF del reporte de faltantes y perdida por productos no despachados.
    /// </summary>
    [HttpGet("shortages-report/pdf")]
    [Microsoft.AspNetCore.Authorization.AllowAnonymous]
    public async Task<IActionResult> GetShortagesReportPdf(
        [FromQuery] DateTime? fromDate = null,
        [FromQuery] DateTime? toDate = null,
        [FromQuery] Guid? routeId = null)
    {
        QuestPDF.Settings.License = LicenseType.Community;
        var reportData = await Mediator.Send(new GetPresaleShortagesReportQuery(fromDate, toDate, routeId));

        var pdfBytes = Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A4.Landscape());
                page.Margin(20);
                page.PageColor(Colors.White);

                page.Header().Row(row =>
                {
                    row.RelativeItem().Column(col =>
                    {
                        col.Item().Text("REPORTE DE FALTANTES Y PÉRDIDA POR PRODUCTOS NO ENTREGADOS").FontSize(16).Bold().FontColor(Colors.Red.Darken2);
                        col.Item().Text("Detalle de Mercadería Solicitada en Preventa vs No Llegó al Cliente").FontSize(10).Italic().FontColor(Colors.Grey.Medium);
                        col.Item().Text($"Rango: {(fromDate.HasValue ? fromDate.Value.ToString("dd/MM/yyyy") : "Inicio")} - {(toDate.HasValue ? toDate.Value.ToString("dd/MM/yyyy") : "Hoy")} | Pérdida Total Estimada: C$ {reportData.TotalPresaleLossAmount:N2}").FontSize(10).Bold();
                    });
                    row.ConstantItem(120).AlignRight().Text($"Fecha: {DateTime.Now:dd/MM/yyyy HH:mm}").FontSize(9);
                });

                page.Content().PaddingVertical(10).Column(col =>
                {
                    col.Item().Table(table =>
                    {
                        table.ColumnsDefinition(cols =>
                        {
                            cols.RelativeColumn(1.5f); // Código
                            cols.RelativeColumn(3.5f); // Producto
                            cols.RelativeColumn(1);    // UOM
                            cols.RelativeColumn(2);    // Solicitado
                            cols.RelativeColumn(2);    // Entregado
                            cols.RelativeColumn(2);    // Faltante
                            cols.RelativeColumn(2);    // Precio
                            cols.RelativeColumn(2);    // Pérdida C$
                        });

                        table.Header(header =>
                        {
                            header.Cell().Background(Colors.Red.Darken3).Padding(5).Text("CÓDIGO").Bold().FontColor(Colors.White).FontSize(9);
                            header.Cell().Background(Colors.Red.Darken3).Padding(5).Text("PRODUCTO").Bold().FontColor(Colors.White).FontSize(9);
                            header.Cell().Background(Colors.Red.Darken3).Padding(5).Text("UOM").Bold().FontColor(Colors.White).FontSize(9);
                            header.Cell().Background(Colors.Red.Darken3).Padding(5).AlignRight().Text("PREVENTA").Bold().FontColor(Colors.White).FontSize(9);
                            header.Cell().Background(Colors.Red.Darken3).Padding(5).AlignRight().Text("ENTREGADO").Bold().FontColor(Colors.White).FontSize(9);
                            header.Cell().Background(Colors.Red.Darken3).Padding(5).AlignRight().Text("FALTANTE").Bold().FontColor(Colors.White).FontSize(9);
                            header.Cell().Background(Colors.Red.Darken3).Padding(5).AlignRight().Text("PRECIO").Bold().FontColor(Colors.White).FontSize(9);
                            header.Cell().Background(Colors.Red.Darken3).Padding(5).AlignRight().Text("PÉRDIDA C$").Bold().FontColor(Colors.White).FontSize(9);
                        });

                        foreach (var item in reportData.Items)
                        {
                            table.Cell().BorderBottom(1).BorderColor(Colors.Grey.Lighten2).Padding(4).Text(item.ProductCode).FontSize(9);
                            table.Cell().BorderBottom(1).BorderColor(Colors.Grey.Lighten2).Padding(4).Text(item.ProductName).FontSize(9);
                            table.Cell().BorderBottom(1).BorderColor(Colors.Grey.Lighten2).Padding(4).Text(item.UnitOfMeasureCode).FontSize(9);
                            table.Cell().BorderBottom(1).BorderColor(Colors.Grey.Lighten2).Padding(4).AlignRight().Text($"{item.RequestedQuantity:N0}").FontSize(9);
                            table.Cell().BorderBottom(1).BorderColor(Colors.Grey.Lighten2).Padding(4).AlignRight().Text($"{item.DeliveredQuantity:N0}").FontSize(9);
                            table.Cell().BorderBottom(1).BorderColor(Colors.Grey.Lighten2).Padding(4).AlignRight().Text($"{item.ShortageQuantity:N0}").Bold().FontColor(Colors.Red.Medium).FontSize(9);
                            table.Cell().BorderBottom(1).BorderColor(Colors.Grey.Lighten2).Padding(4).AlignRight().Text($"C$ {item.UnitPrice:N2}").FontSize(9);
                            table.Cell().BorderBottom(1).BorderColor(Colors.Grey.Lighten2).Padding(4).AlignRight().Text($"C$ {item.TotalLossAmount:N2}").Bold().FontColor(Colors.Red.Darken3).FontSize(9);
                        }
                    });
                });

                page.Footer().AlignCenter().Text(x =>
                {
                    x.Span("Página ");
                    x.CurrentPageNumber();
                    x.Span(" de ");
                    x.TotalPages();
                });
            });
        }).GeneratePdf();

        return File(pdfBytes, "application/pdf", "Reporte_Faltantes_y_Perdidas_Preventa.pdf");
    }
>>>>>>> 4d7933a (feat: agregar reporte de faltantes y perdida por productos no entregados en preventa (JSON y PDF))
}
