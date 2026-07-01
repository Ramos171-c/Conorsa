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

public class ProductPresentationRepository : Repository<ProductPresentation>, IProductPresentationRepository
{
    public ProductPresentationRepository(ApplicationDbContext context) : base(context)
    {
    }

    public async Task<ProductPresentation?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _context.ProductPresentations
            .Include(p => p.Product)
                .ThenInclude(prod => prod.Tax)
            .Include(p => p.UnitOfMeasure)
            .FirstOrDefaultAsync(p => p.Id == id, cancellationToken);
    }

    public async Task<ProductPresentation?> GetByBarcodeAsync(string barcode, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(barcode)) return null;
        return await _context.ProductPresentations
            .Include(p => p.Product)
                .ThenInclude(prod => prod.Tax)
            .Include(p => p.UnitOfMeasure)
            .FirstOrDefaultAsync(p => p.Barcode == barcode, cancellationToken);
    }

    public async Task<IEnumerable<ProductPresentation>> GetByProductIdAsync(Guid productId, CancellationToken cancellationToken = default)
    {
        return await _context.ProductPresentations
            .Include(p => p.UnitOfMeasure)
            .Where(p => p.ProductId == productId)
            .OrderBy(p => p.ConversionFactor)
            .ToListAsync(cancellationToken);
    }

    public async Task<ProductPresentation?> GetDefaultPresentationAsync(Guid productId, CancellationToken cancellationToken = default)
    {
        return await _context.ProductPresentations
            .Include(p => p.UnitOfMeasure)
            .FirstOrDefaultAsync(p => p.ProductId == productId && p.IsDefaultSalePresentation, cancellationToken);
    }
}
