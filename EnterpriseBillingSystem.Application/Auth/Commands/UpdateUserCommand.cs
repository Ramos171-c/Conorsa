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

public record UpdateUserCommand(
    Guid Id,
    string Email,
    string FirstName,
    string LastName,
    Guid DefaultBranchId,
    string Role,
    bool IsActive,
    string? Password = null,
    string? Cedula = null,
    string? PhoneNumber = null,
    string? Address = null,
    string? Municipality = null,
    string? City = null,
    string? EmergencyContactName = null,
    string? EmergencyContactPhone = null,
    Guid? RouteId = null,
    bool IsEmployeeActive = true
) : IRequest<bool>;

public class UpdateUserCommandValidator : AbstractValidator<UpdateUserCommand>
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly IRepository<Route> _routeRepository;

    public UpdateUserCommandValidator(UserManager<ApplicationUser> userManager, IRepository<Route> routeRepository)
    {
        _userManager = userManager;
        _routeRepository = routeRepository;

        RuleFor(x => x.Email)
            .NotEmpty().WithMessage("El correo electrónico es requerido.")
            .EmailAddress().WithMessage("El correo electrónico no es válido.")
            .MustAsync(async (command, email, cancellation) =>
            {
                var existing = await _userManager.FindByEmailAsync(email);
                return existing == null || existing.Id == command.Id;
            }).WithMessage("El correo electrónico ya está registrado por otro usuario.");

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
            .Must((command, cedula) =>
            {
                if (string.IsNullOrWhiteSpace(cedula)) return true;
                var trimmed = cedula.Trim();
                return trimmed.Length >= 3;
            }).WithMessage("La cédula debe tener al menos 3 caracteres.")
            .MustAsync(async (command, cedula, cancellation) =>
            {
                if (string.IsNullOrWhiteSpace(cedula)) return true;
                var trimmed = cedula.Trim();
                return !await _userManager.Users.AnyAsync(u => u.Id != command.Id && u.Cedula == trimmed, cancellation);
            }).WithMessage("La cédula ya está registrada en el sistema por otro usuario.");

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

public class UpdateUserCommandHandler : IRequestHandler<UpdateUserCommand, bool>
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly RoleManager<ApplicationRole> _roleManager;

    public UpdateUserCommandHandler(
        UserManager<ApplicationUser> userManager,
        RoleManager<ApplicationRole> roleManager)
    {
        _userManager = userManager;
        _roleManager = roleManager;
    }

    public async Task<bool> Handle(UpdateUserCommand request, CancellationToken cancellationToken)
    {
        var user = await _userManager.FindByIdAsync(request.Id.ToString());
        if (user == null || user.IsDeleted) return false;

        user.Email = request.Email;
        user.FirstName = request.FirstName?.Trim() ?? string.Empty;
        user.LastName = request.LastName?.Trim() ?? string.Empty;
        user.Cedula = string.IsNullOrWhiteSpace(request.Cedula) ? null : request.Cedula.Trim();
        user.PhoneNumber = string.IsNullOrWhiteSpace(request.PhoneNumber) ? null : request.PhoneNumber.Trim();
        user.Address = string.IsNullOrWhiteSpace(request.Address) ? null : request.Address.Trim();
        user.Municipality = string.IsNullOrWhiteSpace(request.Municipality) ? null : request.Municipality.Trim();
        user.City = string.IsNullOrWhiteSpace(request.City) ? null : request.City.Trim();
        user.EmergencyContactName = string.IsNullOrWhiteSpace(request.EmergencyContactName) ? null : request.EmergencyContactName.Trim();
        user.EmergencyContactPhone = string.IsNullOrWhiteSpace(request.EmergencyContactPhone) ? null : request.EmergencyContactPhone.Trim();
        user.RouteId = request.RouteId;
        user.IsEmployeeActive = request.IsEmployeeActive;
        user.DefaultBranchId = request.DefaultBranchId;
        user.IsActive = request.IsActive;
        user.LastModifiedBy = "System";
        user.LastModifiedOnUtc = DateTime.UtcNow;

        var result = await _userManager.UpdateAsync(user);
        if (!result.Succeeded)
        {
            var errors = string.Join(", ", result.Errors.Select(e => e.Description));
            throw new Exception($"No se pudo actualizar el usuario: {errors}");
        }

        var currentRoles = await _userManager.GetRolesAsync(user);
        if (!currentRoles.Contains(request.Role))
        {
            await _userManager.RemoveFromRolesAsync(user, currentRoles);
            var roleExists = await _roleManager.RoleExistsAsync(request.Role);
            if (roleExists)
            {
                await _userManager.AddToRoleAsync(user, request.Role);
            }
        }

        if (!string.IsNullOrWhiteSpace(request.Password))
        {
            var token = await _userManager.GeneratePasswordResetTokenAsync(user);
            var resetResult = await _userManager.ResetPasswordAsync(user, token, request.Password);
            if (!resetResult.Succeeded)
            {
                var errors = string.Join(", ", resetResult.Errors.Select(e => e.Description));
                throw new Exception($"No se pudo cambiar la contraseña del usuario: {errors}");
            }
        }

        return true;
    }
}
