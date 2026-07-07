using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using EnterpriseBillingSystem.Domain.Entities;
using EnterpriseBillingSystem.Domain.Enums;
using EnterpriseBillingSystem.Domain.Repositories;
using EnterpriseBillingSystem.Infrastructure.Data;

namespace EnterpriseBillingSystem.Infrastructure.Repositories;

public class SalesOrderRepository : Repository<SalesOrder>, ISalesOrderRepository
{
    public SalesOrderRepository(ApplicationDbContext context) : base(context)
    {
    }

    public async Task<SalesOrder?> GetByIdWithDetailsAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _context.SalesOrders
            .Include(so => so.Customer)
            .Include(so => so.Details)
                .ThenInclude(d => d.Product)
            .Include(so => so.Details)
                .ThenInclude(d => d.UnitOfMeasure)
            .FirstOrDefaultAsync(so => so.Id == id, cancellationToken);
    }

    public async Task<SalesOrder?> GetByOrderNumberAsync(string orderNumber, CancellationToken cancellationToken = default)
    {
        return await _context.SalesOrders
            .Include(so => so.Customer)
            .FirstOrDefaultAsync(so => so.OrderNumber == orderNumber, cancellationToken);
    }

    public async Task<string> GenerateOrderNumberAsync(CancellationToken cancellationToken = default)
    {
        // Formato: SO-yyyyMMdd-NNNNN (contador diario)
        var today = DateTime.UtcNow.Date;
        var prefix = $"SO-{today:yyyyMMdd}-";

        var lastToday = await _context.SalesOrders
            .IgnoreQueryFilters()
            .Where(so => so.OrderNumber.StartsWith(prefix))
            .Select(so => so.OrderNumber)
            .OrderByDescending(n => n)
            .FirstOrDefaultAsync(cancellationToken);

        int nextSeq = 1;
        if (!string.IsNullOrEmpty(lastToday))
        {
            var seqPart = lastToday.Substring(prefix.Length);
            if (int.TryParse(seqPart, out int lastSeq))
                nextSeq = lastSeq + 1;
        }

        return $"{prefix}{nextSeq:D5}";
    }

    public async Task<(IEnumerable<SalesOrder> Items, int TotalCount)> GetPagedAsync(
        Guid? customerId,
        string? status,
        DateTime? fromDate,
        DateTime? toDate,
        int pageNumber,
        int pageSize,
        string? createdBy = null,
        Guid? routeId = null,
        CancellationToken cancellationToken = default)
    {
        var query = _context.SalesOrders
            .Include(so => so.Customer)
            .AsNoTracking();

        if (customerId.HasValue)
            query = query.Where(so => so.CustomerId == customerId.Value);

        if (routeId.HasValue)
            query = query.Where(so => so.Customer.RouteId == routeId.Value);

        if (!string.IsNullOrWhiteSpace(createdBy))
            query = query.Where(so => so.CreatedBy == createdBy);

        if (!string.IsNullOrWhiteSpace(status))
        {
            if (status.Equals("RecentReport", StringComparison.OrdinalIgnoreCase))
            {
                query = query.Where(so => so.Status != SalesOrderStatus.Recibido && so.Status != SalesOrderStatus.Anulado);
            }
            else if (Enum.TryParse<SalesOrderStatus>(status, true, out var statusEnum))
            {
                query = query.Where(so => so.Status == statusEnum);
            }
        }

        if (fromDate.HasValue)
            query = query.Where(so => so.OrderDate >= fromDate.Value);

        if (toDate.HasValue)
            query = query.Where(so => so.OrderDate <= toDate.Value);

        int totalCount = await query.CountAsync(cancellationToken);

        var items = await query
            .OrderByDescending(so => so.OrderDate)
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        return (items, totalCount);
    }

    public async Task<IEnumerable<SalesOrder>> GetFilteredWithDetailsAsync(
        Guid? customerId,
        string? status,
        DateTime? fromDate,
        DateTime? toDate,
        Guid? routeId = null,
        CancellationToken cancellationToken = default)
    {
        var query = _context.SalesOrders
            .Include(so => so.Customer)
            .Include(so => so.Details)
                .ThenInclude(d => d.Product)
            .Include(so => so.Details)
                .ThenInclude(d => d.UnitOfMeasure)
            .AsNoTracking();

        if (customerId.HasValue)
            query = query.Where(so => so.CustomerId == customerId.Value);

        if (routeId.HasValue)
            query = query.Where(so => so.Customer.RouteId == routeId.Value);

        if (!string.IsNullOrWhiteSpace(status))
        {
            if (status.Equals("RecentReport", StringComparison.OrdinalIgnoreCase))
            {
                query = query.Where(so => so.Status != SalesOrderStatus.Recibido && so.Status != SalesOrderStatus.Anulado);
            }
            else if (Enum.TryParse<SalesOrderStatus>(status, true, out var statusEnum))
            {
                query = query.Where(so => so.Status == statusEnum);
            }
        }

        if (fromDate.HasValue)
            query = query.Where(so => so.OrderDate >= fromDate.Value);

        if (toDate.HasValue)
            query = query.Where(so => so.OrderDate <= toDate.Value);

        return await query.ToListAsync(cancellationToken);
    }
}
