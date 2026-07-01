using System;
using System.Collections.Generic;
using EnterpriseBillingSystem.Domain.Common;
using EnterpriseBillingSystem.Domain.Enums;

namespace EnterpriseBillingSystem.Domain.Entities;

/// <summary>
/// Pedido de venta — documento pre-comercial opcional.
/// No mueve inventario. El inventario se mueve solo al confirmar la SalesInvoice.
/// </summary>
public class SalesOrder : AuditableEntity
{
    /// <summary>Número de pedido autogenerado. Formato: SO-yyyyMMdd-00001</summary>
    public string OrderNumber { get; set; } = string.Empty;

    public Guid CustomerId { get; set; }
    public virtual Customer Customer { get; set; } = null!;

    public DateTime OrderDate { get; set; }

    public SalesOrderStatus Status { get; set; } = SalesOrderStatus.Recibido;

    public decimal SubTotal { get; set; }
    public decimal DiscountAmount { get; set; }
    public decimal TaxAmount { get; set; }
    public decimal TotalAmount { get; set; }

    public string? Notes { get; set; }

    // Concurrencia optimista
    public byte[] RowVersion { get; set; } = null!;

    // Navigation
    public virtual ICollection<SalesOrderDetail> Details { get; set; } = new List<SalesOrderDetail>();
    public virtual ICollection<SalesInvoice> SalesInvoices { get; set; } = new List<SalesInvoice>();
}
