using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using EnterpriseBillingSystem.Application.Products.Queries;
using EnterpriseBillingSystem.Application.Products.DTOs;
using EnterpriseBillingSystem.Application.Categories.Queries;
using EnterpriseBillingSystem.Application.Categories.DTOs;
using EnterpriseBillingSystem.Application.Common.Models;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace EnterpriseBillingSystem.WebApi.Controllers;

[Route("api/v1/[controller]")]
public class CatalogController : ApiControllerBase
{
    [HttpGet("products")]
    public async Task<ActionResult<IEnumerable<ProductDto>>> GetCatalogProducts()
    {
        var result = await Mediator.Send(new GetCatalogProductsQuery());
        
        // Build absolute URL for ImagePath
        var baseUri = $"{Request.Scheme}://{Request.Host}{Request.PathBase}";
        var mapped = result.Select(p => p with
        {
            ImagePath = string.IsNullOrWhiteSpace(p.ImagePath)
                ? $"{baseUri}/uploads/products/default-product.png"
                : $"{baseUri}{p.ImagePath}"
        }).ToList();

        return Ok(mapped);
    }

    [HttpGet("categories")]
    public async Task<ActionResult<PagedResult<CategoryDto>>> GetCatalogCategories()
    {
        var result = await Mediator.Send(new GetCategoriesPagedQuery(1, 100, null));
        return Ok(result);
    }

    [HttpGet("export/pdf")]
    public async Task<IActionResult> ExportToPdf([FromQuery] Guid? categoryId, [FromServices] IWebHostEnvironment env)
    {
        // Set QuestPDF Community License
        QuestPDF.Settings.License = LicenseType.Community;

        var productsList = await Mediator.Send(new GetCatalogProductsQuery());
        var products = productsList.AsEnumerable();
        if (categoryId.HasValue)
        {
            products = products.Where(p => p.CategoryId == categoryId.Value);
        }

        var productsArray = products.ToArray();
        if (productsArray.Length == 0)
        {
            return BadRequest(new { Message = "No hay productos en esta categoría para exportar." });
        }

        var pdfStream = new MemoryStream();
        
        var document = Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A4.Landscape());
                page.Margin(40); // ~1.5cm in points
                page.PageColor("#FFFFFF");
                page.DefaultTextStyle(x => x.FontFamily("Arial").FontSize(11).FontColor("#0F172A"));

                page.Footer()
                    .AlignCenter()
                    .Text(x =>
                    {
                        x.Span("Página ").FontSize(9).FontColor("#64748B");
                        x.CurrentPageNumber().FontSize(9).FontColor("#64748B");
                    });

