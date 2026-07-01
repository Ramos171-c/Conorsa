using System;
using EnterpriseBillingSystem.Domain.Common;

namespace EnterpriseBillingSystem.Domain.Entities;

public class SalesOrderDetail : BaseEntity
{
    public Guid SalesOrderId { get; set; }
    public virtual SalesOrder SalesOrder { get; set; } = null!;

    public Guid ProductId { get; set; }
    public virtual Product Product { get; set; } = null!;

    public Guid UnitOfMeasureId { get; set; }
    public virtual UnitOfMeasure UnitOfMeasure { get; set; } = null!;

    public decimal Quantity { get; set; }
    public decimal UnitPrice { get; set; }
    public decimal DiscountPercentage { get; set; }
    public decimal DiscountAmount { get; set; }
    public decimal TaxPercentage { get; set; }
    public decimal TaxAmount { get; set; }

    /// <summary>Importe neto de línea = (Qty * UnitPrice) - Discount + Tax</summary>
    public decimal NetAmount { get; set; }
}
