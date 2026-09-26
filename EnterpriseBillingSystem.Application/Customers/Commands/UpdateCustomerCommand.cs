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
    string? Neighborhood,
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
    List<UpdateCustomerContactInput> Contacts,
    Guid? RouteId = null
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
            .NotEmpty().WithMessage("El identificador del cliente es requerido.");

        RuleFor(x => x.IdentificationNumber)
            .NotEmpty().WithMessage("El número de identificación es requerido.")
            .MaximumLength(50).WithMessage("El número de identificación no puede exceder 50 caracteres.")
            .MustAsync(async (command, idNumber, cancellation) =>
            {
                return !await _customerRepository.ExistsByIdentificationAsync(idNumber, command.Id, cancellation);
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
        customer.RouteId = request.RouteId;

        // Actualizar direcciones
        var incomingAddrIds = request.Addresses.Where(a => a.Id != Guid.Empty).Select(a => a.Id).ToHashSet();
        foreach (var existing in customer.Addresses.Where(a => !incomingAddrIds.Contains(a.Id)).ToList())
        {
            customer.Addresses.Remove(existing);
        }
        foreach (var incoming in request.Addresses)
        {
            if (incoming.Id == Guid.Empty)
            {
                customer.Addresses.Add(new CustomerAddress
                {
                    AddressLine1 = incoming.AddressLine1,
                    AddressLine2 = incoming.AddressLine2,
                    Neighborhood = incoming.Neighborhood,
                    City = incoming.City,
                    State = incoming.State,
                    ZipCode = incoming.ZipCode,
                    Country = incoming.Country,
                    AddressType = incoming.AddressType,
                    IsDefault = incoming.IsDefault
                });
            }
            else
            {
                var addr = customer.Addresses.FirstOrDefault(a => a.Id == incoming.Id);
                if (addr != null)
                {
                    addr.AddressLine1 = incoming.AddressLine1;
                    addr.AddressLine2 = incoming.AddressLine2;
                    addr.Neighborhood = incoming.Neighborhood;
                    addr.City = incoming.City;
                    addr.State = incoming.State;
                    addr.ZipCode = incoming.ZipCode;
                    addr.Country = incoming.Country;
                    addr.AddressType = incoming.AddressType;
                    addr.IsDefault = incoming.IsDefault;
                }
            }
        }

        // Actualizar teléfonos
        var incomingPhoneIds = request.Phones.Where(p => p.Id != Guid.Empty).Select(p => p.Id).ToHashSet();
        foreach (var existing in customer.Phones.Where(p => !incomingPhoneIds.Contains(p.Id)).ToList())
        {
            customer.Phones.Remove(existing);
        }
        foreach (var incoming in request.Phones)
        {
            if (incoming.Id == Guid.Empty)
            {
                customer.Phones.Add(new CustomerPhone
                {
                    PhoneNumber = incoming.PhoneNumber,
                    PhoneType = incoming.PhoneType,
                    IsDefault = incoming.IsDefault
                });
            }
            else
            {
                var ph = customer.Phones.FirstOrDefault(p => p.Id == incoming.Id);
                if (ph != null)
                {
                    ph.PhoneNumber = incoming.PhoneNumber;
                    ph.PhoneType = incoming.PhoneType;
                    ph.IsDefault = incoming.IsDefault;
                }
            }
        }

        // Actualizar correos
        var incomingEmailIds = request.Emails.Where(e => e.Id != Guid.Empty).Select(e => e.Id).ToHashSet();
        foreach (var existing in customer.Emails.Where(e => !incomingEmailIds.Contains(e.Id)).ToList())
        {
            customer.Emails.Remove(existing);
        }
        foreach (var incoming in request.Emails)
        {
            if (incoming.Id == Guid.Empty)
            {
                customer.Emails.Add(new CustomerEmail
                {
                    EmailAddress = incoming.EmailAddress,
                    EmailType = incoming.EmailType,
                    IsDefault = incoming.IsDefault
                });
            }
            else
            {
                var em = customer.Emails.FirstOrDefault(e => e.Id == incoming.Id);
                if (em != null)
                {
                    em.EmailAddress = incoming.EmailAddress;
                    em.EmailType = incoming.EmailType;
                    em.IsDefault = incoming.IsDefault;
                }
            }
        }

        // Actualizar contactos
        var incomingContactIds = request.Contacts.Where(c => c.Id != Guid.Empty).Select(c => c.Id).ToHashSet();
        foreach (var existing in customer.Contacts.Where(c => !incomingContactIds.Contains(c.Id)).ToList())
        {
            customer.Contacts.Remove(existing);
        }
        foreach (var incoming in request.Contacts)
        {
            if (incoming.Id == Guid.Empty)
            {
                customer.Contacts.Add(new CustomerContact
                {
                    FirstName = incoming.FirstName,
                    LastName = incoming.LastName,
                    JobTitle = incoming.JobTitle,
                    Phone = incoming.Phone,
                    Email = incoming.Email,
                    Notes = incoming.Notes,
                    IsDefault = incoming.IsDefault
                });
            }
            else
            {
                var co = customer.Contacts.FirstOrDefault(c => c.Id == incoming.Id);
                if (co != null)
                {
                    co.FirstName = incoming.FirstName;
                    co.LastName = incoming.LastName;
                    co.JobTitle = incoming.JobTitle;
                    co.Phone = incoming.Phone;
                    co.Email = incoming.Email;
                    co.Notes = incoming.Notes;
                    co.IsDefault = incoming.IsDefault;
                }
            }
        }

        _customerRepository.Update(customer);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return true;
    }
}