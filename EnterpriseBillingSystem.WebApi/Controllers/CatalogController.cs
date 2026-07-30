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
                .Where(p => p.Name != null);
                
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
            var webRootPath = env.WebRootPath ?? Path.Combine(Directory.GetCurrentDirectory(), "wwwroot");
            var bgImagePath = Path.Combine(webRootPath, "images", "catalog_background.png");
            
            var document = Document.Create(container =>
            {
                container.Page(page =>
                {
                    page.Size(PageSizes.A4.Landscape()); // Orientación horizontal (apaisado)
                    
                    // IMPORTANTE: Definimos el margen en los contenidos, NO en la página entera,
                    // para permitir que el fondo ocupe el 100% de la hoja (bleed edge-to-edge)
                    if (System.IO.File.Exists(bgImagePath))
                    {
                        page.Background().Image(bgImagePath, ImageScaling.Resize);
                    }
                    else
                    {
                        page.PageColor("#FFFFFF");
                    }
                    
                    page.DefaultTextStyle(x => x.FontFamily("Arial").FontSize(11).FontColor("#0F172A"));

                    page.Footer()
                        .PaddingHorizontal(60)
                        .PaddingBottom(20)
                        .AlignCenter()
                        .Text(x =>
                        {
                            x.Span("Página ").FontSize(9).FontColor("#64748B");
                            x.CurrentPageNumber().FontSize(9).FontColor("#64748B");
                        });

                    page.Content()
                        .PaddingHorizontal(50)
                        .PaddingVertical(25) // Margen ajustado para dar máximo espacio vertical a la imagen
                        .Column(column =>
                        {
                            var categories = productsArray.GroupBy(p => p.CategoryName).ToArray();
                            
                            for (int catIdx = 0; catIdx < categories.Length; catIdx++)
                            {
                                var categoryGroup = categories[catIdx];
                                var categoryName = categoryGroup.Key ?? "Otros";
                                
                                // A) Categoría de Separación (Centrado sobre el fondo de dulces)
                                column.Item().Height(420).AlignCenter().AlignMiddle().Column(catCol =>
                                {
                                    catCol.Item().Text(categoryName.ToUpper())
                                        .Bold()
                                        .FontSize(44)
                                        .FontColor("#E11D48") // Color rosa dulce
                                        .AlignCenter();
                                        
                                    catCol.Item().PaddingTop(15).Text("CATÁLOGO DE PRODUCTOS")
                                        .Bold()
                                        .FontSize(18)
                                        .FontColor("#64748B")
                                        .AlignCenter();
                                });
                                
                                column.Item().PageBreak();

                                // B) Lista de Productos Centrada Verticalmente
                                var prodArray = categoryGroup.ToArray();
                                for (int prodIdx = 0; prodIdx < prodArray.Length; prodIdx++)
                                {
                                    var product = prodArray[prodIdx];
                                    
                                    // 1. Nombre del Producto (Centrado y Grande)
                                    column.Item().Text(product.Name.ToUpper())
                                        .Bold()
                                        .FontSize(24)
                                        .FontColor("#0F172A")
                                        .AlignCenter();
                                        
                                    var ueText = product.Description?.Contains("U/E: ") == true
                                        ? product.Description.Split("U/E: ").LastOrDefault()?.Trim(')')
                                        : "N/A";
                                        
                                    // 2. Detalles (Centrado, SKU y U/E)
                                    column.Item().AlignCenter().PaddingTop(6).Text(x =>
                                    {
                                        x.Span("CÓDIGO SKU: ").Bold().FontSize(13).FontColor("#E11D48");
                                        x.Span($"{product.InternalCode}     •     ").FontSize(13).FontColor("#334155");
                                        
                                        x.Span("U/E: ").Bold().FontSize(13).FontColor("#E11D48");
                                        x.Span($"{ueText}").FontSize(13).FontColor("#334155");
                                    });

                                    column.Item().PaddingVertical(6).LineHorizontal(1f).LineColor("#F1F5F9");

                                    // 3. Imagen del Producto (Centrada abajo, 100% transparente y tamaño grande)
                                    var imgPlaced = false;
                                    if (!string.IsNullOrWhiteSpace(product.ImagePath))
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
                                        
                                        var webRoot = env.WebRootPath ?? Path.Combine(Directory.GetCurrentDirectory(), "wwwroot");
                                        var localImagePath = Path.Combine(webRoot, relativePath.TrimStart('/'));
                                        if (System.IO.File.Exists(localImagePath))
                                        {
                                            var transparentImageBytes = MakeBackgroundTransparent(localImagePath);
                                            column.Item()
                                                .AlignCenter()
                                                .MaxHeight(390) // Imagen significativamente más grande (hasta 390pt de alto)
                                                .Image(transparentImageBytes, ImageScaling.FitArea);
                                                
                                            imgPlaced = true;
                                        }
                                    }

                                    if (!imgPlaced)
                                    {
                                        column.Item()
                                            .AlignCenter()
                                            .Height(180)
                                            .Border(0.5f)
                                            .BorderColor("#E2E8F0")
                                            .Background("#F8FAFC")
                                            .AlignMiddle()
                                            .Text("Sin Imagen")
                                            .FontColor("#94A3B8")
                                            .Italic();
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

    private static byte[] MakeBackgroundTransparent(string imagePath)
    {
        try
        {
            using var original = SkiaSharp.SKBitmap.Decode(imagePath);
            if (original == null) return System.IO.File.ReadAllBytes(imagePath);

            using var transparentBitmap = new SkiaSharp.SKBitmap(original.Width, original.Height, SkiaSharp.SKColorType.Rgba8888, SkiaSharp.SKAlphaType.Premul);
            
            using (var canvas = new SkiaSharp.SKCanvas(transparentBitmap))
            {
                canvas.Clear(SkiaSharp.SKColors.Transparent);
                canvas.DrawBitmap(original, 0, 0);
            }

            // Remover fondo blanco o casi blanco (RGB >= 230) convirtiéndolo a transparente puro (Alpha = 0)
            for (int y = 0; y < transparentBitmap.Height; y++)
            {
                for (int x = 0; x < transparentBitmap.Width; x++)
                {
                    var color = transparentBitmap.GetPixel(x, y);
                    if (color.Red >= 230 && color.Green >= 230 && color.Blue >= 230)
                    {
                        transparentBitmap.SetPixel(x, y, SkiaSharp.SKColors.Transparent);
                    }
                }
            }

            using var image = SkiaSharp.SKImage.FromBitmap(transparentBitmap);
            using var data = image.Encode(SkiaSharp.SKEncodedImageFormat.Png, 100);
            return data.ToArray();
        }
        catch
        {
            return System.IO.File.ReadAllBytes(imagePath);
        }
    }
}
