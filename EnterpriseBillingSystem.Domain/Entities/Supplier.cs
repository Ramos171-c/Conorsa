using System;
using System.Collections.Generic;
using EnterpriseBillingSystem.Domain.Common;
using EnterpriseBillingSystem.Domain.Enums;

namespace EnterpriseBillingSystem.Domain.Entities;

public class Supplier : GlobalAuditableEntity
{
    public string SupplierCode { get; set; } = string.Empty;
    public string IdentificationNumber { get; set; } = string.Empty;
    public IdentificationType IdentificationType { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? LegalName { get; set; }
    
    public Guid SupplierCategoryId { get; set; }
    public virtual SupplierCategory SupplierCategory { get; set; } = null!;

    public string? Phone { get; set; }
    public string? Email { get; set; }
    public string? Address { get; set; }
    public string? ContactName { get; set; }
    
    public SupplierStatus Status { get; set; } = SupplierStatus.Active;
    public bool IsActive { get; set; } = true;

    // Navigation properties
    public virtual ICollection<PurchaseOrder> PurchaseOrders { get; set; } = new List<PurchaseOrder>();
    public virtual ICollection<PurchaseReceipt> PurchaseReceipts { get; set; } = new List<PurchaseReceipt>();
    public virtual ICollection<PurchaseInvoice> PurchaseInvoices { get; set; } = new List<PurchaseInvoice>();
}
