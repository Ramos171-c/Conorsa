using System;
using System.Collections.Generic;
using EnterpriseBillingSystem.Domain.Common;
using EnterpriseBillingSystem.Domain.Enums;

namespace EnterpriseBillingSystem.Domain.Entities;

public class AccountsReceivable : AuditableEntity
{
    public Guid CustomerId { get; set; }
    public virtual Customer Customer { get; set; } = null!;

    public Guid SalesInvoiceId { get; set; }
    public virtual SalesInvoice SalesInvoice { get; set; } = null!;

    public string DocumentNumber { get; set; } = string.Empty;
    public DateTime InvoiceDate { get; set; }
    public DateTime DueDate { get; set; }

    public decimal OriginalAmount { get; set; }
    public decimal PaidAmount { get; set; }
    public decimal CurrentBalance { get; set; }

    public AccountsReceivableStatus Status { get; set; } = AccountsReceivableStatus.Pending;
    public DateTime? LastPaymentDate { get; set; }
    public string? Notes { get; set; }

    // Concurrencia optimista
    public byte[] RowVersion { get; set; } = null!;

    // Relación de pagos
    public virtual ICollection<AccountsReceivablePayment> Payments { get; set; } = new List<AccountsReceivablePayment>();
}
