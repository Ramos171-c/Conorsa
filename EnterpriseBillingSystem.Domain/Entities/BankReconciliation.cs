using System;
using EnterpriseBillingSystem.Domain.Common;

namespace EnterpriseBillingSystem.Domain.Entities;

public class BankReconciliation : AuditableEntity
{
    public Guid BankAccountId { get; set; }
    public virtual BankAccount BankAccount { get; set; } = null!;

    public DateTime StatementDate { get; set; }
    public decimal StatementBalance { get; set; }
    public decimal SystemBalance { get; set; }
    public decimal Difference { get; set; }
    public string? Notes { get; set; }
    public bool IsClosed { get; set; }
}
