using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using FluentValidation;
using EnterpriseBillingSystem.Domain.Entities;
using EnterpriseBillingSystem.Domain.Enums;
using EnterpriseBillingSystem.Domain.Repositories;

namespace EnterpriseBillingSystem.Application.Customers.Commands;

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

public record UpdateCustomerCommand(
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
) : IRequest<bool>;

public class UpdateCustomerCommandValidator : AbstractValidator<UpdateCustomerCommand>
{
    private readonly ICustomerRepository _customerRepository;
    private readonly ICustomerCategoryRepository _customerCategoryRepository;
    private readonly IRepository<CustomerPricingProfile> _pricingProfileRepository;

    public UpdateCustomerCommandValidator(
        ICustomerRepository customerRepository,
        ICustomerCategoryRepository customerCategoryRepository,
        IRepository<CustomerPricingProfile> pricingProfileRepository)
    {
        _customerRepository = customerRepository;
        _customerCategoryRepository = customerCategoryRepository;
        _pricingProfileRepository = pricingProfileRepository;

        RuleFor(x => x.Id)
            .NotEmpty().WithMessage("El ID del cliente es requerido.");

        RuleFor(x => x.IdentificationNumber)
            .NotEmpty().WithMessage("El número de identificación es requerido.")
            .MaximumLength(50).WithMessage("El número de identificación no puede exceder 50 caracteres.")
            .MustAsync(async (cmd, idNumber, cancellation) =>
            {
                return !await _customerRepository.ExistsByIdentificationAsync(idNumber, cmd.Id, cancellation);
            }).WithMessage("Ya existe otro cliente activo con este número de identificación.");

        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("El nombre es requerido.")
            .MaximumLength(150).WithMessage("El nombre no puede exceder 150 caracteres.");

        RuleFor(x => x.LegalName)
            .MaximumLength(150).WithMessage("La razón social/apellidos no puede exceder 150 caracteres.");

        RuleFor(x => x.CustomerCategoryId)
            .NotEmpty().WithMessage("La categoría de cliente es requerida.")
            .MustAsync(async (catId, cancellation) =>
            {
                var cat = await _customerCategoryRepository.GetByIdAsync(catId);
                return cat != null;
            }).WithMessage("La categoría de cliente especificada no existe.");

        RuleFor(x => x.CustomerPricingProfileId)
            .NotEmpty().WithMessage("El perfil de precios de cliente es requerido.")
            .MustAsync(async (profileId, cancellation) =>
            {
                var profile = await _pricingProfileRepository.GetByIdAsync(profileId);
                return profile != null;
            }).WithMessage("El perfil de precios de cliente especificado no existe.");

        RuleFor(x => x.CreditLimit)
            .GreaterThanOrEqualTo(0m).WithMessage("El límite de crédito debe ser mayor o igual a cero.");

        RuleFor(x => x.CreditDays)
            .GreaterThanOrEqualTo(0).WithMessage("Los días de crédito deben ser mayores o igual a cero.");

        RuleFor(x => x.DefaultDiscountPercentage)
            .InclusiveBetween(0.00m, 100.00m).WithMessage("El porcentaje de descuento debe estar entre 0% y 100%.");
    }
}

public class UpdateCustomerCommandHandler : IRequestHandler<UpdateCustomerCommand, bool>
{
    private readonly ICustomerRepository _customerRepository;
    private readonly IUnitOfWork _unitOfWork;

