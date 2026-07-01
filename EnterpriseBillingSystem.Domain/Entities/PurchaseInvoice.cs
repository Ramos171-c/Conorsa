using System;
using System.Collections.Generic;
using EnterpriseBillingSystem.Domain.Common;
using EnterpriseBillingSystem.Domain.Enums;

namespace EnterpriseBillingSystem.Domain.Entities;

public class PurchaseInvoice : AuditableEntity
{
    public string InvoiceNumber { get; set; } = string.Empty; // Número físico del proveedor
    public string InternalInvoiceNumber { get; set; } = string.Empty; // Secuencial interno
    
    public Guid? PurchaseReceiptId { get; set; }
    public virtual PurchaseReceipt? PurchaseReceipt { get; set; }

    public Guid? PurchaseOrderId { get; set; }
    public virtual PurchaseOrder? PurchaseOrder { get; set; }

    public Guid SupplierId { get; set; }
    public virtual Supplier Supplier { get; set; } = null!;

    public DateTime InvoiceDate { get; set; }
    public DateTime? DueDate { get; set; }
    
    public int PaymentTermsDays { get; set; } // Días de plazo de pago

    public decimal SubTotal { get; set; }
    public decimal DiscountAmount { get; set; }
    public decimal TaxAmount { get; set; }
    public decimal TotalAmount { get; set; }

    public PurchaseInvoiceStatus Status { get; set; } = PurchaseInvoiceStatus.Draft;
    public string? Notes { get; set; }

    // Navigation properties
    public virtual ICollection<PurchaseInvoiceDetail> Details { get; set; } = new List<PurchaseInvoiceDetail>();
}
