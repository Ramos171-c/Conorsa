using System;
namespace EnterpriseBillingSystem.Application.Banks.DTOs;

public record BankReconciliationDto(
    Guid Id,
    Guid BankAccountId,
    string BankAccountNumber,
    DateTime StatementDate,
    decimal StatementBalance,
    decimal SystemBalance,
    decimal Difference,
    string? Notes,
    bool IsClosed
);
