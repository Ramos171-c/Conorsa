using System;
using EnterpriseBillingSystem.Domain.Common;

namespace EnterpriseBillingSystem.Domain.Entities;

public class CustomerPhone : BaseEntity
{
    public Guid CustomerId { get; set; }
    public string PhoneNumber { get; set; } = string.Empty;
    public string PhoneType { get; set; } = "Celular";
    public bool IsDefault { get; set; }

    // Navigation property
    public virtual Customer Customer { get; set; } = null!;
}
