using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using EnterpriseBillingSystem.Domain.Entities;

namespace EnterpriseBillingSystem.Domain.Repositories;

public interface ISupplierRepository : IRepository<Supplier>
{
    Task<Supplier?> GetByCodeAsync(string supplierCode, CancellationToken cancellationToken = default);
    Task<Supplier?> GetByIdentificationAsync(string identificationNumber, CancellationToken cancellationToken = default);
    Task<bool> ExistsCodeAsync(string supplierCode, Guid? excludeId = null, CancellationToken cancellationToken = default);
    Task<string> GenerateSupplierCodeAsync(CancellationToken cancellationToken = default);
    Task<(IEnumerable<Supplier> Items, int TotalCount)> GetPagedAsync(
        string? search,
        Guid? categoryId,
        string? status,
        int pageNumber,
        int pageSize,
        CancellationToken cancellationToken = default);
}