    public UpdateCustomerCommandHandler(
        ICustomerRepository customerRepository,
        IUnitOfWork unitOfWork)
    {
        _customerRepository = customerRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<bool> Handle(UpdateCustomerCommand request, CancellationToken cancellationToken)
    {
        var customer = await _customerRepository.GetByIdWithDetailsAsync(request.Id, cancellationToken);
        if (customer == null) return false;

        customer.IdentificationNumber = request.IdentificationNumber;
        customer.IdentificationType = request.IdentificationType;
        customer.CustomerType = request.CustomerType;
        customer.Name = request.Name;
        customer.LegalName = request.LegalName;
        customer.CustomerCategoryId = request.CustomerCategoryId;
        customer.CustomerPricingProfileId = request.CustomerPricingProfileId;
        customer.CreditLimit = request.CreditLimit;
        customer.CreditDays = request.CreditDays;
        customer.CanUseCredit = request.CanUseCredit;
        customer.IsTaxExempt = request.IsTaxExempt;
        customer.DefaultDiscountPercentage = request.DefaultDiscountPercentage;
        customer.Status = request.Status;

        // 1. Sincronizar Direcciones
        var inputAddrIds = request.Addresses.Where(a => a.Id != Guid.Empty).Select(a => a.Id).ToList();
        foreach (var existingAddr in customer.Addresses.ToList())
        {
            if (!inputAddrIds.Contains(existingAddr.Id))
            {
                existingAddr.IsDeleted = true;
            }
        }
        foreach (var addrInput in request.Addresses)
        {
            if (addrInput.Id == Guid.Empty)
            {
                customer.Addresses.Add(new CustomerAddress
                {
                    AddressLine1 = addrInput.AddressLine1,
                    AddressLine2 = addrInput.AddressLine2,
                    City = addrInput.City,
                    State = addrInput.State,
                    ZipCode = addrInput.ZipCode,
                    Country = addrInput.Country,
                    AddressType = addrInput.AddressType,
                    IsDefault = addrInput.IsDefault
                });
            }
            else
            {
                var existing = customer.Addresses.FirstOrDefault(a => a.Id == addrInput.Id);
                if (existing != null)
                {
                    existing.AddressLine1 = addrInput.AddressLine1;
                    existing.AddressLine2 = addrInput.AddressLine2;
                    existing.City = addrInput.City;
                    existing.State = addrInput.State;
                    existing.ZipCode = addrInput.ZipCode;
                    existing.Country = addrInput.Country;
                    existing.AddressType = addrInput.AddressType;
                    existing.IsDefault = addrInput.IsDefault;
                }
            }
        }

        // 2. Sincronizar Teléfonos
        var inputPhoneIds = request.Phones.Where(p => p.Id != Guid.Empty).Select(p => p.Id).ToList();
        foreach (var existingPhone in customer.Phones.ToList())
        {
            if (!inputPhoneIds.Contains(existingPhone.Id))
            {
                existingPhone.IsDeleted = true;
            }
        }
        foreach (var phoneInput in request.Phones)
        {
            if (phoneInput.Id == Guid.Empty)
            {
                customer.Phones.Add(new CustomerPhone
                {
                    PhoneNumber = phoneInput.PhoneNumber,
                    PhoneType = phoneInput.PhoneType,
                    IsDefault = phoneInput.IsDefault
                });
            }
            else
            {
                var existing = customer.Phones.FirstOrDefault(p => p.Id == phoneInput.Id);
                if (existing != null)
                {
                    existing.PhoneNumber = phoneInput.PhoneNumber;
                    existing.PhoneType = phoneInput.PhoneType;
                    existing.IsDefault = phoneInput.IsDefault;
                }
            }
        }

        // 3. Sincronizar Correos
        var inputEmailIds = request.Emails.Where(e => e.Id != Guid.Empty).Select(e => e.Id).ToList();
        foreach (var existingEmail in customer.Emails.ToList())
        {
            if (!inputEmailIds.Contains(existingEmail.Id))
            {
                existingEmail.IsDeleted = true;
            }
        }
        foreach (var emailInput in request.Emails)
        {
            if (emailInput.Id == Guid.Empty)
            {
                customer.Emails.Add(new CustomerEmail
                {
                    EmailAddress = emailInput.EmailAddress,
                    EmailType = emailInput.EmailType,
                    IsDefault = emailInput.IsDefault
                });
            }
            else
            {
                var existing = customer.Emails.FirstOrDefault(e => e.Id == emailInput.Id);
                if (existing != null)
                {
                    existing.EmailAddress = emailInput.EmailAddress;
                    existing.EmailType = emailInput.EmailType;
                    existing.IsDefault = emailInput.IsDefault;
                }
            }
        }

        // 4. Sincronizar Contactos
        var inputContactIds = request.Contacts.Where(c => c.Id != Guid.Empty).Select(c => c.Id).ToList();
        foreach (var existingContact in customer.Contacts.ToList())
        {
            if (!inputContactIds.Contains(existingContact.Id))
            {
                existingContact.IsDeleted = true;
            }
        }
        foreach (var contactInput in request.Contacts)
        {
            if (contactInput.Id == Guid.Empty)
            {
                customer.Contacts.Add(new CustomerContact
                {
                    FirstName = contactInput.FirstName,
                    LastName = contactInput.LastName,
                    JobTitle = contactInput.JobTitle,
                    Phone = contactInput.Phone,
                    Email = contactInput.Email,
                    Notes = contactInput.Notes,
                    IsDefault = contactInput.IsDefault
                });
            }
            else
            {
                var existing = customer.Contacts.FirstOrDefault(c => c.Id == contactInput.Id);
                if (existing != null)
                {
                    existing.FirstName = contactInput.FirstName;
                    existing.LastName = contactInput.LastName;
                    existing.JobTitle = contactInput.JobTitle;
                    existing.Phone = contactInput.Phone;
                    existing.Email = contactInput.Email;
                    existing.Notes = contactInput.Notes;
                    existing.IsDefault = contactInput.IsDefault;
                }
            }
        }

        _customerRepository.Update(customer);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return true;
    }
}
