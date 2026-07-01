using System;
using System.Collections.Generic;
using EnterpriseBillingSystem.Wpf.Models;

namespace EnterpriseBillingSystem.Wpf.Services.Authentication;

public class CurrentUserService
{
    public CurrentUserDto? CurrentUser { get; set; }
    public List<string> Permissions { get; set; } = new();
    public string? Token { get; set; }
    public Guid? BranchId { get; set; }

    public bool HasPermission(string permission)
    {
        if (CurrentUser?.Role == "Admin") return true;
        return Permissions.Contains(permission);
    }
}
