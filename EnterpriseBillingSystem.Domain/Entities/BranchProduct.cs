using System;
using EnterpriseBillingSystem.Domain.Common;

namespace EnterpriseBillingSystem.Domain.Entities;

public class BranchProduct : IAuditable
{
    public Guid BranchId { get; set; }
    public Branch Branch { get; set; } = null!;

    public Guid ProductId { get; set; }
    public Product Product { get; set; } = null!;

    public decimal? LocalSalePrice { get; set; }
    public decimal? MinSalePrice { get; set; }
    public decimal? MaxDiscountPercentage { get; set; }
    public bool IsActive { get; set; } = true;

    // IAuditable implementation
    public string? CreatedBy { get; set; }
    public DateTime CreatedOnUtc { get; set; }
    public string? LastModifiedBy { get; set; }
    public DateTime? LastModifiedOnUtc { get; set; }

    Guid? IAuditable.BranchId
    {
        get => BranchId;
        set => BranchId = value ?? Guid.Empty;
    }
}
