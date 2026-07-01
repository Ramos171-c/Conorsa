using System;

namespace EnterpriseBillingSystem.Application.Brands.DTOs;

public record BrandDto(
    Guid Id,
    string Name,
    string? Description,
    bool IsActive
);
