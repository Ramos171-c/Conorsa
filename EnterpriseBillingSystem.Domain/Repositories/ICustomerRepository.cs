using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using EnterpriseBillingSystem.Domain.Entities;
using EnterpriseBillingSystem.Domain.Enums;

namespace EnterpriseBillingSystem.Domain.Repositories;

public interface ICustomerRepository : IRepository<Customer>
{
    Task<Customer?> GetByIdWithDetailsAsync(Guid id, CancellationToken cancellationToken = default);
    
    Task<(IEnumerable<Customer> Items, int TotalCount)> GetPagedAsync(
        int pageNumber,
        int pageSize,
        string? searchTerm,
        Guid? categoryId,
        CustomerStatus? status,
        CancellationToken cancellationToken = default);

    Task<bool> ExistsByIdentificationAsync(string identificationNumber, Guid? excludeId = null, CancellationToken cancellationToken = default);

    Task<string> GenerateCustomerCodeAsync(CancellationToken cancellationToken = default);
}
