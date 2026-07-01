using System;
using EnterpriseBillingSystem.Domain.Enums;
namespace EnterpriseBillingSystem.Application.Banks.DTOs;

public record BankTransactionDto(
    Guid Id,
    Guid BankAccountId,
    string BankAccountNumber,
    DateTime TransactionDate,
    BankTransactionType TransactionType,
    string TransactionTypeName,
    decimal Amount,
    string ReferenceNumber,
    string Description,
    Guid? RelatedBankAccountId
);
