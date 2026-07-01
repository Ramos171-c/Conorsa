using System;
using System.Collections.Generic;
using EnterpriseBillingSystem.Domain.Common;
using EnterpriseBillingSystem.Domain.Enums;

namespace EnterpriseBillingSystem.Domain.Entities;

public class PurchaseReceipt : AuditableEntity
{
    public string ReceiptNumber { get; set; } = string.Empty;
    
    public Guid? PurchaseOrderId { get; set; }
    public virtual PurchaseOrder? PurchaseOrder { get; set; }

    public Guid SupplierId { get; set; }
    public virtual Supplier Supplier { get; set; } = null!;

    public Guid BranchWarehouseId { get; set; }
    public virtual BranchWarehouse BranchWarehouse { get; set; } = null!;

    public DateTime ReceiptDate { get; set; }
    public string? ReferenceDocument { get; set; }
    public PurchaseReceiptStatus Status { get; set; } = PurchaseReceiptStatus.Draft;
    public string? Notes { get; set; }

    // Concurrencia Optimista (RowVersion)
    public byte[] RowVersion { get; set; } = null!;

    // Navigation properties
    public virtual ICollection<PurchaseReceiptDetail> Details { get; set; } = new List<PurchaseReceiptDetail>();
}
