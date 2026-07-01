using System;
namespace EnterpriseBillingSystem.Application.Banks.DTOs;

public record BankDto(
    Guid Id,
    string Code,
    string Name,
    string SwiftCode,
    string Country,
    bool IsActive
);
