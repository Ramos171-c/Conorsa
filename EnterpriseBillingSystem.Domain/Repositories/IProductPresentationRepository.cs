using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using EnterpriseBillingSystem.Domain.Entities;

namespace EnterpriseBillingSystem.Domain.Repositories;

public interface IProductPresentationRepository : IRepository<ProductPresentation>
{
    Task<ProductPresentation?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<ProductPresentation?> GetByBarcodeAsync(string barcode, CancellationToken cancellationToken = default);
    Task<IEnumerable<ProductPresentation>> GetByProductIdAsync(Guid productId, CancellationToken cancellationToken = default);
    Task<ProductPresentation?> GetDefaultPresentationAsync(Guid productId, CancellationToken cancellationToken = default);
}
