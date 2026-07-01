using System;
using EnterpriseBillingSystem.Domain.Common;
using EnterpriseBillingSystem.Domain.Enums;

namespace EnterpriseBillingSystem.Domain.Entities;

public class CustomerPricingProfile : BaseEntity
{
    public string Name { get; set; } = string.Empty;
    public CustomerPricingType Type { get; set; }
    public bool IsActive { get; set; } = true;
}
