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

    public Guid? BranchId
    {
        get
        {
            var branchIdClaim = _httpContextAccessor.HttpContext?.User?.FindFirstValue("branch_id");
            return branchIdClaim != null ? Guid.Parse(branchIdClaim) : null;
        }
    }
}
