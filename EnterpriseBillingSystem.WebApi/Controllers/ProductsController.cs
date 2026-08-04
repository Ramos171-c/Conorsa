using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http;
using EnterpriseBillingSystem.WebApi.Authorization;
using EnterpriseBillingSystem.Application.Products.Commands;
using EnterpriseBillingSystem.Application.Products.Queries;
using EnterpriseBillingSystem.Application.Products.DTOs;
using EnterpriseBillingSystem.Application.Common.Models;

namespace EnterpriseBillingSystem.WebApi.Controllers;

[Route("api/v1/[controller]")]
public class ProductsController : ApiControllerBase
{
    [HttpPost]
    [HasPermission("products.create")]
    public async Task<ActionResult<Guid>> Create([FromBody] CreateProductCommand command)
    {
        var id = await Mediator.Send(command);
        return CreatedAtAction(nameof(GetById), new { id }, id);
    }

    [HttpPut("{id}")]
    [HasPermission("products.edit")]
    public async Task<ActionResult> Update(Guid id, [FromBody] UpdateProductCommand command)
    {
        if (id != command.Id)
        {
            return BadRequest(new { Message = "El Id en el cuerpo no coincide con el de la ruta." });
        }

        var result = await Mediator.Send(command);
        if (!result) return NotFound();

        return NoContent();
    }

    [HttpDelete("{id}")]
    [HasPermission("products.delete")]
    public async Task<ActionResult> Delete(Guid id)
    {
        var result = await Mediator.Send(new DeleteProductCommand(id));
        if (!result) return NotFound();

        return NoContent();
    }

    [HttpGet("{id}")]
    [HasPermission("products.view")]
    public async Task<ActionResult<ProductDto>> GetById(Guid id)
    {
        var product = await Mediator.Send(new GetProductByIdQuery(id));
        if (product == null) return NotFound();

        return Ok(product with { ImagePath = GetAbsoluteUrl(product.ImagePath), ImageUrl = GetAbsoluteUrl(product.ImageUrl) });
    }

    [HttpGet]
    [HasPermission("products.view")]
    public async Task<ActionResult<PagedResult<ProductDto>>> GetPaged(
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 10,
        [FromQuery] string? searchTerm = null,
        [FromQuery] Guid? categoryId = null,
        [FromQuery] Guid? brandId = null,
        [FromQuery] bool? isForPos = null)
    {
        var result = await Mediator.Send(new GetProductsPagedQuery(pageNumber, pageSize, searchTerm, categoryId, brandId, isForPos));
        var mappedItems = result.Items.Select(item => item with { ImagePath = GetAbsoluteUrl(item.ImagePath), ImageUrl = GetAbsoluteUrl(item.ImageUrl) }).ToList();
        return Ok(new PagedResult<ProductDto>(mappedItems, result.TotalCount, result.PageNumber, result.PageSize));
    }

    [HttpPost("{id}/image")]
    [HasPermission("products.edit")]
    public async Task<ActionResult<string>> UploadImage(Guid id, IFormFile file)
    {
        if (file == null || file.Length == 0)
        {
            return BadRequest(new { Message = "No se ha proporcionado un archivo válido." });
        }

        using var ms = new MemoryStream();
        await file.CopyToAsync(ms);
        var bytes = ms.ToArray();

        var relativePath = await Mediator.Send(new UploadProductImageCommand(id, bytes, file.FileName));
        var absoluteUrl = GetAbsoluteUrl(relativePath);

        return Ok(new { ImageUrl = absoluteUrl });
    }

    [HttpDelete("{id}/image")]
    [HasPermission("products.edit")]
    public async Task<ActionResult> DeleteImage(Guid id)
    {
        var result = await Mediator.Send(new DeleteProductImageCommand(id));
        if (!result) return NotFound();

        return NoContent();
    }

    [HttpGet("low-stock")]
    [HasPermission("products.view")]
    public async Task<ActionResult<IEnumerable<LowStockProductDto>>> GetLowStock()
    {
        var result = await Mediator.Send(new GetLowStockProductsQuery());
        return Ok(result);
    }

    [HttpGet("{id}/price-history")]
    [HasPermission("products.view")]
    public async Task<ActionResult<IEnumerable<ProductPriceHistoryDto>>> GetPriceHistory(Guid id)
    {
        var result = await Mediator.Send(new GetProductPriceHistoryQuery(id));
        return Ok(result);
    }

    [HttpGet("{id}/presentations")]
    [HasPermission("products.view")]
    public async Task<ActionResult<IEnumerable<ProductPresentationDto>>> GetPresentations(Guid id)
    {
        var result = await Mediator.Send(new GetProductPresentationsQuery(id));
        return Ok(result);
    }

    [HttpPost("{id}/presentations")]
    [HasPermission("products.edit")]
    public async Task<ActionResult<Guid>> AddPresentation(Guid id, [FromBody] ProductPresentationInputDto input)
    {
        var presentationId = await Mediator.Send(new CreateProductPresentationCommand(id, input));
        return Ok(presentationId);
    }

    [HttpPut("presentations/{presentationId}")]
    [HasPermission("products.edit")]
    public async Task<ActionResult> UpdatePresentation(Guid presentationId, [FromBody] ProductPresentationInputDto input)
    {
        var result = await Mediator.Send(new UpdateProductPresentationCommand(presentationId, input));
        if (!result) return NotFound();
        return NoContent();
    }

    [HttpDelete("presentations/{presentationId}")]
    [HasPermission("products.edit")]
    public async Task<ActionResult> DeletePresentation(Guid presentationId)
    {
        var result = await Mediator.Send(new DeleteProductPresentationCommand(presentationId));
        if (!result) return NotFound();
        return NoContent();
    }

