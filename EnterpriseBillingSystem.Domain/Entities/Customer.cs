using System;
using System.Collections.Generic;
using EnterpriseBillingSystem.Domain.Common;
using EnterpriseBillingSystem.Domain.Enums;

namespace EnterpriseBillingSystem.Domain.Entities;

public class Customer : GlobalAuditableEntity
{
    public string CustomerCode { get; set; } = string.Empty;
    public string IdentificationNumber { get; set; } = string.Empty;
    public IdentificationType IdentificationType { get; set; }
    public CustomerType CustomerType { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? LegalName { get; set; }
    
    public Guid CustomerCategoryId { get; set; }
    public decimal CreditLimit { get; set; }
    public int CreditDays { get; set; }
    public bool CanUseCredit { get; set; }
    public bool IsTaxExempt { get; set; }
    public decimal DefaultDiscountPercentage { get; set; }
    public CustomerStatus Status { get; set; } = CustomerStatus.Active;

    public Guid CustomerPricingProfileId { get; set; }
    public Guid? RouteId { get; set; }

    // Navigation properties
    public virtual CustomerCategory CustomerCategory { get; set; } = null!;
    public virtual CustomerPricingProfile CustomerPricingProfile { get; set; } = null!;
    public virtual Route? Route { get; set; }
    public virtual ICollection<CustomerAddress> Addresses { get; set; } = new List<CustomerAddress>();
    public virtual ICollection<CustomerPhone> Phones { get; set; } = new List<CustomerPhone>();
    public virtual ICollection<CustomerEmail> Emails { get; set; } = new List<CustomerEmail>();
    public virtual ICollection<CustomerContact> Contacts { get; set; } = new List<CustomerContact>();
}
