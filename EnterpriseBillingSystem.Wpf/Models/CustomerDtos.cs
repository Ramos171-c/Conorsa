using System;
using System.Collections.Generic;

namespace EnterpriseBillingSystem.Wpf.Models;

public enum IdentificationType
{
    Cedula = 1,
    RUC = 2,
    Passport = 3,
    Other = 4
}

public enum CustomerType
{
    Natural = 1,
    LegalEntity = 2
}

public enum CustomerStatus
{
    Active = 1,
    Blocked = 2,
    Inactive = 3,
    Suspended = 4
}

public record CustomerCategoryDto(
    Guid Id,
    string Name,
    string? Description,
    decimal DefaultDiscountPercentage
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

// Input DTOs for Creation

public record CreateCustomerAddressInput(
    string AddressLine1,
    string? AddressLine2,
    string City,
    string? State,
    string? ZipCode,
    string Country,
    string AddressType,
    bool IsDefault
);

public record CreateCustomerPhoneInput(
    string PhoneNumber,
    string PhoneType,
    bool IsDefault
);

public record CreateCustomerEmailInput(
    string EmailAddress,
    string EmailType,
    bool IsDefault
);

public record CreateCustomerContactInput(
    string FirstName,
    string LastName,
    string? JobTitle,
    string? Phone,
    string? Email,
    string? Notes,
    bool IsDefault
);

public record CreateCustomerCommandDto(
    string IdentificationNumber,
    IdentificationType IdentificationType,
    CustomerType CustomerType,
    string Name,
    string? LegalName,
    Guid CustomerCategoryId,
    Guid CustomerPricingProfileId,
    decimal CreditLimit,
    int CreditDays,
    bool CanUseCredit,
    bool IsTaxExempt,
    decimal DefaultDiscountPercentage,
    List<CreateCustomerAddressInput> Addresses,
    List<CreateCustomerPhoneInput> Phones,
    List<CreateCustomerEmailInput> Emails,
    List<CreateCustomerContactInput> Contacts
);

// Input DTOs for Update

public record UpdateCustomerAddressInput(
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

public record UpdateCustomerPhoneInput(
    Guid Id,
    string PhoneNumber,
    string PhoneType,
    bool IsDefault
);

public record UpdateCustomerEmailInput(
    Guid Id,
    string EmailAddress,
    string EmailType,
    bool IsDefault
);

public record UpdateCustomerContactInput(
    Guid Id,
    string FirstName,
    string LastName,
    string? JobTitle,
    string? Phone,
    string? Email,
    string? Notes,
    bool IsDefault
);

public record UpdateCustomerCommandDto(
    Guid Id,
    string IdentificationNumber,
    IdentificationType IdentificationType,
    CustomerType CustomerType,
    string Name,
    string? LegalName,
    Guid CustomerCategoryId,
    Guid CustomerPricingProfileId,
    decimal CreditLimit,
    int CreditDays,
    bool CanUseCredit,
    bool IsTaxExempt,
    decimal DefaultDiscountPercentage,
    CustomerStatus Status,
    List<UpdateCustomerAddressInput> Addresses,
    List<UpdateCustomerPhoneInput> Phones,
    List<UpdateCustomerEmailInput> Emails,
    List<UpdateCustomerContactInput> Contacts
);
