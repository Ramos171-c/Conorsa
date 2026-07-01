using System;
using System.Collections.Generic;

namespace EnterpriseBillingSystem.Wpf.Models;

public record CategoryDto(
    Guid Id,
    string Name,
    string? Description,
    Guid? ParentCategoryId,
    bool IsActive
);

public record TaxDto(
    Guid Id,
    string Name,
    decimal Rate,
    bool IsActive
);

public record UnitOfMeasureDto(
    Guid Id,
    string Code,
    string Name,
    bool IsActive
);

public record BranchProductDto(
    Guid BranchId,
    string BranchName,
    decimal? LocalSalePrice,
    decimal? MinSalePrice,
    decimal? MaxDiscountPercentage,
    bool IsActive
);

public record ProductPresentationDto(
    Guid Id,
    Guid ProductId,
    string ProductName,
    string ProductInternalCode,
    decimal TaxPercentage,
    Guid UnitOfMeasureId,
    string UnitOfMeasureCode,
    string Name,
    decimal ConversionFactor,
    string? Barcode,
    decimal Cost,
    decimal RetailPrice,
    decimal SemiWholesalePrice,
    decimal WholesalePrice,
    bool IsBaseUnit,
    bool IsDefaultSalePresentation,
    bool AllowPurchase,
    bool AllowSale,
    bool IsActive
);

public record ProductPresentationInputDto(
    Guid? Id,
    Guid UnitOfMeasureId,
    string Name,
    decimal ConversionFactor,
    string? Barcode,
    decimal Cost,
    decimal RetailPrice,
    decimal SemiWholesalePrice,
    decimal WholesalePrice,
    bool IsBaseUnit,
    bool IsDefaultSalePresentation,
    bool AllowPurchase,
    bool AllowSale,
    bool IsActive
);

public record CustomerPricingTierDto(
    Guid Id,
    string Name,
    decimal MinimumAmount,
    decimal MaximumAmount,
    decimal DiscountPercentage,
    Guid? BranchId,
    bool IsActive
);

public record ProductDto(
    Guid Id,
    string InternalCode,
    string? Barcode,
    string Name,
    string? Description,
    int ProductType,
    int ProductStatus,
    bool TrackInventory,
    bool RequiresSerialNumber,
    bool RequiresBatchControl,
    Guid CategoryId,
    string CategoryName,
    Guid? BrandId,
    string? BrandName,
    Guid DefaultUnitOfMeasureId,
    string DefaultUnitOfMeasureCode,
    decimal DefaultPurchasePrice,
    decimal DefaultSalePrice,
    decimal CurrentCost,
    string? ImagePath,
    bool IsCatalogVisible,
    bool IsSoldOut,
    DateTime? SoldOutAt,
    string? SoldOutBy,
    decimal MinimumStock,
    bool IsFavorite,
    int FavoriteOrder,
    bool AllowPromotions,
    bool HighlightInCatalog,
    string? ShortDescription,
    string? CatalogBadge,
    int DisplayOrder,
    bool AutoMarkSoldOut,
    bool IsActive,
    ICollection<ProductPresentationDto> Presentations,
    ProductPresentationDto? DefaultPresentation,
    decimal DefaultPrice,
    string? ImageUrl,
    string Availability,
    ICollection<TaxDto> Taxes,
    ICollection<BranchProductDto> BranchProducts
);

public record WarehouseDto(
    Guid Id,
    string Code,
    string Name,
    string? Description,
    bool IsActive
);

public record InventoryDto(
    Guid Id,
    Guid BranchWarehouseId,
    string WarehouseCode,
    string WarehouseName,
    Guid ProductId,
    string ProductName,
    string ProductInternalCode,
    decimal PhysicalStock,
    decimal ReservedStock,
    decimal CommittedStock,
    decimal AvailableStock
);

public record KardexDto(
    Guid DetailId,
    Guid MovementId,
    string MovementNumber,
    int MovementType,
    string MovementTypeName,
    DateTime MovementDate,
    string? ReferenceDocument,
    string? Notes,
    decimal Quantity,
    string UnitOfMeasureCode,
    decimal ConversionFactor,
    decimal QuantityInBaseUnit,
    bool IsEntry
);

public record ProductPriceHistoryDto(
    Guid Id,
    Guid ProductId,
    Guid? ProductPresentationId,
    decimal OldRetailPrice,
    decimal NewRetailPrice,
    decimal OldSemiWholesalePrice,
    decimal NewSemiWholesalePrice,
    decimal OldWholesalePrice,
    decimal NewWholesalePrice,
    decimal OldCost,
    decimal NewCost,
    string ChangedBy,
    DateTime ChangedAt,
    string? Reason
);

public record LowStockProductDto(
    string Code,
    string Name,
    decimal CurrentStock,
    decimal MinimumStock
);

public record InventoryDashboardKpisDto(
    int TotalProducts,
    int ActiveProducts,
    int SoldOutProducts,
    int HiddenProducts,
    int FavoriteProducts,
    int LowStockProducts,
    decimal InventoryValue,
    int TodayAdjustments,
    int TodayTransfers
);
