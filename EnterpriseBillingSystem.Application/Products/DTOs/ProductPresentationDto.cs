using System;

namespace EnterpriseBillingSystem.Application.Products.DTOs;

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
