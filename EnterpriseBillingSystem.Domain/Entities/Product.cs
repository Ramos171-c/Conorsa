using System;
using System.Collections.Generic;
using EnterpriseBillingSystem.Domain.Common;
using EnterpriseBillingSystem.Domain.Enums;

namespace EnterpriseBillingSystem.Domain.Entities;

public class Product : AuditableEntity
{
    public string InternalCode { get; set; } = string.Empty; // SKU
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public ProductType ProductType { get; set; } = ProductType.Physical;
    public ProductStatus ProductStatus { get; set; } = ProductStatus.Draft;
    public bool TrackInventory { get; set; } = true;
    public bool RequiresSerialNumber { get; set; } = false;
    public bool RequiresBatchControl { get; set; } = false;

    public Guid CategoryId { get; set; }
    public Category Category { get; set; } = null!;

    public Guid? BrandId { get; set; }
    public Brand? Brand { get; set; }

    public Guid DefaultUnitOfMeasureId { get; set; }
    public UnitOfMeasure DefaultUnitOfMeasure { get; set; } = null!;

    public decimal CurrentCost { get; set; }
    
    public string? ImagePath { get; set; }
    public bool IsCatalogVisible { get; set; } = true;
    public bool IsSoldOut { get; set; }
    public DateTime? SoldOutAt { get; set; }
    public string? SoldOutBy { get; set; }
    public decimal MinimumStock { get; set; }
    public bool IsFavorite { get; set; }
    public int FavoriteOrder { get; set; } = 0;

    public bool AllowPromotions { get; set; } = true;
    public bool HighlightInCatalog { get; set; } = false;
    public string? ShortDescription { get; set; }
    public string? CatalogBadge { get; set; }
    public int DisplayOrder { get; set; } = 0;
    public bool AutoMarkSoldOut { get; set; } = true;

    public bool IsActive { get; set; } = true;

    public Guid TaxId { get; set; }
    public Tax Tax { get; set; } = null!;

    public ICollection<ProductPresentation> Presentations { get; set; } = new List<ProductPresentation>();
    public ICollection<BranchProduct> BranchProducts { get; set; } = new List<BranchProduct>();
}
