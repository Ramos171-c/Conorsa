using System;
using EnterpriseBillingSystem.Domain.Enums;

namespace EnterpriseBillingSystem.Application.FixedAssets.DTOs;

public record FixedAssetTransactionDto(
    Guid Id,
    Guid FixedAssetId,
    string AssetNumber,
    string AssetName,
    DateTime TransactionDate,
    FixedAssetTransactionType TransactionType,
    string TransactionTypeName,
    decimal Amount,
    Guid? JournalEntryId,
    string? Notes
);
