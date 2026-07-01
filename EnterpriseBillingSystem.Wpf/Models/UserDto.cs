using System;

namespace EnterpriseBillingSystem.Wpf.Models;

public record UserDto(
    Guid Id,
    string Username,
    string? Email,
    string FirstName,
    string LastName,
    bool IsActive,
    Guid DefaultBranchId,
    string DefaultBranchName,
    string Role,
    string? Cedula,
    string? PhoneNumber,
    string? Address,
    string? Municipality,
    string? City,
    string? EmergencyContactName,
    string? EmergencyContactPhone,
    Guid? RouteId,
    string? Route,
    bool IsEmployeeActive
)
{
    public string FullName => $"{FirstName} {LastName}".Trim();
}

public record CreateUserCommandDto(
    string Username,
    string Password,
    string? Email,
    string FirstName,
    string LastName,
    Guid DefaultBranchId,
    string Role,
    string? Cedula = null,
    string? PhoneNumber = null,
    string? Address = null,
    string? Municipality = null,
    string? City = null,
    string? EmergencyContactName = null,
    string? EmergencyContactPhone = null,
    Guid? RouteId = null,
    bool IsEmployeeActive = true
);

public record UpdateUserCommandDto(
    Guid Id,
    string? Email,
    string FirstName,
    string LastName,
    Guid DefaultBranchId,
    string Role,
    bool IsActive,
    string? Password = null,
    string? Cedula = null,
    string? PhoneNumber = null,
    string? Address = null,
    string? Municipality = null,
    string? City = null,
    string? EmergencyContactName = null,
    string? EmergencyContactPhone = null,
    Guid? RouteId = null,
    bool IsEmployeeActive = true
);

public record RouteDto(Guid Id, string Code, string Name, bool IsActive);
public record CreateRouteDto(string Code, string Name);
public record UpdateRouteDto(string Code, string Name, bool IsActive);
