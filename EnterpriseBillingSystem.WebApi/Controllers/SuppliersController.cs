using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using EnterpriseBillingSystem.WebApi.Authorization;
using EnterpriseBillingSystem.Application.Purchases.Commands;
using EnterpriseBillingSystem.Application.Purchases.Queries;

namespace EnterpriseBillingSystem.WebApi.Controllers;

[Route("api/v1/[controller]")]
public class SuppliersController : ApiControllerBase
{
    /// <summary>
    /// Crear un nuevo proveedor.
    /// </summary>
    [HttpPost]
    [HasPermission("suppliers.create")]
    public async Task<ActionResult<Guid>> Create([FromBody] CreateSupplierCommand command)
    {
        var id = await Mediator.Send(command);
        return CreatedAtAction(nameof(GetById), new { id }, id);
    }

    /// <summary>
    /// Obtener proveedor por ID.
    /// </summary>
    [HttpGet("{id:guid}")]
    [HasPermission("suppliers.view")]
    public async Task<ActionResult<SupplierDetailDto>> GetById(Guid id)
    {
        var supplier = await Mediator.Send(new GetSupplierByIdQuery(id));
        if (supplier == null) return NotFound();
        return Ok(supplier);
    }

    /// <summary>
    /// Listar proveedores con paginación y filtros.
    /// </summary>
    [HttpGet]
    [HasPermission("suppliers.view")]
    public async Task<ActionResult<PagedResult<SupplierListItemDto>>> GetPaged(
        [FromQuery] string? search = null,
        [FromQuery] Guid? categoryId = null,
        [FromQuery] string? status = null,
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 20)
    {
        var result = await Mediator.Send(new GetSuppliersQuery(search, categoryId, status, pageNumber, pageSize));
        return Ok(result);
    }

    /// <summary>
    /// Obtener todas las categorías de proveedores.
    /// </summary>
    [HttpGet("categories")]
    [HasPermission("suppliers.view")]
    public async Task<ActionResult<System.Collections.Generic.IEnumerable<SupplierCategoryDto>>> GetCategories()
    {
        var result = await Mediator.Send(new GetSupplierCategoriesQuery());
        return Ok(result);
    }
}
