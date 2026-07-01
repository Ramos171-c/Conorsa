using MediatR;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;
using EnterpriseBillingSystem.Domain.Entities;
using EnterpriseBillingSystem.Domain.Repositories;
using EnterpriseBillingSystem.Application.Auth.DTOs;
using EnterpriseBillingSystem.Application.Common.Interfaces;

namespace EnterpriseBillingSystem.Application.Auth.Commands;

public record LoginCommand(string Username, string Password) : IRequest<AuthResponseDto?>;

public class LoginCommandHandler : IRequestHandler<LoginCommand, AuthResponseDto?>
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly IRepository<Branch> _branchRepository;
    private readonly IPermissionRepository _permissionRepository;
    private readonly IJwtProvider _jwtProvider;
    private readonly IRepository<RefreshToken> _refreshTokenRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<LoginCommandHandler> _logger;

    public LoginCommandHandler(
        UserManager<ApplicationUser> userManager,
        IRepository<Branch> branchRepository,
        IPermissionRepository permissionRepository,
        IJwtProvider jwtProvider,
        IRepository<RefreshToken> refreshTokenRepository,
        IUnitOfWork unitOfWork,
        ILogger<LoginCommandHandler> logger)
    {
        _userManager = userManager;
        _branchRepository = branchRepository;
        _permissionRepository = permissionRepository;
        _jwtProvider = jwtProvider;
        _refreshTokenRepository = refreshTokenRepository;
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task<AuthResponseDto?> Handle(LoginCommand request, CancellationToken cancellationToken)
    {
        var user = await _userManager.FindByNameAsync(request.Username);
        if (user == null)
        {
            _logger.LogWarning("Intento de inicio de sesión fallido: El usuario '{Username}' no existe.", request.Username);
            return null;
        }

        if (!user.IsActive || user.IsDeleted)
        {
            _logger.LogWarning("Intento de inicio de sesión fallido: El usuario '{Username}' está inactivo o ha sido eliminado.", request.Username);
            return null;
        }

        var branch = await _branchRepository.GetByIdAsync(user.DefaultBranchId);
        if (branch == null || !branch.IsActive || branch.IsDeleted)
        {
            _logger.LogWarning("Intento de inicio de sesión fallido: La sucursal por defecto del usuario '{Username}' está inactiva o no existe.", request.Username);
            return null;
        }

        var isPasswordValid = await _userManager.CheckPasswordAsync(user, request.Password);
        if (!isPasswordValid)
        {
            _logger.LogWarning("Intento de inicio de sesión fallido: Contraseña incorrecta para el usuario '{Username}'.", request.Username);
            await _userManager.AccessFailedAsync(user);
            return null;
        }

        await _userManager.ResetAccessFailedCountAsync(user);
        user.LastLoginAt = DateTime.UtcNow;
        await _userManager.UpdateAsync(user);

        if (user.ForcePasswordChange)
        {
            _logger.LogInformation("Usuario '{Username}' autenticado correctamente, pero requiere cambio obligatorio de contraseña.", request.Username);
            return new AuthResponseDto(
                AccessToken: string.Empty,
                RefreshToken: string.Empty,
                Expiration: DateTime.MinValue,
                Username: user.UserName ?? string.Empty,
                RequiresPasswordChange: true
            );
        }

        var roles = await _userManager.GetRolesAsync(user);
        var roleName = roles.FirstOrDefault() ?? "User";

        var permissions = await _permissionRepository.GetPermissionsByRoleNameAsync(roleName);

        var (accessToken, expiryMinutes) = _jwtProvider.GenerateToken(user, roleName, permissions);
        var expiration = DateTime.UtcNow.AddMinutes(expiryMinutes);

        var refreshTokenValue = Guid.NewGuid().ToString();
        var refreshToken = new RefreshToken
        {
            UserId = user.Id,
            Token = refreshTokenValue,
            JwtId = Guid.NewGuid().ToString(),
            IsUsed = false,
            IsRevoked = false,
            CreatedOnUtc = DateTime.UtcNow,
            ExpiresOnUtc = DateTime.UtcNow.AddDays(7)
        };

        await _refreshTokenRepository.AddAsync(refreshToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Usuario '{Username}' inició sesión con éxito. Sucursal activa: '{BranchName}'", request.Username, branch.Name);

        return new AuthResponseDto(
            AccessToken: accessToken,
            RefreshToken: refreshTokenValue,
            Expiration: expiration,
            Username: user.UserName ?? string.Empty,
            RequiresPasswordChange: false
        );
    }
}
