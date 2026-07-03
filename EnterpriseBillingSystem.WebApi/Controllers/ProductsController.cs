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


    private string GetAbsoluteUrl(string? relativePath)
    {
        var baseUri = $"{Request.Scheme}://{Request.Host}{Request.PathBase}";
        var path = string.IsNullOrWhiteSpace(relativePath)
            ? "/uploads/products/default-product.png"
            : relativePath;
        return $"{baseUri}{path}";
    }
}
