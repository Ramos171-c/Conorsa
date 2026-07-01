using System;

namespace EnterpriseBillingSystem.Domain.Common;

public interface IAuditable : IGlobalAuditable
{
    Guid? BranchId { get; set; }
}
