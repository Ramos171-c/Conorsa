using System.Collections.Generic;

namespace EnterpriseBillingSystem.Application.Common.Models;

public record PagedResult<T>(
    IEnumerable<T> Items,
    int TotalCount,
    int PageNumber,
    int PageSize
);
