using System;
using System.Collections.Generic;
using EnterpriseBillingSystem.Domain.Common;
using EnterpriseBillingSystem.Domain.Enums;

namespace EnterpriseBillingSystem.Domain.Entities;

public class InventoryMovement : AuditableEntity
{
    public string MovementNumber { get; set; } = string.Empty;
    public MovementType MovementType { get; set; }
    
    public Guid? FromBranchWarehouseId { get; set; }
    public BranchWarehouse? FromBranchWarehouse { get; set; }

    public Guid? ToBranchWarehouseId { get; set; }
    public BranchWarehouse? ToBranchWarehouse { get; set; }

    public string? ReferenceDocument { get; set; }
    public string? Notes { get; set; }
    public DateTime MovementDate { get; set; } = DateTime.UtcNow;

    public ICollection<InventoryMovementDetail> Details { get; set; } = new List<InventoryMovementDetail>();
}
