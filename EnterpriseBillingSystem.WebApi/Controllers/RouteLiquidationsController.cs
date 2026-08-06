using System;
using System.Threading.Tasks;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using EnterpriseBillingSystem.Application.Sales.Commands;
using EnterpriseBillingSystem.Application.Sales.Queries;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace EnterpriseBillingSystem.WebApi.Controllers;

[ApiController]
[Route("api/v1/route-liquidations")]
[Authorize]
public class RouteLiquidationsController : ControllerBase
{
    private readonly IMediator _mediator;

    public RouteLiquidationsController(IMediator mediator)
    {
        _mediator = mediator;
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateRouteLiquidationCommand command)
    {
        try
        {
            var liquidationId = await _mediator.Send(command);
            return Ok(new { Id = liquidationId, Message = "Liquidación de ruta procesada y confirmada exitosamente." });
        }
        catch (Exception ex)
        {
            return BadRequest(new { Message = ex.Message });
        }
    }

    [HttpGet("returns-report")]
    public async Task<IActionResult> GetReturnsReport(
        [FromQuery] DateTime? fromDate,
        [FromQuery] DateTime? toDate,
        [FromQuery] Guid? routeId)
    {
        var result = await _mediator.Send(new GetRouteReturnsReportQuery(fromDate, toDate, routeId));
        return Ok(result);
    }

