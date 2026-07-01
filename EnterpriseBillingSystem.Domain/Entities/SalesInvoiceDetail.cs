using System;
using EnterpriseBillingSystem.Domain.Common;

namespace EnterpriseBillingSystem.Domain.Entities;

public class SalesInvoiceDetail : BaseEntity
{
    public Guid SalesInvoiceId { get; set; }
    public virtual SalesInvoice SalesInvoice { get; set; } = null!;

    // ─── Producto (FK viva) ───────────────────────────────────────────────────
    public Guid ProductId { get; set; }
    public virtual Product Product { get; set; } = null!;

    public Guid UnitOfMeasureId { get; set; }
    public virtual UnitOfMeasure UnitOfMeasure { get; set; } = null!;

    public Guid ProductPresentationId { get; set; }
    public virtual ProductPresentation ProductPresentation { get; set; } = null!;

    // ─── Snapshots históricos (inmutables) ────────────────────────────────────
    /// <summary>Código interno del producto al momento de facturar (e.g. SKU).</summary>
    public string ProductCodeSnapshot { get; set; } = string.Empty;

    /// <summary>Nombre del producto al momento de facturar.</summary>
    public string ProductNameSnapshot { get; set; } = string.Empty;

    /// <summary>Código de la unidad de medida al momento de facturar.</summary>
    public string UnitOfMeasureSnapshot { get; set; } = string.Empty;

    // ─── Cantidades y precios ─────────────────────────────────────────────────
    public decimal Quantity { get; set; }
    public decimal UnitPrice { get; set; }
    public decimal DiscountPercentage { get; set; }
    public decimal DiscountAmount { get; set; }
    public decimal TaxPercentage { get; set; }
    public decimal TaxAmount { get; set; }

    /// <summary>Importe neto de línea = (Qty * UnitPrice) - Discount + Tax</summary>
    public decimal NetAmount { get; set; }
}
