using Microsoft.AspNetCore.Mvc;
using EnterpriseBillingSystem.WebApi.Authorization;

namespace EnterpriseBillingSystem.WebApi.Controllers;

public class TestPermissionsController : ApiControllerBase
{
    [HttpGet("view-users")]
    [HasPermission("users.view")]
    public IActionResult ViewUsers()
    {
        return Ok(new { Message = "Acceso concedido a 'users.view'. Puede ver los usuarios." });
    }

    [HttpGet("create-users")]
    [HasPermission("users.create")]
    public IActionResult CreateUsers()
    {
        return Ok(new { Message = "Acceso concedido a 'users.create'. Puede crear usuarios." });
    }

    [HttpGet("adjust-inventory")]
    [HasPermission("inventory.adjust")]
    public IActionResult AdjustInventory()
    {
        return Ok(new { Message = "Acceso concedido a 'inventory.adjust'. Puede ajustar el inventario." });
    }
}
