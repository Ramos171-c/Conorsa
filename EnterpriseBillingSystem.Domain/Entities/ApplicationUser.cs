using System;
using System.Collections.Generic;
using Microsoft.AspNetCore.Identity;
using EnterpriseBillingSystem.Domain.Common;

namespace EnterpriseBillingSystem.Domain.Entities;

public class ApplicationUser : IdentityUser<Guid>, IAuditable, ISoftDelete
{
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string? Cedula { get; set; }
    public string? Address { get; set; }
    public string? Municipality { get; set; }
    public string? City { get; set; }
    public string? EmergencyContactName { get; set; }
    public string? EmergencyContactPhone { get; set; }
    public Guid? RouteId { get; set; }
    public virtual Route? Route { get; set; }
    public bool IsEmployeeActive { get; set; } = true;
    public bool IsActive { get; set; } = true;
    public DateTime? LastLoginAt { get; set; }
    public bool ForcePasswordChange { get; set; } = true;
    public Guid DefaultBranchId { get; set; }
    public Branch DefaultBranch { get; set; } = null!;
    
    // Soft Delete
    public bool IsDeleted { get; set; }
    public DateTime? DeletedAt { get; set; }
    public string? DeletedBy { get; set; }

    // IAuditable
    public string? CreatedBy { get; set; }
    public DateTime CreatedOnUtc { get; set; }
    public string? LastModifiedBy { get; set; }
    public DateTime? LastModifiedOnUtc { get; set; }
    
    public Guid? BranchId 
    { 
        get => DefaultBranchId; 
        set => DefaultBranchId = value ?? Guid.Empty; 
    }

    // Navegación
    public ICollection<UserBranch> UserBranches { get; set; } = new List<UserBranch>();
    public virtual ICollection<SalesGoal> SalesGoals { get; set; } = new List<SalesGoal>();
}
