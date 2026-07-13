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
        try
        {
            // Set QuestPDF Community License & Enable Debugging
            QuestPDF.Settings.License = LicenseType.Community;
            QuestPDF.Settings.EnableDebugging = true;

            var productsList = await Mediator.Send(new GetCatalogProductsQuery());
            var products = productsList.AsEnumerable()
                .Where(p => p.Name != null && !p.Name.Contains("SURTIDO", StringComparison.OrdinalIgnoreCase));
                
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
            var bgImagePath = Path.Combine(env.WebRootPath, "images", "catalog_background.png");
            
            var document = Document.Create(container =>
            {
                container.Page(page =>
                {
                    page.Size(PageSizes.A4.Landscape()); // Orientación horizontal (apaisado)
                    page.Margin(60); // Margen para el marco decorado
                    
                    if (System.IO.File.Exists(bgImagePath))
                    {
                        page.Background().Image(bgImagePath);
                    }
                    else
                    {
                        page.PageColor("#FFFFFF");
                    }
                    
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
                                
                                // A) Categoría de Separación (Centrado sobre el fondo de dulces)
                                column.Item().Height(400).AlignCenter().AlignMiddle().Column(catCol =>
                                {
                                    catCol.Item().Text(categoryName.ToUpper())
                                        .Bold()
                                        .FontSize(42)
                                        .FontColor("#E11D48") // Color rosa dulce
                                        .AlignCenter();
                                        
                                    catCol.Item().PaddingTop(15).Text("CATÁLOGO DE PRODUCTOS")
                                        .Bold()
                                        .FontSize(18)
                                        .FontColor("#64748B")
                                        .AlignCenter();
                                });
                                
                                column.Item().PageBreak();

                                // B) Lista de Productos en Fila Horizontal
                                var prodArray = categoryGroup.ToArray();
                                for (int prodIdx = 0; prodIdx < prodArray.Length; prodIdx++)
                                {
                                    var product = prodArray[prodIdx];
                                    
                                    column.Item().PaddingVertical(10).Row(row =>
                                    {
                                        // Columna Izquierda: Información del Producto (55% del ancho)
                                        row.RelativeItem(11).Column(infoCol =>
                                        {
                                            infoCol.Item().Text(product.Name.ToUpper())
                                                .Bold()
                                                .FontSize(24)
                                                .FontColor("#0F172A");
                                                
                                            var ueText = product.Description?.Contains("U/E: ") == true
                                                ? product.Description.Split("U/E: ").LastOrDefault()?.Trim(')')
                                                : "N/A";
                                                
                                            infoCol.Item().PaddingTop(12).Text(x =>
                                            {
                                                x.Span("CÓDIGO SKU: ").Bold().FontSize(14).FontColor("#E11D48");
                                                x.Span($"{product.InternalCode}\n\n").FontSize(14).FontColor("#334155");
                                                
                                                x.Span("MEDIDA: ").Bold().FontSize(14).FontColor("#E11D48");
                                                x.Span($"{product.DefaultUnitOfMeasureCode}\n\n").FontSize(14).FontColor("#334155");
                                                
                                                x.Span("U/E: ").Bold().FontSize(14).FontColor("#E11D48");
                                                x.Span($"{ueText}").FontSize(14).FontColor("#334155");
                                            });

                                            infoCol.Item().PaddingVertical(10).LineHorizontal(1f).LineColor("#F1F5F9");

                                            if (!string.IsNullOrWhiteSpace(product.Description))
                                            {
                                                infoCol.Item().Text(product.Description)
                                                    .FontSize(13)
                                                    .FontColor("#475569");
                                            }
                                        });

                                        // Espaciador entre columnas
                                        row.ConstantItem(30);

                                        // Columna Derecha: Imagen del Producto (45% del ancho)
                                        row.RelativeItem(9).AlignMiddle().AlignCenter().Column(imgCol =>
                                        {
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
                                                    imgCol.Item()
                                                        .AlignCenter()
                                                        .MaxHeight(260)
                                                        .Image(localImagePath, ImageScaling.FitArea);
                                                        
                                                    imgPlaced = true;
                                                }
                                            }

                                            if (!imgPlaced)
                                            {
                                                imgCol.Item()
                                                    .AlignCenter()
                                                    .Height(120)
                                                    .Border(0.5f)
                                                    .BorderColor("#E2E8F0")
                                                    .Background("#F8FAFC")
                                                    .AlignMiddle()
                                                    .Text("Sin Imagen")
                                                    .FontColor("#94A3B8")
                                                    .Italic();
                                            }
                                        });
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
        catch (Exception ex)
        {
            return StatusCode(500, new 
            { 
                Error = ex.ToString(), 
                Message = ex.Message, 
                InnerError = ex.InnerException?.ToString() 
            });
        }
    }
}
