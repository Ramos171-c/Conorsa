using System;
using EnterpriseBillingSystem.Domain.Common;

namespace EnterpriseBillingSystem.Domain.Entities;

public class JournalEntryDetail : BaseEntity
{
    public Guid JournalEntryId { get; set; }
    public Guid AccountId { get; set; }
    public decimal DebitAmount { get; set; }
    public decimal CreditAmount { get; set; }
    public string? Description { get; set; }

    // Navigation properties
    public virtual JournalEntry JournalEntry { get; set; } = null!;
    public virtual Account Account { get; set; } = null!;
}
