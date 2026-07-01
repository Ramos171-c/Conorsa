using System;
using EnterpriseBillingSystem.Domain.Common;

namespace EnterpriseBillingSystem.Domain.Entities;

public class InventoryReservation : AuditableEntity
{
    public Guid BranchWarehouseId { get; set; }
    public BranchWarehouse BranchWarehouse { get; set; } = null!;

    public Guid ProductId { get; set; }
    public Product Product { get; set; } = null!;

    public decimal Quantity { get; set; }
    public DateTime ExpiryDate { get; set; }
    public bool IsCompleted { get; set; }
    public bool IsCancelled { get; set; }
    public string? ReferenceDocument { get; set; }
}
