using System;

namespace EnterpriseBillingSystem.Domain.Entities;

public class UserBranch
{
    public Guid UserId { get; set; }
    public ApplicationUser User { get; set; } = null!;
    
    public Guid BranchId { get; set; }
    public Branch Branch { get; set; } = null!;
    
    public bool IsDefault { get; set; }
}
