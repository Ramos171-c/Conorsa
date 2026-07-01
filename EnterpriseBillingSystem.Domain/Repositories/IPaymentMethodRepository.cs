using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using EnterpriseBillingSystem.Domain.Entities;

namespace EnterpriseBillingSystem.Domain.Repositories;

public interface IPaymentMethodRepository : IRepository<PaymentMethod>
{
    Task<PaymentMethod?> GetByCodeAsync(string code, CancellationToken cancellationToken = default);
    Task<(IEnumerable<PaymentMethod> Items, int TotalCount)> GetPagedAsync(
        string? searchTerm,
        int pageNumber,
        int pageSize,
        CancellationToken cancellationToken = default);
}
