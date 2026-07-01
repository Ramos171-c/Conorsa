using System;

namespace EnterpriseBillingSystem.Domain.Common;

public abstract class AuditableEntity : BaseEntity, IAuditable
{
    public string? CreatedBy { get; set; }
    public DateTime CreatedOnUtc { get; set; }
    public string? LastModifiedBy { get; set; }
    public DateTime? LastModifiedOnUtc { get; set; }
    
    // Soporte para multi-sucursal / multi-tenant
    public Guid? BranchId { get; set; }
}
