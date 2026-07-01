using System;
using EnterpriseBillingSystem.Domain.Common;

namespace EnterpriseBillingSystem.Domain.Entities;

public class PurchaseReceiptDetail : BaseEntity
{
    public Guid PurchaseReceiptId { get; set; }
    public virtual PurchaseReceipt PurchaseReceipt { get; set; } = null!;

    public Guid ProductId { get; set; }
    public virtual Product Product { get; set; } = null!;

    public decimal Quantity { get; set; }
    
    public Guid UnitOfMeasureId { get; set; }
    public virtual UnitOfMeasure UnitOfMeasure { get; set; } = null!;

    public decimal UnitPrice { get; set; }
}