    [HttpGet("returns-report/pdf")]
    [AllowAnonymous]
    public async Task<IActionResult> ExportReturnsReportPdf(
        [FromQuery] DateTime? fromDate,
        [FromQuery] DateTime? toDate,
        [FromQuery] Guid? routeId)
    {
        try
        {
            QuestPDF.Settings.License = QuestPDF.Infrastructure.LicenseType.Community;
            var reportData = await _mediator.Send(new GetRouteReturnsReportQuery(fromDate, toDate, routeId));

            var document = QuestPDF.Fluent.Document.Create(container =>
            {
                container.Page(page =>
                {
                    page.Size(QuestPDF.Helpers.PageSizes.A4.Landscape());
                    page.Margin(30);
                    page.PageColor("#FFFFFF");
                    page.DefaultTextStyle(x => x.FontFamily("Arial").FontSize(10).FontColor("#1E293B"));

                    page.Header().Column(col =>
                    {
                        col.Item().Row(row =>
                        {
                            row.RelativeItem().Column(c =>
                            {
                                c.Item().Text("CONORTE — DISTRIBUIDORA").FontSize(20).Bold().FontColor("#1E1B4B");
                                c.Item().Text("Reporte Auditado de Devoluciones y Faltantes por Ruta").FontSize(13).FontColor("#475569");
                                var dateStr = $"{fromDate:dd/MM/yyyy} - {toDate:dd/MM/yyyy}";
                                c.Item().Text($"Período: {(fromDate.HasValue ? dateStr : "Todos los registros históricamente")}");
                            });
                            row.ConstantItem(200).AlignRight().Column(c =>
                            {
                                c.Item().Text($"Generado: {DateTime.Now:dd/MM/yyyy HH:mm}").FontSize(9).FontColor("#64748B");
                                c.Item().Text($"Total Registros: {reportData.TotalReturnedItemsCount}").FontSize(10).Bold();
                            });
                        });
                        col.Item().PaddingTop(10).LineHorizontal(1).LineColor("#CBD5E1");
                    });

                    page.Content().PaddingVertical(10).Column(col =>
                    {
                        // Summary Box
                        col.Item().PaddingBottom(12).Row(row =>
                        {
                            row.RelativeItem().Border(1).BorderColor("#CBD5E1").Background("#F8FAFC").Padding(10).Column(c =>
                            {
                                c.Item().Text("TOTAL PRODUCTOS/ITEMS DEVUELTOS").FontSize(9).Bold().FontColor("#64748B");
                                c.Item().Text($"{reportData.TotalReturnedItemsCount} ítems").FontSize(16).Bold().FontColor("#0F172A");
                            });
                            row.ConstantItem(15);
                            row.RelativeItem().Border(1).BorderColor("#CBD5E1").Background("#F8FAFC").Padding(10).Column(c =>
                            {
                                c.Item().Text("TOTAL UNIDADES FALTANTES / DEVUELTAS").FontSize(9).Bold().FontColor("#64748B");
                                c.Item().Text($"{reportData.TotalReturnedQuantity:N2} unds").FontSize(16).Bold().FontColor("#D97706");
                            });
                            row.ConstantItem(15);
                            row.RelativeItem().Border(1).BorderColor("#DC2626").Background("#FEF2F2").Padding(10).Column(c =>
                            {
                                c.Item().Text("TOTAL MONTO RESTADO DE VENTAS").FontSize(9).Bold().FontColor("#991B1B");
                                c.Item().Text($"-C$ {reportData.TotalReturnedAmount:N2}").FontSize(16).Bold().FontColor("#DC2626");
                            });
                        });

                        // Tabla de Productos Devueltos
                        col.Item().Table(table =>
                        {
                            table.ColumnsDefinition(columns =>
                            {
                                columns.ConstantColumn(90);  // Liquidación
                                columns.ConstantColumn(75);  // Fecha
                                columns.ConstantColumn(100); // Ruta
                                columns.ConstantColumn(75);  // SKU
                                columns.RelativeColumn(3);   // Producto
                                columns.ConstantColumn(55);  // Enviado
                                columns.ConstantColumn(60);  // Devuelto
                                columns.ConstantColumn(60);  // Precio
                                columns.ConstantColumn(80);  // Subtotal Restado
                                columns.RelativeColumn(2);   // Observaciones
                            });

                            table.Header(header =>
                            {
                                header.Cell().Background("#1E1B4B").Padding(5).Text("N° Liq.").Bold().FontColor("#FFFFFF").FontSize(9);
                                header.Cell().Background("#1E1B4B").Padding(5).Text("Fecha").Bold().FontColor("#FFFFFF").FontSize(9);
                                header.Cell().Background("#1E1B4B").Padding(5).Text("Ruta").Bold().FontColor("#FFFFFF").FontSize(9);
                                header.Cell().Background("#1E1B4B").Padding(5).Text("Código").Bold().FontColor("#FFFFFF").FontSize(9);
                                header.Cell().Background("#1E1B4B").Padding(5).Text("Producto").Bold().FontColor("#FFFFFF").FontSize(9);
                                header.Cell().Background("#1E1B4B").Padding(5).AlignRight().Text("Env.").Bold().FontColor("#FFFFFF").FontSize(9);
                                header.Cell().Background("#1E1B4B").Padding(5).AlignRight().Text("Devuelto").Bold().FontColor("#FFFFFF").FontSize(9);
                                header.Cell().Background("#1E1B4B").Padding(5).AlignRight().Text("Precio").Bold().FontColor("#FFFFFF").FontSize(9);
                                header.Cell().Background("#1E1B4B").Padding(5).AlignRight().Text("Monto Restado").Bold().FontColor("#FFFFFF").FontSize(9);
                                header.Cell().Background("#1E1B4B").Padding(5).Text("Observaciones").Bold().FontColor("#FFFFFF").FontSize(9);
                            });

                            foreach (var item in reportData.Items)
                            {
                                var bg = "#FFFFFF";
                                table.Cell().Background(bg).Padding(4).Text(item.LiquidationNumber).FontSize(8);
                                table.Cell().Background(bg).Padding(4).Text(item.LiquidationDate.ToString("dd/MM/yyyy")).FontSize(8);
                                table.Cell().Background(bg).Padding(4).Text(item.RouteName).FontSize(8);
                                table.Cell().Background(bg).Padding(4).Text(item.ProductCode).FontSize(8);
                                table.Cell().Background(bg).Padding(4).Text(item.ProductName).FontSize(8).Bold();
                                table.Cell().Background(bg).Padding(4).AlignRight().Text(item.QuantitySent.ToString("N0")).FontSize(8);
                                table.Cell().Background(bg).Padding(4).AlignRight().Text(item.QuantityReturned.ToString("N0")).FontSize(8).Bold().FontColor("#D97706");
                                table.Cell().Background(bg).Padding(4).AlignRight().Text($"C${item.SalePrice:N2}").FontSize(8);
                                table.Cell().Background(bg).Padding(4).AlignRight().Text($"-C${item.SubtotalReturned:N2}").FontSize(8).Bold().FontColor("#DC2626");
                                table.Cell().Background(bg).Padding(4).Text(item.Notes ?? "-").FontSize(8);
                            }
                        });
                    });

                    page.Footer().AlignCenter().Text(x =>
                    {
                        x.Span("Página ").FontSize(9).FontColor("#64748B");
                        x.CurrentPageNumber().FontSize(9).FontColor("#64748B");
                        x.Span(" de ").FontSize(9).FontColor("#64748B");
                        x.TotalPages().FontSize(9).FontColor("#64748B");
                    });
                });
            });

            var stream = new System.IO.MemoryStream();
            document.GeneratePdf(stream);
            stream.Position = 0;
            return File(stream, "application/pdf", $"Reporte_Devoluciones_Faltantes_{DateTime.Now:yyyyMMdd}.pdf");
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { Message = $"Error al generar PDF de devoluciones: {ex.Message}" });
        }
    }
}
