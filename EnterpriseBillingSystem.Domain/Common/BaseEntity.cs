using System;

namespace EnterpriseBillingSystem.Domain.Common;

public abstract class BaseEntity : ISoftDelete
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public bool IsDeleted { get; set; }
}
