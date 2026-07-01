using System;
using EnterpriseBillingSystem.Domain.Common;

namespace EnterpriseBillingSystem.Domain.Entities;

public class ProductPriceHistory : BaseEntity
{
    public Guid ProductId { get; set; }
    public Product Product { get; set; } = null!;

    public Guid? ProductPresentationId { get; set; }
    public ProductPresentation? ProductPresentation { get; set; }

    public decimal OldRetailPrice { get; set; }
    public decimal NewRetailPrice { get; set; }
    public decimal OldSemiWholesalePrice { get; set; }
    public decimal NewSemiWholesalePrice { get; set; }
    public decimal OldWholesalePrice { get; set; }
    public decimal NewWholesalePrice { get; set; }
    public decimal OldCost { get; set; }
    public decimal NewCost { get; set; }

    public string ChangedBy { get; set; } = string.Empty;
    public DateTime ChangedAt { get; set; }
    public string? Reason { get; set; }
}
