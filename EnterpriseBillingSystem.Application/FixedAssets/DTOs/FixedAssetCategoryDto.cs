using System;

namespace EnterpriseBillingSystem.Application.FixedAssets.DTOs;

public record FixedAssetCategoryDto(
    Guid Id,
    string Code,
    string Name,
    string AssetAccountCode,
    string AccumulatedDepreciationAccountCode,
    string DepreciationExpenseAccountCode,
    int UsefulLifeMonths,
    bool IsActive
);
