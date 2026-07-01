using System;

namespace EnterpriseBillingSystem.Domain.Common;

public abstract class GlobalAuditableEntity : BaseEntity, IGlobalAuditable
{
    public string? CreatedBy { get; set; }
    public DateTime CreatedOnUtc { get; set; }
    public string? LastModifiedBy { get; set; }
    public DateTime? LastModifiedOnUtc { get; set; }
}
