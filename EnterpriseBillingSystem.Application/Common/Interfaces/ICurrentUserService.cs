using System;

namespace EnterpriseBillingSystem.Application.Common.Interfaces;

public interface ICurrentUserService
{
    string? UserId { get; }
    Guid? BranchId { get; }
}
