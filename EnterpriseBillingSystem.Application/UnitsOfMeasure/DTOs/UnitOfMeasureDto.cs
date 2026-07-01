using System;

namespace EnterpriseBillingSystem.Application.UnitsOfMeasure.DTOs;

public record UnitOfMeasureDto(
    Guid Id,
    string Code,
    string Name,
    bool IsActive
);
