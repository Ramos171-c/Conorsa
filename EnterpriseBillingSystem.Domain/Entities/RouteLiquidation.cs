using System;
using System.Collections.Generic;
using EnterpriseBillingSystem.Domain.Common;
using EnterpriseBillingSystem.Domain.Enums;

namespace EnterpriseBillingSystem.Domain.Entities;

public class RouteLiquidation : AuditableEntity
{
    public string LiquidationNumber { get; set; } = string.Empty;
    public Guid RouteId { get; set; }
    public Route Route { get; set; } = null!;

    public DateTime LiquidationDate { get; set; } = DateTime.UtcNow;
    public RouteLiquidationStatus Status { get; set; } = RouteLiquidationStatus.Confirmada;

    public decimal TotalQuantitySent { get; set; }
    public decimal TotalQuantityReturned { get; set; }
    public decimal TotalQuantitySold { get; set; }

    public decimal TotalAmountSold { get; set; }
    public decimal TotalAmountReturned { get; set; }
    public decimal TotalCostSold { get; set; }
    public decimal EstimatedProfit { get; set; }

    public string? Observations { get; set; }

    public ICollection<RouteLiquidationDetail> Details { get; set; } = new List<RouteLiquidationDetail>();
}
