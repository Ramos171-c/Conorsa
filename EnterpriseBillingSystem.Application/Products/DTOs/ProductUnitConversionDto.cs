using System;

namespace EnterpriseBillingSystem.Application.Products.DTOs;

public record ProductUnitConversionDto(
    Guid FromUnitId,
    string FromUnitCode,
    Guid ToUnitId,
    string ToUnitCode,
    decimal ConversionFactor
);