    [HttpGet("presentations/barcode/{barcode}")]
    [HasPermission("products.view")]
    public async Task<ActionResult<ProductPresentationDto>> GetPresentationByBarcode(string barcode)
    {
        var result = await Mediator.Send(new GetProductPresentationByBarcodeQuery(barcode));
        if (result == null) return NotFound();
        return Ok(result);
    }

    private byte[] CompressImage(byte[] imageBytes, string fileName)
    {
        try
        {
            var ext = Path.GetExtension(fileName).ToLower();
            if (ext == ".gif") return imageBytes; // Conservar GIFs animados intactos

            using var ms = new MemoryStream(imageBytes);
            using var codec = SkiaSharp.SKCodec.Create(ms);
            if (codec == null) return imageBytes;

            using var decodedBitmap = SkiaSharp.SKBitmap.Decode(codec);
            if (decodedBitmap == null) return imageBytes;

            // Corregir orientación EXIF automática para que las fotos tomadas en vertical u horizontal salgan en su posición correcta
            using var originalBitmap = AutoOrientBitmap(decodedBitmap, codec.EncodedOrigin);

            int maxDimension = 800; // Resolución óptima para dispositivos móviles
            int width = originalBitmap.Width;
            int height = originalBitmap.Height;

            if (width > maxDimension || height > maxDimension)
            {
                if (width > height)
                {
                    height = (int)(height * ((double)maxDimension / width));
                    width = maxDimension;
                }
                else
                {
                    width = (int)(width * ((double)maxDimension / height));
                    height = maxDimension;
                }

                using var resizedBitmap = originalBitmap.Resize(new SkiaSharp.SKImageInfo(width, height), new SkiaSharp.SKSamplingOptions(SkiaSharp.SKFilterMode.Linear));
                if (resizedBitmap == null) return imageBytes;

                using var image = SkiaSharp.SKImage.FromBitmap(resizedBitmap);
                using var data = image.Encode(SkiaSharp.SKEncodedImageFormat.Jpeg, 75); // 75% calidad Jpeg (ideal balance peso/nitidez)
                return data.ToArray();
            }
            else
            {
                using var image = SkiaSharp.SKImage.FromBitmap(originalBitmap);
                using var data = image.Encode(SkiaSharp.SKEncodedImageFormat.Jpeg, 75);
                return data.ToArray();
            }
        }
        catch
        {
            return imageBytes; // Si ocurre algún error, salvaguardar la imagen original
        }
    }

    private SkiaSharp.SKBitmap AutoOrientBitmap(SkiaSharp.SKBitmap bitmap, SkiaSharp.SKEncodedOrigin origin)
    {
        switch (origin)
        {
            case SkiaSharp.SKEncodedOrigin.RightTop: // 90 grados CW (Foto vertical de teléfono)
                return RotateBitmap(bitmap, 90);
            case SkiaSharp.SKEncodedOrigin.BottomRight: // 180 grados
                return RotateBitmap(bitmap, 180);
            case SkiaSharp.SKEncodedOrigin.LeftBottom: // 270 grados CW / 90 CCW
                return RotateBitmap(bitmap, 270);
            case SkiaSharp.SKEncodedOrigin.TopRight: // Flip Horizontal
                return RotateBitmap(bitmap, 0, flipHorizontal: true);
            case SkiaSharp.SKEncodedOrigin.BottomLeft: // Flip Vertical
                return RotateBitmap(bitmap, 180, flipHorizontal: true);
            case SkiaSharp.SKEncodedOrigin.LeftTop:
                return RotateBitmap(bitmap, 90, flipHorizontal: true);
            case SkiaSharp.SKEncodedOrigin.RightBottom:
                return RotateBitmap(bitmap, 270, flipHorizontal: true);
            default:
                return bitmap.Copy();
        }
    }

    private SkiaSharp.SKBitmap RotateBitmap(SkiaSharp.SKBitmap bitmap, int degrees, bool flipHorizontal = false)
    {
        bool swapDimensions = degrees == 90 || degrees == 270;
        int newWidth = swapDimensions ? bitmap.Height : bitmap.Width;
        int newHeight = swapDimensions ? bitmap.Width : bitmap.Height;

        var rotated = new SkiaSharp.SKBitmap(newWidth, newHeight);
        using var canvas = new SkiaSharp.SKCanvas(rotated);
        canvas.Clear(SkiaSharp.SKColors.Transparent);
        canvas.Translate(newWidth / 2f, newHeight / 2f);
        if (degrees != 0)
        {
            canvas.RotateDegrees(degrees);
        }
        if (flipHorizontal)
        {
            canvas.Scale(-1, 1);
        }
        canvas.Translate(-bitmap.Width / 2f, -bitmap.Height / 2f);
        canvas.DrawBitmap(bitmap, 0, 0);
        return rotated;
    }
    private string? GetAbsoluteUrl(string? relativePath)
    {
        if (string.IsNullOrWhiteSpace(relativePath) || relativePath.EndsWith("default-product.png", StringComparison.OrdinalIgnoreCase))
        {
            return null;
        }

        if (Uri.TryCreate(relativePath, UriKind.Absolute, out _))
        {
            return relativePath;
        }

        var baseUri = $"{Request.Scheme}://{Request.Host}{Request.PathBase}";
        var path = relativePath.StartsWith('/') ? relativePath : $"/{relativePath}";
        return $"{baseUri}{path}";
    }
}
