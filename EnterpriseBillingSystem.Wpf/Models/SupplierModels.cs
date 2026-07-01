using System;

namespace EnterpriseBillingSystem.Wpf.Models;

public record SupplierDto(
    Guid Id,
    string SupplierCode,
    string IdentificationNumber,
    string IdentificationType,
    string Name,
    string? LegalName,
    string CategoryName,
    string? Phone,
    string? Email,
    string Status
);

public record SupplierDetailDto(
    Guid Id,
    string SupplierCode,
    string IdentificationNumber,
    string IdentificationType,
    string Name,
    string? LegalName,
    Guid SupplierCategoryId,
    string CategoryName,
    string? Phone,
    string? Email,
    string? Address,
    string? ContactName,
    string Status,
    bool IsActive,
    DateTime CreatedOnUtc
);

public record SupplierCategoryDto(
    Guid Id,
    string Name,
    string? Description
);

public record CreateSupplierCommandDto(
    string IdentificationNumber,
    IdentificationType IdentificationType,
    string Name,
    string? LegalName,
    Guid SupplierCategoryId,
    string? Phone,
    string? Email,
    string? Address,
    string? ContactName
);
