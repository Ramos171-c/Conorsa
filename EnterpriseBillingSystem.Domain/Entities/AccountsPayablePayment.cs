using System;
using EnterpriseBillingSystem.Domain.Common;

namespace EnterpriseBillingSystem.Domain.Entities;

public class AccountsPayablePayment : BaseEntity
{
    public Guid AccountsPayableId { get; set; }
    public virtual AccountsPayable AccountsPayable { get; set; } = null!;

    public Guid CashSessionId { get; set; }
    public virtual CashSession CashSession { get; set; } = null!;

    public Guid PaymentMethodId { get; set; }
    public virtual PaymentMethod PaymentMethod { get; set; } = null!;

    public DateTime PaymentDate { get; set; }
    public decimal Amount { get; set; }
    public string? ReferenceNumber { get; set; }
    public string? Notes { get; set; }
}
