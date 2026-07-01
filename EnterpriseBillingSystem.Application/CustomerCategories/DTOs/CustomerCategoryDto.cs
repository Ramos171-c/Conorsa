using System;

namespace EnterpriseBillingSystem.Application.CustomerCategories.DTOs;

public record CustomerCategoryDto(
    Guid Id,
    string Name,
    string? Description,
    decimal DefaultDiscountPercentage
);
