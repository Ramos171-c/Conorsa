using System;
using EnterpriseBillingSystem.Domain.Common;
using EnterpriseBillingSystem.Domain.Enums;

namespace EnterpriseBillingSystem.Domain.Entities;

public class FixedAssetTransaction : AuditableEntity
{
    public Guid FixedAssetId { get; set; }
    public virtual FixedAsset FixedAsset { get; set; } = null!;

    public DateTime TransactionDate { get; set; }
    public FixedAssetTransactionType TransactionType { get; set; }

    /// <summary>Monto del movimiento (siempre positivo)</summary>
    public decimal Amount { get; set; }

    public Guid? JournalEntryId { get; set; }
    public virtual JournalEntry? JournalEntry { get; set; }

    public string? Notes { get; set; }
}
