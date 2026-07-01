using System;
using EnterpriseBillingSystem.Domain.Common;
using EnterpriseBillingSystem.Domain.Enums;

namespace EnterpriseBillingSystem.Domain.Entities;

public class CashMovement : BaseEntity
{
    public Guid CashSessionId { get; set; }
    public virtual CashSession CashSession { get; set; } = null!;

    public CashMovementType MovementType { get; set; }

    public Guid PaymentMethodId { get; set; }
    public virtual PaymentMethod PaymentMethod { get; set; } = null!;

    public string? ReferenceDocument { get; set; }
    public Guid? ReferenceId { get; set; }

    public decimal Amount { get; set; }

    /// <summary>Notas adicionales sobre el movimiento</summary>
    public string? Notes { get; set; }

    /// <summary>Motivo obligatorio para egresos (CashOut)</summary>
    public string? Reason { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
