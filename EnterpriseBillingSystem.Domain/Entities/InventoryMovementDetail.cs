using System;
using EnterpriseBillingSystem.Domain.Common;

namespace EnterpriseBillingSystem.Domain.Entities;

public class InventoryMovementDetail : AuditableEntity
{
    public Guid InventoryMovementId { get; set; }
    public InventoryMovement InventoryMovement { get; set; } = null!;

    public Guid ProductId { get; set; }
    public Product Product { get; set; } = null!;

    public decimal Quantity { get; set; }
    
    public Guid UnitOfMeasureId { get; set; }
    public UnitOfMeasure UnitOfMeasure { get; set; } = null!;

    public Guid ProductPresentationId { get; set; }
    public ProductPresentation ProductPresentation { get; set; } = null!;

    public decimal ConversionFactor { get; set; } = 1.000000m;
    public decimal QuantityInBaseUnit { get; set; }
}
