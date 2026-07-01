using System;
using System.Collections.Generic;
using EnterpriseBillingSystem.Domain.Common;

namespace EnterpriseBillingSystem.Domain.Entities;

public class ProductPresentation : BaseEntity
{
    public Guid ProductId { get; set; }
    public Product Product { get; set; } = null!;

    public Guid UnitOfMeasureId { get; set; }
    public UnitOfMeasure UnitOfMeasure { get; set; } = null!;

    public string Name { get; set; } = string.Empty;
    public decimal ConversionFactor { get; set; }
    public string? Barcode { get; set; }
    public decimal Cost { get; set; }
    public decimal RetailPrice { get; set; }
    public decimal SemiWholesalePrice { get; set; }
    public decimal WholesalePrice { get; set; }
    public bool IsBaseUnit { get; set; }
    public bool IsDefaultSalePresentation { get; set; }
    public bool AllowPurchase { get; set; } = true;
    public bool AllowSale { get; set; } = true;
    public bool IsActive { get; set; } = true;
}
