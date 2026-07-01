using System;

namespace EnterpriseBillingSystem.Application.Products.DTOs;

public record BranchProductDto(
    Guid BranchId,
    string BranchName,
    decimal? LocalSalePrice,
    decimal? MinSalePrice,
    decimal? MaxDiscountPercentage,
    bool IsActive
);
