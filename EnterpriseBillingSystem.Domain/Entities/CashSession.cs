using System;
using System.Collections.Generic;
using EnterpriseBillingSystem.Domain.Common;
using EnterpriseBillingSystem.Domain.Enums;

namespace EnterpriseBillingSystem.Domain.Entities;

public class CashSession : AuditableEntity
{
    public string SessionNumber { get; set; } = string.Empty;

    public Guid CashRegisterId { get; set; }
    public virtual CashRegister CashRegister { get; set; } = null!;

    public Guid OpenedByUserId { get; set; }
    public virtual ApplicationUser OpenedByUser { get; set; } = null!;

    public Guid? ClosedByUserId { get; set; }
    public virtual ApplicationUser? ClosedByUser { get; set; }

    public decimal OpeningAmount { get; set; }
    public decimal? ClosingAmount { get; set; }
    public decimal? ExpectedAmount { get; set; }
    public decimal? DifferenceAmount { get; set; }

    public DateTime OpenedAt { get; set; }
    public DateTime? ClosedAt { get; set; }

    public CashSessionStatus Status { get; set; } = CashSessionStatus.Open;
    public string? Notes { get; set; }

    // Concurrencia optimista
    public byte[] RowVersion { get; set; } = null!;

    // Navigation
    public virtual ICollection<CashMovement> CashMovements { get; set; } = new List<CashMovement>();
}
