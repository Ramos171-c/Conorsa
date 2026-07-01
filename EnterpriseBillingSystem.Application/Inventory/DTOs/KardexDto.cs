using System;
using EnterpriseBillingSystem.Domain.Enums;

namespace EnterpriseBillingSystem.Application.Inventory.DTOs;

public record KardexDto(
    Guid DetailId,
    Guid MovementId,
    string MovementNumber,
    MovementType MovementType,
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
