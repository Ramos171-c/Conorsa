using System;
using EnterpriseBillingSystem.Domain.Common;

namespace EnterpriseBillingSystem.Domain.Entities;

public class AccountingPeriod : GlobalAuditableEntity
{
    public int Year { get; set; }
    public int Month { get; set; }
    public bool IsClosed { get; set; }
    public string? ClosedBy { get; set; }
    public DateTime? ClosedAt { get; set; }
}
