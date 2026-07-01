using System;
using System.Collections.Generic;
using EnterpriseBillingSystem.Domain.Common;
using EnterpriseBillingSystem.Domain.Enums;

namespace EnterpriseBillingSystem.Domain.Entities;

public class JournalEntry : AuditableEntity
{
    public string EntryNumber { get; set; } = string.Empty;
    public DateTime EntryDate { get; set; }
    public string Description { get; set; } = string.Empty;
    public JournalEntryStatus Status { get; set; } = JournalEntryStatus.Draft;
    public string? ReferenceDocument { get; set; }
    public Guid? ReferenceId { get; set; }
    public string SourceModule { get; set; } = string.Empty;
    public string? PostedByUserId { get; set; }
    public DateTime? PostedAt { get; set; }
    public byte[] RowVersion { get; set; } = Array.Empty<byte>();

    // Navigation properties
    public virtual ICollection<JournalEntryDetail> Details { get; set; } = new List<JournalEntryDetail>();
}
