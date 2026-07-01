using System;

namespace EnterpriseBillingSystem.Application.Taxes.DTOs;

public record TaxDto(
    Guid Id,
    string Name,
    decimal Rate,
    bool IsActive
);
