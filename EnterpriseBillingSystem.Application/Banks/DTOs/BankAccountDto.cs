using System;
namespace EnterpriseBillingSystem.Application.Banks.DTOs;

public record BankAccountDto(
    Guid Id,
    Guid BankId,
    string BankName,
    string AccountNumber,
    string AccountName,
    string CurrencyCode,
    decimal CurrentBalance,
    string AccountingAccountCode,
    bool IsActive,
    Guid? BranchId
);
