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

public record CreateCustomerCommand(
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
    List<CreateCustomerContactInput> Contacts,
    Guid? RouteId = null
) : IRequest<Guid>;

public class CreateCustomerCommandValidator : AbstractValidator<CreateCustomerCommand>
{
    private readonly ICustomerRepository _customerRepository;
    private readonly ICustomerCategoryRepository _customerCategoryRepository;
    private readonly IRepository<CustomerPricingProfile> _pricingProfileRepository;

    public CreateCustomerCommandValidator(
        ICustomerRepository customerRepository,
        ICustomerCategoryRepository customerCategoryRepository,
        IRepository<CustomerPricingProfile> pricingProfileRepository)
    {
        _customerRepository = customerRepository;
        _customerCategoryRepository = customerCategoryRepository;
        _pricingProfileRepository = pricingProfileRepository;

        RuleFor(x => x.IdentificationNumber)
            .NotEmpty().WithMessage("El número de identificación es requerido.")
            .MaximumLength(50).WithMessage("El número de identificación no puede exceder 50 caracteres.")
            .MustAsync(async (idNumber, cancellation) =>
            {
                return !await _customerRepository.ExistsByIdentificationAsync(idNumber, null, cancellation);
            }).WithMessage("Ya existe un cliente activo con este número de identificación.");

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

public class CreateCustomerCommandHandler : IRequestHandler<CreateCustomerCommand, Guid>
{
    private readonly ICustomerRepository _customerRepository;
    private readonly IRepository<ApplicationUser> _userRepository;
    private readonly Common.Interfaces.ICurrentUserService _currentUserService;
    private readonly IUnitOfWork _unitOfWork;

    public CreateCustomerCommandHandler(
        ICustomerRepository customerRepository,
        IRepository<ApplicationUser> userRepository,
        Common.Interfaces.ICurrentUserService currentUserService,
        IUnitOfWork unitOfWork)
    {
        _customerRepository = customerRepository;
        _userRepository = userRepository;
        _currentUserService = currentUserService;
        _unitOfWork = unitOfWork;
    }

    public async Task<Guid> Handle(CreateCustomerCommand request, CancellationToken cancellationToken)
    {
        // Generar código autogenerado
        var customerCode = await _customerRepository.GenerateCustomerCodeAsync(cancellationToken);

        Guid? sellerRouteId = null;
        if (Guid.TryParse(_currentUserService.UserId, out var sellerId))
        {
            var seller = await _userRepository.GetByIdAsync(sellerId);
            if (seller != null && seller.RouteId.HasValue)
            {
                sellerRouteId = seller.RouteId;
            }
        }

        var customer = new Customer
        {
            CustomerCode = customerCode,
            IdentificationNumber = request.IdentificationNumber,
            IdentificationType = request.IdentificationType,
            CustomerType = request.CustomerType,
            Name = request.Name,
            LegalName = request.LegalName,
            CustomerCategoryId = request.CustomerCategoryId,
            CustomerPricingProfileId = request.CustomerPricingProfileId,
            CreditLimit = request.CreditLimit,
            CreditDays = request.CreditDays,
            CanUseCredit = request.CanUseCredit,
            IsTaxExempt = request.IsTaxExempt,
            DefaultDiscountPercentage = request.DefaultDiscountPercentage,
            Status = CustomerStatus.Active,
            RouteId = request.RouteId ?? sellerRouteId
        };

        // Agregar direcciones
        if (request.Addresses != null)
        {
            foreach (var addr in request.Addresses)
            {
                customer.Addresses.Add(new CustomerAddress
                {
                    AddressLine1 = addr.AddressLine1,
                    AddressLine2 = addr.AddressLine2,
                    City = addr.City,
                    State = addr.State,
                    ZipCode = addr.ZipCode,
                    Country = addr.Country,
                    AddressType = addr.AddressType,
                    IsDefault = addr.IsDefault
                });
            }
        }

        // Agregar teléfonos
        if (request.Phones != null)
        {
            foreach (var ph in request.Phones)
            {
                customer.Phones.Add(new CustomerPhone
                {
                    PhoneNumber = ph.PhoneNumber,
                    PhoneType = ph.PhoneType,
                    IsDefault = ph.IsDefault
                });
            }
        }

        // Agregar correos
        if (request.Emails != null)
        {
            foreach (var em in request.Emails)
            {
                customer.Emails.Add(new CustomerEmail
                {
                    EmailAddress = em.EmailAddress,
                    EmailType = em.EmailType,
                    IsDefault = em.IsDefault
                });
            }
        }

        // Agregar contactos
        if (request.Contacts != null)
        {
            foreach (var co in request.Contacts)
            {
                customer.Contacts.Add(new CustomerContact
                {
                    FirstName = co.FirstName,
                    LastName = co.LastName,
                    JobTitle = co.JobTitle,
                    Phone = co.Phone,
                    Email = co.Email,
                    Notes = co.Notes,
                    IsDefault = co.IsDefault
                });
            }
        }

        await _customerRepository.AddAsync(customer);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return customer.Id;
    }
}
