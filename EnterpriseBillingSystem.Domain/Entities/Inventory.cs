using System;
using EnterpriseBillingSystem.Domain.Common;

namespace EnterpriseBillingSystem.Domain.Entities;

public class Inventory : AuditableEntity
{
    public Guid BranchWarehouseId { get; set; }
    public BranchWarehouse BranchWarehouse { get; set; } = null!;

    public Guid ProductId { get; set; }
    public Product Product { get; set; } = null!;

    public decimal PhysicalStock { get; set; }
    public decimal ReservedStock { get; set; }
    public decimal CommittedStock { get; set; }

    public decimal AvailableStock => PhysicalStock - ReservedStock - CommittedStock;

    // Concurrencia Optimista (RowVersion)
    public byte[] RowVersion { get; set; } = null!;
}
