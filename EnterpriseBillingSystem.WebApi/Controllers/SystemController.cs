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

    [HttpPost("process-july-ruta3-liquidation")]
    [Microsoft.AspNetCore.Authorization.AllowAnonymous]
    public async Task<IActionResult> ProcessJulyRuta3Liquidation(
        [FromServices] EnterpriseBillingSystem.Domain.Repositories.ISalesOrderRepository salesOrderRepository,
        [FromServices] EnterpriseBillingSystem.Domain.Repositories.IRepository<EnterpriseBillingSystem.Domain.Entities.Route> routeRepository,
        [FromServices] EnterpriseBillingSystem.Domain.Repositories.IProductRepository productRepository,
        [FromServices] EnterpriseBillingSystem.Domain.Repositories.IInventoryRepository inventoryRepository,
        [FromServices] EnterpriseBillingSystem.Domain.Repositories.IWarehouseRepository warehouseRepository,
        [FromServices] EnterpriseBillingSystem.Domain.Repositories.IRouteLiquidationRepository liquidationRepository,
        [FromServices] EnterpriseBillingSystem.Domain.Repositories.IUnitOfWork unitOfWork)
    {
        try
        {
            var routes = await routeRepository.GetAllAsync();
            var ruta3 = routes.FirstOrDefault(r => r.Code == "R04" || r.Name.Contains("Ruta 3") || r.Name.Contains("Ruta03")) ?? routes.FirstOrDefault();
            if (ruta3 == null) return BadRequest("No se encontró la Ruta 3.");

            var fromDate = new DateTime(2026, 7, 20, 0, 0, 0, DateTimeKind.Utc);
            var toDate = new DateTime(2026, 7, 26, 23, 59, 59, DateTimeKind.Utc);

            var orders = (await salesOrderRepository.GetFilteredWithDetailsAsync(
                customerId: null,
                status: null,
                fromDate: fromDate,
                toDate: toDate,
                routeId: ruta3.Id)).ToList();

            // Lista de 11 productos devueltos en RC-00000010 (Total 24 piezas)
            var returnedItemsInput = new Dictionary<string, decimal>
            {
                { "CA036", 1m },
                { "GA016", 1m },
                { "MA007", 1m },
                { "CA021", 1m },
                { "MA002", 1m },
                { "GA008", 1m },
                { "GA018", 7m },
                { "CA038", 2m },
                { "TA003", 3m },
                { "GA007", 1m },
                { "TA001", 5m }
            };

            var allProducts = await productRepository.GetAllAsync();

            var liquidationNumber = await liquidationRepository.GenerateNextLiquidationNumberAsync();
            var liquidation = new EnterpriseBillingSystem.Domain.Entities.RouteLiquidation
            {
                Id = Guid.NewGuid(),
                LiquidationNumber = liquidationNumber,
                RouteId = ruta3.Id,
                LiquidationDate = new DateTime(2026, 7, 26, 18, 0, 0, DateTimeKind.Utc),
                Status = EnterpriseBillingSystem.Domain.Enums.RouteLiquidationStatus.Confirmada,
                Observations = "Devolución Ruta 3 - Deysi (Semana 20 al 26 de Julio). Recepción RC-00000010 (24 piezas devueltas).",
                CreatedBy = "System",
                CreatedOnUtc = DateTime.UtcNow
            };

            var allWarehouses = await warehouseRepository.GetAllAsync();
            var targetWarehouse = allWarehouses.FirstOrDefault(w => w.Name.Contains("Exhibici")) ?? allWarehouses.FirstOrDefault();
            var branchWarehouseId = targetWarehouse?.BranchWarehouses?.FirstOrDefault()?.Id;

            int updatedOrdersCount = 0;
            decimal totalAmountSubtracted = 0;

            foreach (var kvp in returnedItemsInput)
            {
                var sku = kvp.Key;
                var qtyReturned = kvp.Value;
                var product = allProducts.FirstOrDefault(p => (p.InternalCode ?? "").Equals(sku, StringComparison.OrdinalIgnoreCase));
                if (product == null) continue;

                decimal remainingToDeduct = qtyReturned;

                // Reingresar stock a bodega
                if (branchWarehouseId.HasValue)
                {
                    var invRecord = await inventoryRepository.GetByWarehouseAndProductAsync(branchWarehouseId.Value, product.Id);
                    if (invRecord != null)
                    {
                        invRecord.PhysicalStock += qtyReturned;
                        invRecord.LastModifiedBy = "LiquidationSystem";
                        invRecord.LastModifiedOnUtc = DateTime.UtcNow;
                    }
                }

                // Deducir de pedidos
                foreach (var order in orders.Where(o => o.Status != EnterpriseBillingSystem.Domain.Enums.SalesOrderStatus.Anulado))
                {
                    if (remainingToDeduct <= 0) break;
                    var detail = order.Details.FirstOrDefault(d => d.ProductId == product.Id && d.Quantity > 0);
                    if (detail != null)
                    {
                        decimal deduct = Math.Min(remainingToDeduct, detail.Quantity);
                        detail.Quantity -= deduct;
                        remainingToDeduct -= deduct;

                        var lineDiscount = detail.Quantity * detail.UnitPrice * (detail.DiscountPercentage / 100m);
                        var lineBase = detail.Quantity * detail.UnitPrice - lineDiscount;
                        var lineTax = lineBase * (detail.TaxPercentage / 100m);
                        detail.DiscountAmount = lineDiscount;
                        detail.TaxAmount = lineTax;
                        detail.NetAmount = lineBase + lineTax;

                        order.SubTotal = order.Details.Sum(d => d.Quantity * d.UnitPrice);
                        order.DiscountAmount = order.Details.Sum(d => d.DiscountAmount);
                        order.TaxAmount = order.Details.Sum(d => d.TaxAmount);
                        order.TotalAmount = order.SubTotal - order.DiscountAmount + order.TaxAmount;

                        totalAmountSubtracted += deduct * detail.UnitPrice;
                        var obsText = $" [Devolución RC-00000010]: Devueltas {deduct} unds de {product.Name}. Monto restado.";
                        order.Notes = $"{order.Notes}\n{obsText}".Trim();
                        salesOrderRepository.Update(order);
                    }
                }

                liquidation.Details.Add(new EnterpriseBillingSystem.Domain.Entities.RouteLiquidationDetail
                {
                    Id = Guid.NewGuid(),
                    RouteLiquidationId = liquidation.Id,
                    ProductId = product.Id,
                    UnitOfMeasureId = product.DefaultUnitOfMeasureId,
                    QuantitySent = qtyReturned,
                    QuantityReturned = qtyReturned,
                    QuantitySold = 0,
                    BaseQuantitySent = qtyReturned,
                    BaseQuantityReturned = qtyReturned,
                    BaseQuantitySold = 0,
                    SalePrice = product.CurrentCost,
                    Cost = product.CurrentCost,
                    SubtotalSold = 0,
                    SubtotalReturned = qtyReturned * product.CurrentCost,
                    Notes = $"Devolución de 24 pzas RC-00000010 - Ruta 3 Deysi"
                });
            }

            // Marcar todos los pedidos de esta ruta del 20 al 26 de Julio como Completados
            foreach (var order in orders)
            {
                if (order.Status != EnterpriseBillingSystem.Domain.Enums.SalesOrderStatus.Anulado)
                {
                    order.Status = EnterpriseBillingSystem.Domain.Enums.SalesOrderStatus.Completado;
                    order.LastModifiedBy = "LiquidationSystem";
                    order.LastModifiedOnUtc = DateTime.UtcNow;
                    salesOrderRepository.Update(order);
                    updatedOrdersCount++;
                }
            }

            liquidation.TotalQuantityReturned = 24;
            liquidation.TotalAmountReturned = totalAmountSubtracted;
            await liquidationRepository.AddAsync(liquidation);

            await unitOfWork.SaveChangesAsync();

            return Ok(new
            {
                Message = $"Liquidación y devolución completada con éxito para Ruta 3 (Deysi) del 20 al 26 de Julio.",
                LiquidationNumber = liquidationNumber,
                TotalReturnedPieces = 24,
                TotalOrdersUpdatedToCompleted = updatedOrdersCount,
                TotalSalesSubtracted = totalAmountSubtracted
            });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { Message = $"Error al procesar liquidación: {ex.Message}" });
        }
    }
}
