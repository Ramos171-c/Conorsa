using System;
using EnterpriseBillingSystem.Domain.Common;

namespace EnterpriseBillingSystem.Domain.Entities;

public class CustomerEmail : BaseEntity
{
    public Guid CustomerId { get; set; }
    public string EmailAddress { get; set; } = string.Empty;
    public string EmailType { get; set; } = "Personal";
    public bool IsDefault { get; set; }

    // Navigation property
    public virtual Customer Customer { get; set; } = null!;
}
