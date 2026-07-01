using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using FluentValidation;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using EnterpriseBillingSystem.Domain.Entities;
using EnterpriseBillingSystem.Domain.Repositories;

namespace EnterpriseBillingSystem.Application.Auth.Commands;

public record CreateUserCommand(
    string Username,
    string Password,
    string? Email,
    string FirstName,
    string LastName,
    Guid DefaultBranchId,
    string Role,
    string? Cedula = null,
    string? PhoneNumber = null,
    string? Address = null,
    string? Municipality = null,
    string? City = null,
    string? EmergencyContactName = null,
    string? EmergencyContactPhone = null,
    Guid? RouteId = null,
    bool IsEmployeeActive = true
) : IRequest<Guid>;

public class CreateUserCommandValidator : AbstractValidator<CreateUserCommand>
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly IRepository<Route> _routeRepository;

    public CreateUserCommandValidator(UserManager<ApplicationUser> userManager, IRepository<Route> routeRepository)
    {
        _userManager = userManager;
        _routeRepository = routeRepository;

        RuleFor(x => x.Username)
            .NotEmpty().WithMessage("El nombre de usuario es requerido.")
            .MinimumLength(3).WithMessage("El nombre de usuario debe tener al menos 3 caracteres.")
            .MustAsync(async (username, cancellation) =>
            {
                var existing = await _userManager.FindByNameAsync(username);
                return existing == null;
            }).WithMessage("El nombre de usuario ya está registrado.");

        RuleFor(x => x.Email)
            .EmailAddress().When(x => !string.IsNullOrWhiteSpace(x.Email)).WithMessage("El correo electrónico no es válido.")
            .MustAsync(async (email, cancellation) =>
            {
                if (string.IsNullOrWhiteSpace(email)) return true;
                var existing = await _userManager.FindByEmailAsync(email.Trim());
                return existing == null;
            }).WithMessage("El correo electrónico ya está registrado.");

        RuleFor(x => x.Password)
            .NotEmpty().WithMessage("La contraseña es requerida.")
            .MinimumLength(6).WithMessage("La contraseña debe tener al menos 6 caracteres.");

        RuleFor(x => x.FirstName)
            .NotEmpty().WithMessage("El nombre es requerido.");

        RuleFor(x => x.LastName)
            .NotEmpty().WithMessage("El apellido es requerido.");

        RuleFor(x => x.DefaultBranchId)
            .NotEmpty().WithMessage("La sucursal predeterminada es requerida.");

        RuleFor(x => x.Role)
            .NotEmpty().WithMessage("El rol es requerido.");

        RuleFor(x => x.Cedula)
            .MaximumLength(50).WithMessage("La cédula no puede exceder 50 caracteres.")
            .Must((cedula) =>
            {
                if (string.IsNullOrWhiteSpace(cedula)) return true;
                var trimmed = cedula.Trim();
                return trimmed.Length >= 3;
            }).WithMessage("La cédula debe tener al menos 3 caracteres.")
            .MustAsync(async (cedula, cancellation) =>
            {
                if (string.IsNullOrWhiteSpace(cedula)) return true;
                var trimmed = cedula.Trim();
                return !await _userManager.Users.AnyAsync(u => u.Cedula == trimmed, cancellation);
            }).WithMessage("La cédula ya está registrada en el sistema.");

        RuleFor(x => x.PhoneNumber)
            .MaximumLength(50).WithMessage("El número de teléfono no puede exceder 50 caracteres.");

        RuleFor(x => x.Address)
            .MaximumLength(500).WithMessage("El domicilio no puede exceder 500 caracteres.");

        RuleFor(x => x.Municipality)
            .MaximumLength(100).WithMessage("El municipio no puede exceder 100 caracteres.");

        RuleFor(x => x.City)
            .MaximumLength(100).WithMessage("La ciudad no puede exceder 100 caracteres.");

        RuleFor(x => x.RouteId)
            .MustAsync(async (routeId, cancellation) =>
            {
                if (!routeId.HasValue) return true;
                var route = await _routeRepository.GetByIdAsync(routeId.Value);
                return route != null;
            }).WithMessage("La ruta especificada no existe.");

        RuleFor(x => x.EmergencyContactName)
            .MaximumLength(200).WithMessage("El nombre de contacto de emergencia no puede exceder 200 caracteres.");

        RuleFor(x => x.EmergencyContactPhone)
            .MaximumLength(50).WithMessage("El teléfono de contacto de emergencia no puede exceder 50 caracteres.");
    }
}

public class CreateUserCommandHandler : IRequestHandler<CreateUserCommand, Guid>
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly RoleManager<ApplicationRole> _roleManager;

    public CreateUserCommandHandler(
        UserManager<ApplicationUser> userManager,
        RoleManager<ApplicationRole> roleManager)
    {
        _userManager = userManager;
        _roleManager = roleManager;
    }

    public async Task<Guid> Handle(CreateUserCommand request, CancellationToken cancellationToken)
    {
        var user = new ApplicationUser
        {
            UserName = request.Username,
            Email = string.IsNullOrWhiteSpace(request.Email) ? null : request.Email.Trim(),
            FirstName = request.FirstName?.Trim() ?? string.Empty,
            LastName = request.LastName?.Trim() ?? string.Empty,
            Cedula = string.IsNullOrWhiteSpace(request.Cedula) ? null : request.Cedula.Trim(),
            PhoneNumber = string.IsNullOrWhiteSpace(request.PhoneNumber) ? null : request.PhoneNumber.Trim(),
            Address = string.IsNullOrWhiteSpace(request.Address) ? null : request.Address.Trim(),
            Municipality = string.IsNullOrWhiteSpace(request.Municipality) ? null : request.Municipality.Trim(),
            City = string.IsNullOrWhiteSpace(request.City) ? null : request.City.Trim(),
            EmergencyContactName = string.IsNullOrWhiteSpace(request.EmergencyContactName) ? null : request.EmergencyContactName.Trim(),
            EmergencyContactPhone = string.IsNullOrWhiteSpace(request.EmergencyContactPhone) ? null : request.EmergencyContactPhone.Trim(),
            RouteId = request.RouteId,
            IsEmployeeActive = request.IsEmployeeActive,
            DefaultBranchId = request.DefaultBranchId,
            IsActive = true,
            ForcePasswordChange = false,
            CreatedBy = "System",
            CreatedOnUtc = DateTime.UtcNow
        };

        var result = await _userManager.CreateAsync(user, request.Password);
        if (!result.Succeeded)
        {
            var errors = string.Join(", ", result.Errors.Select(e => e.Description));
            throw new Exception($"No se pudo crear el usuario: {errors}");
        }

        if (!string.IsNullOrWhiteSpace(request.Role))
        {
            var roleExists = await _roleManager.RoleExistsAsync(request.Role);
            if (roleExists)
            {
                await _userManager.AddToRoleAsync(user, request.Role);
            }
        }

        return user.Id;
    }
}
