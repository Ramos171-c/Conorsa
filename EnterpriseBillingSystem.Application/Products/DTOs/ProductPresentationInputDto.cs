using System;

namespace EnterpriseBillingSystem.Application.Products.DTOs;

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
