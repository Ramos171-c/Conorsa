using System;
using EnterpriseBillingSystem.Domain.Common;

namespace EnterpriseBillingSystem.Domain.Entities;

public class PricingThreshold : BaseEntity
{
    public string LevelName { get; set; } = string.Empty;
    public decimal MinimumSubtotal { get; set; }
    public bool IsActive { get; set; } = true;
}
