using System;
using EnterpriseBillingSystem.Domain.Enums;

namespace EnterpriseBillingSystem.Application.FixedAssets.DTOs;

public record FixedAssetDto(
    Guid Id,
    string AssetNumber,
    string Name,
    string? Description,
    Guid FixedAssetCategoryId,
    string CategoryName,
    string AssetAccountCode,
    Guid? PurchaseInvoiceId,
    DateTime AcquisitionDate,
    decimal AcquisitionCost,
    decimal ResidualValue,
    int UsefulLifeMonths,
    DateTime DepreciationStartDate,
    decimal AccumulatedDepreciation,
    decimal CurrentBookValue,
    FixedAssetStatus Status,
    string StatusName,
    string? Location,
    string? SerialNumber,
    string? Notes,
    DateTime? LastDepreciationDate,
    Guid? BranchId
);
