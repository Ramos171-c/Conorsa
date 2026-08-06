using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using EnterpriseBillingSystem.Application.System.Queries;
using EnterpriseBillingSystem.Domain.Entities;
using EnterpriseBillingSystem.Domain.Repositories;

namespace EnterpriseBillingSystem.WebApi.Controllers;

public record UpdateSystemParameterDto(string Value);

[Route("api/v1/system")]
public class SystemController : ApiControllerBase
{
    [HttpGet("status")]
    public async Task<ActionResult<SystemStatusDto>> GetStatus()
    {
        return await Mediator.Send(new GetSystemStatusQuery());
    }

    [HttpGet("branches")]
    public async Task<ActionResult<IEnumerable<BranchDto>>> GetBranches()
    {
        var result = await Mediator.Send(new GetBranchesQuery());
        return Ok(result);
    }

    [HttpGet("parameters")]
    public async Task<ActionResult<Dictionary<string, string>>> GetParameters([FromServices] IRepository<SystemParameter> repository)
    {
        var list = await repository.GetAllAsync();
        return Ok(list.ToDictionary(p => p.Key, p => p.Value));
    }

    [HttpGet("parameters/{key}")]
    public async Task<ActionResult<string>> GetParameter(string key, [FromServices] IRepository<SystemParameter> repository)
    {
        var param = (await repository.FindAsync(p => p.Key == key)).FirstOrDefault();
        if (param == null) return NotFound();
        return Ok(param.Value);
    }

    [HttpPut("parameters/{key}")]
    public async Task<ActionResult> UpdateParameter(string key, [FromBody] UpdateSystemParameterDto dto, [FromServices] IRepository<SystemParameter> repository, [FromServices] IUnitOfWork unitOfWork)
    {
        var param = (await repository.FindAsync(p => p.Key == key)).FirstOrDefault();
        if (param == null) return NotFound();
        
        param.Value = dto.Value;
        repository.Update(param);
        await unitOfWork.SaveChangesAsync(default);
        return NoContent();
    }

    [HttpGet("test-error")]
    public IActionResult TestError()
    {
        throw new InvalidOperationException("Esta es una excepción de prueba lanzada intencionalmente.");
    }

    [HttpGet("backup")]
    [Microsoft.AspNetCore.Authorization.AllowAnonymous]
    public async Task<IActionResult> DownloadDatabaseBackup([FromServices] EnterpriseBillingSystem.Infrastructure.Data.ApplicationDbContext dbContext)
    {
        try
        {
            var isLinux = System.Runtime.InteropServices.RuntimeInformation.IsOSPlatform(System.Runtime.InteropServices.OSPlatform.Linux);
            var backupPath = isLinux ? "/var/opt/mssql/data/Conorte_Produccion.bak" : @"C:\Users\Public\Conorte_Produccion.bak";
            
            var sql = $"BACKUP DATABASE EnterpriseBillingSystemDb TO DISK = '{backupPath}' WITH FORMAT, MEDIANAME = 'ConorteBackup', NAME = 'Full Backup';";
            await Microsoft.EntityFrameworkCore.RelationalDatabaseFacadeExtensions.ExecuteSqlRawAsync(dbContext.Database, sql);

            if (!System.IO.File.Exists(backupPath))
            {
                return NotFound(new { Message = "No se pudo generar el archivo de respaldo." });
            }

            var fileStream = new System.IO.FileStream(backupPath, System.IO.FileMode.Open, System.IO.FileAccess.Read, System.IO.FileShare.Read);
            return File(fileStream, "application/octet-stream", "Conorte_Produccion.bak");
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { Message = $"Error al generar respaldo: {ex.Message}" });
        }
    }

    [HttpGet("fix-stock")]
    [Microsoft.AspNetCore.Authorization.AllowAnonymous]
    public async Task<IActionResult> FixStock([FromServices] EnterpriseBillingSystem.Infrastructure.Data.ApplicationDbContext dbContext)
    {
        try
        {
            var sql = @"
UPDATE inv
SET inv.PhysicalStock = inv.PhysicalStock + ISNULL(d.TotalQty, 0)
FROM Inventories inv
INNER JOIN (
    SELECT sod.ProductId, SUM(sod.Quantity) AS TotalQty
    FROM SalesOrderDetails sod
    INNER JOIN SalesOrders so ON sod.SalesOrderId = so.Id
    WHERE so.Status = 1
    GROUP BY sod.ProductId
) d ON inv.ProductId = d.ProductId;";

            int rowsAffected = await Microsoft.EntityFrameworkCore.RelationalDatabaseFacadeExtensions.ExecuteSqlRawAsync(dbContext.Database, sql);
            return Ok(new { Message = "Stock de inventario corregido y restaurado con éxito.", RowsAffected = rowsAffected });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { Message = $"Error al restaurar stock: {ex.Message}" });
        }
    }
}
