using System;
using System.Collections.Generic;
using EnterpriseBillingSystem.Domain.Common;
using EnterpriseBillingSystem.Domain.Enums;

namespace EnterpriseBillingSystem.Domain.Entities;

/// <summary>
/// Factura de venta — documento contable que mueve inventario.
/// Al pasar a estado Posted se descuenta el stock de la bodega indicada.
/// Al anularse (Cancelled) se genera un movimiento SaleReversal que repone el stock.
/// </summary>
public class SalesInvoice : AuditableEntity
{
    /// <summary>Número de factura autogenerado. Formato: INV-yyyyMMdd-00001</summary>
    public string InvoiceNumber { get; set; } = string.Empty;

    // ─── Cliente ─────────────────────────────────────────────────────────────
    public Guid CustomerId { get; set; }
    public virtual Customer Customer { get; set; } = null!;

    /// <summary>Snapshot del nombre del cliente al momento de facturar (inmutable histórico).</summary>
    public string CustomerNameSnapshot { get; set; } = string.Empty;

    /// <summary>Snapshot del número de identificación del cliente al momento de facturar.</summary>
    public string CustomerIdentificationSnapshot { get; set; } = string.Empty;

    /// <summary>Snapshot del tipo de cliente al momento de facturar.</summary>
    public CustomerType CustomerType { get; set; } = CustomerType.Natural;

    // ─── Bodega de salida ─────────────────────────────────────────────────────
    /// <summary>Bodega desde la cual se descuenta el inventario al confirmar.</summary>
    public Guid BranchWarehouseId { get; set; }
    public virtual BranchWarehouse BranchWarehouse { get; set; } = null!;

    // ─── Pedido de origen (opcional) ──────────────────────────────────────────
    public Guid? SalesOrderId { get; set; }
    public virtual SalesOrder? SalesOrder { get; set; }

    // ─── Notas de crédito (futuro) ────────────────────────────────────────────
    /// <summary>Referencia a la factura original cuando este documento es una nota de crédito.</summary>
    public Guid? OriginalInvoiceId { get; set; }
    public virtual SalesInvoice? OriginalInvoice { get; set; }

    // ─── Fechas ───────────────────────────────────────────────────────────────
    public DateTime InvoiceDate { get; set; }
    /// <summary>Fecha de vencimiento. null = venta contado.</summary>
    public DateTime? DueDate { get; set; }

    // ─── Tipo de venta ────────────────────────────────────────────────────────
    public bool IsCreditSale { get; set; }
    public int PaymentTermsDays { get; set; }

    // ─── Estado ───────────────────────────────────────────────────────────────
    public SalesInvoiceStatus Status { get; set; } = SalesInvoiceStatus.Draft;
    public string? CancellationReason { get; set; }
    public DateTime? CancelledOnUtc { get; set; }

    // ─── Totales ──────────────────────────────────────────────────────────────
    public decimal SubTotal { get; set; }
    public decimal DiscountAmount { get; set; }
    public decimal TaxAmount { get; set; }
    public decimal TotalAmount { get; set; }

    public string? Notes { get; set; }

    // Concurrencia optimista
    public byte[] RowVersion { get; set; } = null!;

    // Navigation
    public virtual ICollection<SalesInvoiceDetail> Details { get; set; } = new List<SalesInvoiceDetail>();
}
