using System;

namespace EnterpriseBillingSystem.Application.Common.Interfaces;

public interface ICurrentUserService
{
    string? UserId { get; }
    string? UserName { get; }
    Guid? BranchId { get; }
    bool IsAdmin { get; }
}
