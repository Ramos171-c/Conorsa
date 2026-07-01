using System;
using System.Collections.Generic;
using EnterpriseBillingSystem.Domain.Common;

namespace EnterpriseBillingSystem.Domain.Entities;

public class BranchWarehouse : AuditableEntity
{
    public new Guid BranchId { get; set; }
    public Branch Branch { get; set; } = null!;

    public Guid WarehouseId { get; set; }
    public Warehouse Warehouse { get; set; } = null!;

    public bool IsDefault { get; set; }
    public bool IsActive { get; set; } = true;
    public bool AllowNegativeInventory { get; set; } = false;

    public ICollection<Inventory> Inventories { get; set; } = new List<Inventory>();
}
