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
        var filteredResult = result.Where(p => p.Name != null && p.IsCatalogVisible && p.IsActive &&
            !p.Name.Contains("SURTIDO", StringComparison.OrdinalIgnoreCase) &&
            !(p.CategoryName != null && p.CategoryName.Contains("SURTIDO", StringComparison.OrdinalIgnoreCase)));
        
        // Build absolute URL for ImagePath
        var baseUri = $"{Request.Scheme}://{Request.Host}{Request.PathBase}";
        var mapped = filteredResult.Select(p => p with
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
                .Where(p => p.Name != null && p.IsCatalogVisible && p.IsActive &&
                            !(p.CategoryName != null && p.CategoryName.Contains("SURTIDO", StringComparison.OrdinalIgnoreCase)) &&
                            HasValidImage(p, env));
            if (categoryId.HasValue)
            {
                products = products.Where(p => p.CategoryId == categoryId.Value);
            }

            var productsArray = products.ToArray();
            if (productsArray.Length == 0)
            {
                return BadRequest(new { Message = "No hay productos con imagen en esta categoría para exportar." });
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
                        .PaddingHorizontal(40)
                        .PaddingVertical(15) // Margen ajustado para dar máximo espacio vertical a la imagen
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
                                        
                                    catCol.Item().PaddingTop(10).Text("CATÁLOGO DE PRODUCTOS")
                                        .FontSize(16)
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
                                        .FontSize(30)
                                        .FontColor("#0F172A")
                                        .AlignCenter();
                                        
                                    // 2. Detalles (Centrado, SKU y U/E)
                                    var ueText = product.Description?.Contains("U/E: ") == true
                                        ? product.Description.Split("U/E: ").LastOrDefault()?.Trim(')')
                                        : "N/A";
                                        
                                    column.Item().AlignCenter().PaddingTop(4).Text(x =>
                                    {
                                        x.Span("CÓDIGO SKU: ").Bold().FontSize(16).FontColor("#E11D48");
                                        x.Span($"{product.InternalCode}     •     ").FontSize(16).FontColor("#334155");
                                        
                                        x.Span("U/E: ").Bold().FontSize(16).FontColor("#E11D48");
                                        x.Span($"{ueText}").FontSize(16).FontColor("#334155");
                                    });

                                    column.Item().PaddingVertical(4).LineHorizontal(1f).LineColor("#F1F5F9");

                                    // 3. Imagen del Producto
                                    var imgPlaced = false;
                                    if (!string.IsNullOrWhiteSpace(product.ImagePath))
                                    {
                                        try
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
                                                    .MaxHeight(430)
                                                    .Image(transparentImageBytes, ImageScaling.FitArea);
                                                imgPlaced = true;
                                            }
                                        }
                                        catch { }
                                    }

                                    // 4. Description (Centered)
                                    if (!string.IsNullOrWhiteSpace(product.Description))
                                    {
                                        column.Item().PaddingTop(5).AlignCenter().Text(product.Description)
                                            .FontSize(12)
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

    private static byte[] MakeBackgroundTransparent(string imagePath)
    {
        try
        {
            using var original = SkiaSharp.SKBitmap.Decode(imagePath);
            if (original == null) return System.IO.File.ReadAllBytes(imagePath);

            int width = original.Width;
            int height = original.Height;

            using var resultBitmap = new SkiaSharp.SKBitmap(width, height, SkiaSharp.SKColorType.Rgba8888, SkiaSharp.SKAlphaType.Premul);

            using (var canvas = new SkiaSharp.SKCanvas(resultBitmap))
            {
                canvas.Clear(SkiaSharp.SKColors.Transparent);
                canvas.DrawBitmap(original, 0, 0);
            }

            // Algoritmo de Flood-Fill desde el perímetro para eliminar fondos claros, oscuros y neutros
            bool[,] visited = new bool[width, height];
            var queue = new Queue<SkiaSharp.SKPointI>();

            // Agregar todo el perímetro exterior a la cola de Flood-Fill
            for (int x = 0; x < width; x++)
            {
                queue.Enqueue(new SkiaSharp.SKPointI(x, 0));
                queue.Enqueue(new SkiaSharp.SKPointI(x, height - 1));
            }
            for (int y = 0; y < height; y++)
            {
                queue.Enqueue(new SkiaSharp.SKPointI(0, y));
                queue.Enqueue(new SkiaSharp.SKPointI(width - 1, y));
            }

            // Criterio agresivo de detección de fondo:
            // a) Fondos Blancos/Claros/Cremas (R >= 195 && G >= 195 && B >= 195)
            // b) Fondos Negros/Oscuros (R <= 50 && G <= 50 && B <= 50)
            // c) Fondos neutros monocromáticos (diferencia max de canales <= 25 && (R >= 170 || R <= 60))
            static bool IsBackgroundColor(SkiaSharp.SKColor c)
            {
                bool isWhiteOrLight = c.Red >= 195 && c.Green >= 195 && c.Blue >= 195;
                bool isBlackOrDark = c.Red <= 50 && c.Green <= 50 && c.Blue <= 50;
                int maxDiff = Math.Max(Math.Abs(c.Red - c.Green), Math.Max(Math.Abs(c.Green - c.Blue), Math.Abs(c.Red - c.Blue)));
                bool isMonochromeNeutral = maxDiff <= 25 && (c.Red >= 170 || c.Red <= 60);

                return isWhiteOrLight || isBlackOrDark || isMonochromeNeutral;
            }

            while (queue.Count > 0)
            {
                var p = queue.Dequeue();
                int px = p.X;
                int py = p.Y;

                if (px < 0 || px >= width || py < 0 || py >= height) continue;
                if (visited[px, py]) continue;

                visited[px, py] = true;

                var pixelColor = resultBitmap.GetPixel(px, py);

                if (IsBackgroundColor(pixelColor))
                {
                    resultBitmap.SetPixel(px, py, SkiaSharp.SKColors.Transparent);

                    // Expandir a píxeles vecinos (4 direcciones)
                    if (px > 0 && !visited[px - 1, py]) queue.Enqueue(new SkiaSharp.SKPointI(px - 1, py));
                    if (px < width - 1 && !visited[px + 1, py]) queue.Enqueue(new SkiaSharp.SKPointI(px + 1, py));
                    if (py > 0 && !visited[px, py - 1]) queue.Enqueue(new SkiaSharp.SKPointI(px, py - 1));
                    if (py < height - 1 && !visited[px, py + 1]) queue.Enqueue(new SkiaSharp.SKPointI(px, py + 1));
                }
            }

            // Segunda pasada: Limpieza agresiva de remanentes de bordes externos (12% del margen perimetral)
            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    var c = resultBitmap.GetPixel(x, y);
                    if (c.Alpha > 0)
                    {
                        bool isLightBorder = c.Red >= 210 && c.Green >= 210 && c.Blue >= 210;
                        bool isDarkBorder = c.Red <= 40 && c.Green <= 40 && c.Blue <= 40;
                        if (isLightBorder || isDarkBorder)
                        {
                            if (x < width * 0.12 || x > width * 0.88 || y < height * 0.12 || y > height * 0.88)
                            {
                                resultBitmap.SetPixel(x, y, SkiaSharp.SKColors.Transparent);
                            }
                        }
                    }
                }
            }

            using var image = SkiaSharp.SKImage.FromBitmap(resultBitmap);
            using var data = image.Encode(SkiaSharp.SKEncodedImageFormat.Png, 100);
            return data.ToArray();
        }
        catch
        {
            return System.IO.File.ReadAllBytes(imagePath);
        }
    }

    private static bool HasValidImage(ProductDto p, IWebHostEnvironment env)
    {
        if (string.IsNullOrWhiteSpace(p.ImagePath)) return false;
        if (p.ImagePath.Contains("default-product.png", StringComparison.OrdinalIgnoreCase)) return false;

        var relativePath = p.ImagePath;
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
        return System.IO.File.Exists(localImagePath);
    }
}
