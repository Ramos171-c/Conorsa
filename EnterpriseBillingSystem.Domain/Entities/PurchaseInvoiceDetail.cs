using System;
using EnterpriseBillingSystem.Domain.Common;

namespace EnterpriseBillingSystem.Domain.Entities;

public class PurchaseInvoiceDetail : BaseEntity
{
    public Guid PurchaseInvoiceId { get; set; }
    public virtual PurchaseInvoice PurchaseInvoice { get; set; } = null!;

    public Guid ProductId { get; set; }
    public virtual Product Product { get; set; } = null!;

    public decimal Quantity { get; set; }

    public Guid UnitOfMeasureId { get; set; }
    public virtual UnitOfMeasure UnitOfMeasure { get; set; } = null!;

    public Guid ProductPresentationId { get; set; }
    public virtual ProductPresentation ProductPresentation { get; set; } = null!;

    public decimal UnitPrice { get; set; }
    public decimal DiscountPercentage { get; set; }
    public decimal DiscountAmount { get; set; }
    public decimal TaxPercentage { get; set; }
    public decimal TaxAmount { get; set; }
    public decimal NetAmount { get; set; }
}
