using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using EnterpriseBillingSystem.Domain.Entities;
using EnterpriseBillingSystem.Domain.Repositories;
using EnterpriseBillingSystem.Infrastructure.Data;

namespace EnterpriseBillingSystem.Infrastructure.Repositories;

public class InventoryMovementRepository : Repository<InventoryMovement>, IInventoryMovementRepository
{
    public InventoryMovementRepository(ApplicationDbContext context) : base(context)
    {
    }

    public async Task<InventoryMovement?> GetByIdWithDetailsAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _context.InventoryMovements
            .Include(m => m.Details)
                .ThenInclude(d => d.Product)
            .Include(m => m.Details)
                .ThenInclude(d => d.UnitOfMeasure)
            .Include(m => m.FromBranchWarehouse!)
                .ThenInclude(bw => bw.Warehouse)
            .Include(m => m.ToBranchWarehouse!)
                .ThenInclude(bw => bw.Warehouse)
            .FirstOrDefaultAsync(m => m.Id == id, cancellationToken);
    }

    public async Task<(IEnumerable<InventoryMovementDetail> Items, int TotalCount)> GetKardexAsync(
        Guid branchWarehouseId,
        Guid productId,
        DateTime? startDate,
        DateTime? endDate,
        int pageNumber,
        int pageSize,
        CancellationToken cancellationToken = default)
    {
        var query = _context.InventoryMovementDetails
            .Include(d => d.InventoryMovement)
            .Include(d => d.UnitOfMeasure)
            .Include(d => d.Product)
            .Where(d => d.ProductId == productId && 
                        (d.InventoryMovement.FromBranchWarehouseId == branchWarehouseId || 
                         d.InventoryMovement.ToBranchWarehouseId == branchWarehouseId))
            .AsNoTracking();

        if (startDate.HasValue)
        {
            query = query.Where(d => d.InventoryMovement.MovementDate >= startDate.Value);
        }

        if (endDate.HasValue)
        {
            query = query.Where(d => d.InventoryMovement.MovementDate <= endDate.Value);
        }

        int totalCount = await query.CountAsync(cancellationToken);

        var items = await query
            .OrderByDescending(d => d.InventoryMovement.MovementDate)
            .ThenByDescending(d => d.InventoryMovement.CreatedOnUtc)
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        return (items, totalCount);
    }

    public async Task<string> GenerateMovementNumberAsync(CancellationToken cancellationToken = default)
    {
        var prefix = $"MOV-{DateTime.UtcNow:yyyyMMdd}";
        var lastMovement = await _context.InventoryMovements
            .Where(m => m.MovementNumber.StartsWith(prefix))
            .OrderByDescending(m => m.MovementNumber)
            .FirstOrDefaultAsync(cancellationToken);

        int nextSequence = 1;
        if (lastMovement != null && lastMovement.MovementNumber.Length > prefix.Length + 1)
        {
            var parts = lastMovement.MovementNumber.Split('-');
            if (parts.Length == 3 && int.TryParse(parts[2], out int seq))
            {
                nextSequence = seq + 1;
            }
        }

        return $"{prefix}-{nextSequence:D5}";
    }
}
