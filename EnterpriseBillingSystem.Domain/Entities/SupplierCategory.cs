using System.Collections.Generic;
using EnterpriseBillingSystem.Domain.Common;

namespace EnterpriseBillingSystem.Domain.Entities;

public class SupplierCategory : GlobalAuditableEntity
{
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }

    // Navigation property
    public virtual ICollection<Supplier> Suppliers { get; set; } = new List<Supplier>();
}