                page.Content()
                    .Column(column =>
                    {
                        var categories = productsArray.GroupBy(p => p.CategoryName).ToArray();
                        
                        for (int catIdx = 0; catIdx < categories.Length; catIdx++)
                        {
                            var categoryGroup = categories[catIdx];
                            var categoryName = categoryGroup.Key ?? "Otros";
                            
                            // A) Category Divider Page
                            column.Item().Background("#0F172A").Height(400).AlignCenter().AlignMiddle().Column(catCol =>
                            {
                                catCol.Item().Text(categoryName.ToUpper())
                                    .Bold()
                                    .FontSize(42)
                                    .FontColor("#FFFFFF")
                                    .AlignCenter();
                                    
                                catCol.Item().PaddingTop(10).Text("CATÁLOGO DE PRODUCTOS")
                                    .FontSize(16)
                                    .FontColor("#CBD5E1")
                                    .AlignCenter();
                            });
                            
                            column.Item().PageBreak();

                            // B) Products Page
                            var prodArray = categoryGroup.ToArray();
                            for (int prodIdx = 0; prodIdx < prodArray.Length; prodIdx++)
                            {
                                var product = prodArray[prodIdx];
                                
                                column.Item().Row(row =>
                                {
                                    // Left side: Info
                                    row.RelativeItem(1.2f).Column(infoCol =>
                                    {
                                        infoCol.Spacing(10);
                                        
                                        infoCol.Item().Text(product.Name.ToUpper())
                                            .Bold()
                                            .FontSize(20)
                                            .FontColor("#1E3A8A");
                                            
                                        var ueText = product.Description?.Contains("U/E: ") == true
                                            ? product.Description.Split("U/E: ").LastOrDefault()?.Trim(')')
                                            : "N/A";
                                            
                                        infoCol.Item().Text(x =>
                                        {
                                            x.Span("CÓDIGO SKU: ").Bold().FontColor("#334155");
                                            x.Span($"{product.InternalCode}   |   ").FontColor("#475569");
                                            x.Span("MEDIDA: ").Bold().FontColor("#334155");
                                            x.Span($"{product.DefaultUnitOfMeasureCode}   |   ").FontColor("#475569");
                                            x.Span("U/E: ").Bold().FontColor("#334155");
                                            x.Span($"{ueText}").FontColor("#475569");
                                        });

                                        infoCol.Item().LineHorizontal(1f).LineColor("#CBD5E1");

                                        infoCol.Item().Text("LISTA DE PRECIOS").Bold().FontSize(12).FontColor("#1E293B");

                                        infoCol.Item().Table(table =>
                                        {
                                            table.ColumnsDefinition(columns =>
                                            {
                                                columns.RelativeColumn(2); // Tipo
                                                columns.RelativeColumn(1.5f); // Unidad
                                                columns.RelativeColumn(1.5f); // Caja
                                            });

                                            table.Header(header =>
                                            {
                                                header.Cell().Background("#E2E8F0").Padding(5).Text("TIPO PRECIO").Bold().FontSize(9);
                                                header.Cell().Background("#E2E8F0").Padding(5).Text("UNIDAD").Bold().FontSize(9);
                                                header.Cell().Background("#E2E8F0").Padding(5).Text("CAJA").Bold().FontSize(9);
                                            });

                                            var unitPres = product.Presentations.FirstOrDefault(pr => pr.IsBaseUnit || pr.ConversionFactor == 1.0000m);
                                            var boxPres = product.Presentations.FirstOrDefault(pr => !pr.IsBaseUnit && pr.ConversionFactor > 1.0000m);
                                            var boxFactor = boxPres?.ConversionFactor.ToString("0") ?? "0";

                                            // Mayorista
                                            table.Cell().Padding(5).Text("Mayorista").FontSize(10);
                                            table.Cell().Padding(5).Text($"C${unitPres?.WholesalePrice.ToString("F2") ?? "0.00"}").FontSize(10);
                                            table.Cell().Padding(5).Text($"C${boxPres?.WholesalePrice.ToString("F2") ?? "0.00"} ({boxFactor} uds)").FontSize(10);

                                            // Semimayorista
                                            table.Cell().Padding(5).Text("Semimayorista").FontSize(10);
                                            table.Cell().Padding(5).Text($"C${unitPres?.SemiWholesalePrice.ToString("F2") ?? "0.00"}").FontSize(10);
                                            table.Cell().Padding(5).Text($"C${boxPres?.SemiWholesalePrice.ToString("F2") ?? "0.00"} ({boxFactor} uds)").FontSize(10);

                                            // Detalle
                                            table.Cell().Padding(5).Text("Detalle").FontSize(10);
                                            table.Cell().Padding(5).Text($"C${unitPres?.RetailPrice.ToString("F2") ?? "0.00"}").FontSize(10);
                                            table.Cell().Padding(5).Text($"C${boxPres?.RetailPrice.ToString("F2") ?? "0.00"} ({boxFactor} uds)").FontSize(10);
                                        });

                                        if (!string.IsNullOrWhiteSpace(product.Description))
                                        {
                                            infoCol.Item().PaddingTop(10).Column(descCol =>
                                            {
                                                descCol.Item().Text("DESCRIPCIÓN ADICIONAL:").Bold().FontSize(9).FontColor("#334155");
                                                descCol.Item().Text(product.Description).FontSize(10).FontColor("#475569");
                                            });
                                        }
                                    });

                                    row.ConstantItem(30); // Spacing

                                    // Right side: Image
                                    var imgPlaced = false;
                                    if (!string.IsNullOrWhiteSpace(product.ImagePath) && env.WebRootPath != null)
                                    {
                                        var relativePath = product.ImagePath;
                                        if (relativePath.StartsWith("http", StringComparison.OrdinalIgnoreCase))
                                        {
                                            try
                                            {
                                                var uri = new Uri(relativePath);
                                                relativePath = uri.AbsolutePath;
                                            }
                                            catch { }
                                        }
                                        
                                        var localImagePath = Path.Combine(env.WebRootPath, relativePath.TrimStart('/'));
                                        if (System.IO.File.Exists(localImagePath))
                                        {
                                            row.RelativeItem(0.8f)
                                                .Height(350)
                                                .Border(1f)
                                                .BorderColor("#E2E8F0")
                                                .Background("#F8FAFC")
                                                .AlignCenter()
                                                .AlignMiddle()
                                                .Image(localImagePath)
                                                .FitArea();
                                                
                                            imgPlaced = true;
                                        }
                                    }

                                    if (!imgPlaced)
                                    {
                                        row.RelativeItem(0.8f)
                                            .Height(350)
                                            .Border(1f)
                                            .BorderColor("#E2E8F0")
                                            .Background("#F8FAFC")
                                            .AlignCenter()
                                            .AlignMiddle()
                                            .Text("Sin Imagen")
                                            .FontColor("#94A3B8")
                                            .Italic();
                                    }
                                });

                                if (prodIdx < prodArray.Length - 1 || catIdx < categories.Length - 1)
                                {
                                    column.Item().PageBreak();
                                }
                            }
                        }
                    });
            });
        });

        document.GeneratePdf(pdfStream);
        pdfStream.Position = 0;
        
        var categoryNameHeader = categoryId.HasValue && productsArray.Length > 0 ? productsArray[0].CategoryName : "Todos";
        var safeCategoryName = string.Join("_", categoryNameHeader.Split(Path.GetInvalidFileNameChars()));
        var fileName = $"catalogo_{safeCategoryName.ToLower().Replace(" ", "_")}.pdf";
        
        return File(pdfStream.ToArray(), "application/pdf", fileName);
    }
}
