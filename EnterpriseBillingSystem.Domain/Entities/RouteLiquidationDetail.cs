using System;
using EnterpriseBillingSystem.Domain.Common;

namespace EnterpriseBillingSystem.Domain.Entities;

public class RouteLiquidationDetail : BaseEntity
{
    public Guid RouteLiquidationId { get; set; }
    public RouteLiquidation RouteLiquidation { get; set; } = null!;

    public Guid ProductId { get; set; }
    public Product Product { get; set; } = null!;

    public Guid UnitOfMeasureId { get; set; }
    public UnitOfMeasure UnitOfMeasure { get; set; } = null!;

    public Guid? ProductPresentationId { get; set; }
    public ProductPresentation? ProductPresentation { get; set; }

    public decimal QuantitySent { get; set; }
    public decimal QuantityReturned { get; set; }
    public decimal QuantitySold { get; set; }

    public decimal BaseQuantitySent { get; set; }
    public decimal BaseQuantityReturned { get; set; }
    public decimal BaseQuantitySold { get; set; }

    public decimal SalePrice { get; set; }
    public decimal Cost { get; set; }
    public decimal SubtotalSold { get; set; }
    public decimal SubtotalReturned { get; set; }

    public string? Notes { get; set; }
}
