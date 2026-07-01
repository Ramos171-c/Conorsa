using System;
using EnterpriseBillingSystem.Domain.Common;
using EnterpriseBillingSystem.Domain.Enums;

namespace EnterpriseBillingSystem.Domain.Entities;

public class BankTransaction : AuditableEntity
{
    public Guid BankAccountId { get; set; }
    public virtual BankAccount BankAccount { get; set; } = null!;

    public DateTime TransactionDate { get; set; }
    public BankTransactionType TransactionType { get; set; }
    public decimal Amount { get; set; }
    public string ReferenceNumber { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;

    public Guid? RelatedBankAccountId { get; set; }
    public virtual BankAccount? RelatedBankAccount { get; set; }

    public Guid? JournalEntryId { get; set; }
    public virtual JournalEntry? JournalEntry { get; set; }

    public byte[] RowVersion { get; set; } = Array.Empty<byte>();
}
