using System;
using System.Collections.Generic;
using EnterpriseBillingSystem.Domain.Common;
using EnterpriseBillingSystem.Domain.Enums;

namespace EnterpriseBillingSystem.Domain.Entities;

public class AccountsPayable : AuditableEntity
{
    public Guid SupplierId { get; set; }
    public virtual Supplier Supplier { get; set; } = null!;

    public Guid PurchaseInvoiceId { get; set; }
    public virtual PurchaseInvoice PurchaseInvoice { get; set; } = null!;

    public string DocumentNumber { get; set; } = string.Empty;
    public DateTime InvoiceDate { get; set; }
    public DateTime DueDate { get; set; }

    public decimal OriginalAmount { get; set; }
    public decimal PaidAmount { get; set; }
    public decimal CurrentBalance { get; set; }

    public AccountsPayableStatus Status { get; set; } = AccountsPayableStatus.Pending;
    public DateTime? LastPaymentDate { get; set; }
    public string? Notes { get; set; }

    // Concurrencia optimista
    public byte[] RowVersion { get; set; } = null!;

    // Relación de abonos/pagos
    public virtual ICollection<AccountsPayablePayment> Payments { get; set; } = new List<AccountsPayablePayment>();
}
