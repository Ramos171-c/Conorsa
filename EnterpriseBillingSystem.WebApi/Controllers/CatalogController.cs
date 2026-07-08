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
                    page.Size(PageSizes.A4); // Portrait A4
                    page.Margin(40);
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
                                
                                // A) Category Divider Page (Safe height: 200)
                                column.Item().Background("#0F172A").Height(200).AlignCenter().AlignMiddle().Column(catCol =>
                                {
                                    catCol.Item().Text(categoryName.ToUpper())
                                        .Bold()
                                        .FontSize(36)
                                        .FontColor("#FFFFFF")
                                        .AlignCenter();
                                        
                                    catCol.Item().PaddingTop(10).Text("CATÁLOGO DE PRODUCTOS")
                                        .FontSize(14)
                                        .FontColor("#CBD5E1")
                                        .AlignCenter();
                                });
                                
                                column.Item().PageBreak();

                                // B) Products List
                                var prodArray = categoryGroup.ToArray();
                                for (int prodIdx = 0; prodIdx < prodArray.Length; prodIdx++)
                                {
                                    var product = prodArray[prodIdx];
                                    
                                    // 1. Product Name (Centered)
                                    column.Item().AlignCenter().Text(product.Name.ToUpper())
                                        .Bold()
                                        .FontSize(20)
                                        .FontColor("#1E3A8A");
                                        
                                    // 2. Product Details (Centered)
                                    var ueText = product.Description?.Contains("U/E: ") == true
                                        ? product.Description.Split("U/E: ").LastOrDefault()?.Trim(')')
                                        : "N/A";
                                        
                                    column.Item().AlignCenter().Text(x =>
                                    {
                                        x.Span("CÓDIGO SKU: ").Bold().FontColor("#334155");
                                        x.Span($"{product.InternalCode}   •   ").FontColor("#475569");
                                        x.Span("MEDIDA: ").Bold().FontColor("#334155");
                                        x.Span($"{product.DefaultUnitOfMeasureCode}   •   ").FontColor("#475569");
                                        x.Span("U/E: ").Bold().FontColor("#334155");
                                        x.Span($"{ueText}").FontColor("#475569");
                                    });

                                    column.Item().PaddingVertical(5).LineHorizontal(1f).LineColor("#CBD5E1");

                                    // 3. Image (Centered, FitArea scaling)
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
                                            column.Item()
                                                .AlignCenter()
                                                .MaxHeight(250)
                                                .Image(localImagePath, ImageScaling.FitArea);
                                                
                                            imgPlaced = true;
                                        }
                                    }

                                    if (!imgPlaced)
                                    {
                                        column.Item()
                                            .AlignCenter()
                                            .Height(60)
                                            .Text("Sin Imagen")
                                            .FontColor("#94A3B8")
                                            .Italic();
                                    }

                                    // 4. Description (Centered)
                                    if (!string.IsNullOrWhiteSpace(product.Description))
                                    {
                                        column.Item().PaddingTop(5).AlignCenter().Text(product.Description)
                                            .FontSize(10)
                                            .FontColor("#475569");
                                    }

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
