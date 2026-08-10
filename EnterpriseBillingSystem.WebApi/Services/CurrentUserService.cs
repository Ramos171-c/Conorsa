using System;
using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using EnterpriseBillingSystem.Application.Common.Interfaces;

namespace EnterpriseBillingSystem.WebApi.Services;

public class CurrentUserService : ICurrentUserService
{
    private readonly IHttpContextAccessor _httpContextAccessor;

    public CurrentUserService(IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor;
    }

    public string? UserId => _httpContextAccessor.HttpContext?.User?.FindFirstValue(ClaimTypes.NameIdentifier);

    public string? UserName => _httpContextAccessor.HttpContext?.User?.Identity?.Name
                               ?? _httpContextAccessor.HttpContext?.User?.FindFirstValue(ClaimTypes.Name);

    public Guid? BranchId
    {
        get
        {
            var branchIdClaim = _httpContextAccessor.HttpContext?.User?.FindFirstValue("branch_id");
            return branchIdClaim != null ? Guid.Parse(branchIdClaim) : null;
        }
    }

    public bool IsAdmin
    {
        get
        {
            var user = _httpContextAccessor.HttpContext?.User;
            if (user == null) return false;
            var role = user.FindFirstValue(ClaimTypes.Role) ?? user.FindFirstValue("role") ?? "";
            return role.Equals("SUPER_ADMIN", StringComparison.OrdinalIgnoreCase) ||
                   role.Equals("ADMINISTRADOR", StringComparison.OrdinalIgnoreCase);
        }
    }
}
