using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using EnterpriseBillingSystem.Application.Products.Queries;
using EnterpriseBillingSystem.Application.Products.DTOs;
using ShapeCrawler;

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

    [HttpGet("export/pptx")]
    public async Task<IActionResult> ExportToPptx([FromQuery] Guid? categoryId, [FromServices] IWebHostEnvironment env)
    {
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

        var tempFile = Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid()}.pptx");
        
        try
        {
            using (var pres = new Presentation())
            {
                bool first = true;
                foreach (var product in productsArray)
                {
                    IUserSlide slide;
                    if (pres.Slides.Count > 0 && first)
                    {
                        slide = pres.Slides[0];
                        first = false;
                    }
                    else
                    {
                        pres.Slides.Add(1);
                        slide = pres.Slides[pres.Slides.Count - 1];
                    }

                    var shapes = slide.Shapes;
                    
                    // 1. Title (Product Name)
                    shapes.AddTextBox(50, 30, 860, 60, product.Name.ToUpper());
                    var titleShape = shapes.Last();
                    titleShape.SetFontSize(24);
                    titleShape.SetFontColor("0F172A"); // Slate 900

                    // 2. Details (SKU, Category, U/E)
                    var ueText = product.Description?.Contains("U/E: ") == true
                        ? product.Description.Split("U/E: ").LastOrDefault()?.Trim(')')
                        : "N/A";
                    
                    var detailsText = 
                        $"CÓDIGO SKU: {product.InternalCode}\n" +
                        $"CATEGORÍA: {product.CategoryName}\n" +
                        $"UNIDAD DE EMPAQUE (U/E): {ueText}";

                    shapes.AddTextBox(50, 110, 420, 120, detailsText);
                    var detailsShape = shapes.Last();
                    detailsShape.SetFontSize(14);
                    detailsShape.SetFontColor("475569"); // Slate 600

                    // 3. Prices
                    var unitPres = product.Presentations.FirstOrDefault(pr => pr.IsBaseUnit || pr.ConversionFactor == 1.0000m);
                    var boxPres = product.Presentations.FirstOrDefault(pr => !pr.IsBaseUnit && pr.ConversionFactor > 1.0000m);

                    var pricesText = 
                        "PRECIOS:\n" +
                        $"• MAYORISTA:\n" +
                        $"  - Unidad: C${unitPres?.WholesalePrice.ToString("F2") ?? "0.00"}\n" +
                        $"  - Caja ({boxPres?.ConversionFactor.ToString("0") ?? "0"} uds): C${boxPres?.WholesalePrice.ToString("F2") ?? "0.00"}\n" +
                        $"• SEMIMAYORISTA:\n" +
                        $"  - Unidad: C${unitPres?.SemiWholesalePrice.ToString("F2") ?? "0.00"}\n" +
                        $"  - Caja ({boxPres?.ConversionFactor.ToString("0") ?? "0"} uds): C${boxPres?.SemiWholesalePrice.ToString("F2") ?? "0.00"}\n" +
                        $"• DETALLE:\n" +
                        $"  - Unidad: C${unitPres?.RetailPrice.ToString("F2") ?? "0.00"}\n" +
                        $"  - Caja ({boxPres?.ConversionFactor.ToString("0") ?? "0"} uds): C${boxPres?.RetailPrice.ToString("F2") ?? "0.00"}";

                    shapes.AddTextBox(50, 250, 420, 250, pricesText);
                    var pricesShape = shapes.Last();
                    pricesShape.SetFontSize(12);
                    pricesShape.SetFontColor("1E293B"); // Slate 800

                    // 4. Image
                    if (!string.IsNullOrWhiteSpace(product.ImagePath))
                    {
                        var rootPath = env.WebRootPath;
                        // Strip base URL to get relative path if absolute URL is present
                        var relativePath = product.ImagePath;
                        if (relativePath.StartsWith("http", StringComparison.OrdinalIgnoreCase))
                        {
                            var uri = new Uri(relativePath);
                            relativePath = uri.AbsolutePath;
                        }
                        
                        var localImagePath = Path.Combine(rootPath, relativePath.TrimStart('/'));
                        if (System.IO.File.Exists(localImagePath))
                        {
                            using (var imgStream = System.IO.File.OpenRead(localImagePath))
                            {
                                shapes.AddPicture(imgStream);
                                var pictureShape = shapes.Last();
                                pictureShape.Width = 410;
                                pictureShape.Height = 370;
                                
                                dynamic dynPicture = pictureShape;
                                dynPicture.X = 500;
                                dynPicture.Y = 110;
                            }
                        }
                    }
                }

                pres.Save(tempFile);
            }

            var bytes = await System.IO.File.ReadAllBytesAsync(tempFile);
            var categoryName = categoryId.HasValue && productsArray.Length > 0 ? productsArray[0].CategoryName : "Todos";
            var safeCategoryName = string.Join("_", categoryName.Split(Path.GetInvalidFileNameChars()));
            var fileName = $"catalogo_{safeCategoryName.ToLower().Replace(" ", "_")}.pptx";
            
            return File(bytes, "application/vnd.openxmlformats-officedocument.presentationml.presentation", fileName);
        }
        finally
        {
            if (System.IO.File.Exists(tempFile))
            {
                System.IO.File.Delete(tempFile);
            }
        }
    }
}



