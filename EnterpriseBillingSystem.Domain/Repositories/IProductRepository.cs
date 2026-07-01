using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using EnterpriseBillingSystem.Domain.Entities;

namespace EnterpriseBillingSystem.Domain.Repositories;

public interface IProductRepository : IRepository<Product>
{
    Task<Product?> GetByIdWithDetailsAsync(Guid id, CancellationToken cancellationToken = default);
    Task<(IEnumerable<Product> Items, int TotalCount)> GetPagedAsync(
        int pageNumber,
        int pageSize,
        string? searchTerm,
        Guid? categoryId,
        Guid? brandId,
        bool? isForPos = null,
        CancellationToken cancellationToken = default);
    Task<bool> ExistsSkuAsync(string sku, Guid? excludeId = null, CancellationToken cancellationToken = default);
    Task<bool> ExistsBarcodeAsync(string barcode, Guid? excludeId = null, CancellationToken cancellationToken = default);
    Task<IEnumerable<Product>> GetCatalogProductsAsync(CancellationToken cancellationToken = default);
    Task<IEnumerable<Product>> GetTopProductsForCustomerAsync(Guid customerId, int limit, CancellationToken cancellationToken = default);
}
