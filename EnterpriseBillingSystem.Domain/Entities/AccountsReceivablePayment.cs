using System;
using EnterpriseBillingSystem.Domain.Common;

namespace EnterpriseBillingSystem.Domain.Entities;

public class AccountsReceivablePayment : BaseEntity
{
    public Guid AccountsReceivableId { get; set; }
    public virtual AccountsReceivable AccountsReceivable { get; set; } = null!;

    public Guid CashSessionId { get; set; }
    public virtual CashSession CashSession { get; set; } = null!;

    public Guid PaymentMethodId { get; set; }
    public virtual PaymentMethod PaymentMethod { get; set; } = null!;

    public DateTime PaymentDate { get; set; }
    public decimal Amount { get; set; }
    public string? ReferenceNumber { get; set; }
    public string? Notes { get; set; }
}
