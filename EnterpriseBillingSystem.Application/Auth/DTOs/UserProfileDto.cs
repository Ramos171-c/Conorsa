using System;
using System.Collections.Generic;

namespace EnterpriseBillingSystem.Application.Auth.DTOs;

public record UserProfileDto(
    Guid Id,
    string Username,
    string Email,
    string FirstName,
    string LastName,
    Guid DefaultBranchId,
    string Role,
    IEnumerable<string> Permissions
);
