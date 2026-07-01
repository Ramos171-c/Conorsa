using System;

namespace EnterpriseBillingSystem.Wpf.Models;

public enum CustomerPricingType
{
    Retail = 0,
    SemiWholesale = 1,
    Wholesale = 2
}

public class CustomerSearchResultDto
{
    public Guid Id { get; set; }
    public string CustomerCode { get; set; } = string.Empty;
    public string IdentificationNumber { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public bool CanUseCredit { get; set; }
    public decimal CreditLimit { get; set; }
    public int CreditDays { get; set; }
    public decimal DefaultDiscountPercentage { get; set; }
    public bool IsTaxExempt { get; set; }
    public Guid CustomerPricingProfileId { get; set; }
    public string CustomerPricingProfileName { get; set; } = string.Empty;
    public CustomerPricingType CustomerPricingProfileType { get; set; }
    public CustomerType CustomerType { get; set; }
}

public record CustomerPricingProfileDto(
    Guid Id,
    string Name,
    CustomerPricingType Type,
    bool IsActive
);
