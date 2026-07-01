using System;
using Microsoft.AspNetCore.Identity;
using EnterpriseBillingSystem.Domain.Common;

using System.Collections.Generic;

namespace EnterpriseBillingSystem.Domain.Entities;

public class ApplicationRole : IdentityRole<Guid>, IAuditable, ISoftDelete
{
    public string? Description { get; set; }
    public bool IsDeleted { get; set; }
    public ICollection<Permission> Permissions { get; set; } = new List<Permission>();

    // IAuditable
    public string? CreatedBy { get; set; }
    public DateTime CreatedOnUtc { get; set; }
    public string? LastModifiedBy { get; set; }
    public DateTime? LastModifiedOnUtc { get; set; }
    public Guid? BranchId { get; set; }

    public ApplicationRole() : base()
    {
    }

    public ApplicationRole(string roleName) : base(roleName)
    {
    }
}
