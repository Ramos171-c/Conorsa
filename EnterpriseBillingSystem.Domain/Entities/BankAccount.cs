using System;
using System.Collections.Generic;
using EnterpriseBillingSystem.Domain.Common;

namespace EnterpriseBillingSystem.Domain.Entities;

public class BankAccount : AuditableEntity
{
    public Guid BankId { get; set; }
    public virtual Bank Bank { get; set; } = null!;

    public string AccountNumber { get; set; } = string.Empty;
    public string AccountName { get; set; } = string.Empty;
    public string CurrencyCode { get; set; } = string.Empty;
    public decimal CurrentBalance { get; set; }
    public string AccountingAccountCode { get; set; } = string.Empty;
    public bool IsActive { get; set; } = true;
    public byte[] RowVersion { get; set; } = Array.Empty<byte>();

    // Navigation properties
    public virtual ICollection<BankTransaction> BankTransactions { get; set; } = new List<BankTransaction>();
}
