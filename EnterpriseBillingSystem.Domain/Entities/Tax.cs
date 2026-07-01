using System.Collections.Generic;
using EnterpriseBillingSystem.Domain.Common;

namespace EnterpriseBillingSystem.Domain.Entities;

public class Tax : AuditableEntity
{
    public string Name { get; set; } = string.Empty;
    public decimal Rate { get; set; }
    public bool IsActive { get; set; } = true;
    public ICollection<Product> Products { get; set; } = new List<Product>();
}
