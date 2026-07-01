using System;
using System.Collections.Generic;
using EnterpriseBillingSystem.Domain.Enums;

namespace EnterpriseBillingSystem.Application.Customers.DTOs;

public record CustomerPricingProfileDto(
    Guid Id,
    string Name,
    CustomerPricingType Type,
    bool IsActive
);

public record CustomerAddressDto(
    Guid Id,
    string AddressLine1,
    string? AddressLine2,
    string City,
    string? State,
    string? ZipCode,
    string Country,
    string AddressType,
    bool IsDefault
);

public record CustomerPhoneDto(
    Guid Id,
    string PhoneNumber,
    string PhoneType,
    bool IsDefault
);

public record CustomerEmailDto(
    Guid Id,
    string EmailAddress,
    string EmailType,
    bool IsDefault
);

public record CustomerContactDto(
    Guid Id,
    string FirstName,
    string LastName,
    string? JobTitle,
    string? Phone,
    string? Email,
    string? Notes,
    bool IsDefault
);

public record CustomerDto(
    Guid Id,
    string CustomerCode,
    string IdentificationNumber,
    IdentificationType IdentificationType,
    CustomerType CustomerType,
    string Name,
    string? LegalName,
    Guid CustomerCategoryId,
    string CustomerCategoryName,
    Guid CustomerPricingProfileId,
    string CustomerPricingProfileName,
    CustomerPricingType CustomerPricingProfileType,
    decimal CreditLimit,
    int CreditDays,
    bool CanUseCredit,
    bool IsTaxExempt,
    decimal DefaultDiscountPercentage,
    CustomerStatus Status,
    List<CustomerAddressDto> Addresses,
    List<CustomerPhoneDto> Phones,
    List<CustomerEmailDto> Emails,
    List<CustomerContactDto> Contacts,
    Guid? RouteId = null,
    string? RouteName = null
);
