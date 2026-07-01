using System.Collections.Generic;
using EnterpriseBillingSystem.Domain.Common;

namespace EnterpriseBillingSystem.Domain.Entities;

public class CustomerCategory : GlobalAuditableEntity
{
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public decimal DefaultDiscountPercentage { get; set; }

    // Navigation property
    public virtual ICollection<Customer> Customers { get; set; } = new List<Customer>();
}
