using System;
using System.Collections.Generic;
using EnterpriseBillingSystem.Domain.Common;
using EnterpriseBillingSystem.Domain.Enums;

namespace EnterpriseBillingSystem.Domain.Entities;

public class Account : GlobalAuditableEntity
{
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public Guid? ParentAccountId { get; set; }
    public AccountType AccountType { get; set; }
    public AccountNature Nature { get; set; }
    public int Level { get; set; }
    public bool IsPostingAccount { get; set; }
    public bool IsActive { get; set; } = true;

    // Navigation properties
    public virtual Account? ParentAccount { get; set; }
    public virtual ICollection<Account> SubAccounts { get; set; } = new List<Account>();
}
