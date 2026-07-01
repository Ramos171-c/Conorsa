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

public record RefreshTokenCommand(string ExpiredToken, string RefreshToken) : IRequest<AuthResponseDto?>;

public class RefreshTokenCommandHandler : IRequestHandler<RefreshTokenCommand, AuthResponseDto?>
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly IPermissionRepository _permissionRepository;
    private readonly IJwtProvider _jwtProvider;
    private readonly IRepository<RefreshToken> _refreshTokenRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<RefreshTokenCommandHandler> _logger;

    public RefreshTokenCommandHandler(
        UserManager<ApplicationUser> userManager,
        IPermissionRepository permissionRepository,
        IJwtProvider jwtProvider,
        IRepository<RefreshToken> refreshTokenRepository,
        IUnitOfWork unitOfWork,
        ILogger<RefreshTokenCommandHandler> logger)
    {
        _userManager = userManager;
        _permissionRepository = permissionRepository;
        _jwtProvider = jwtProvider;
        _refreshTokenRepository = refreshTokenRepository;
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task<AuthResponseDto?> Handle(RefreshTokenCommand request, CancellationToken cancellationToken)
    {
        var tokens = await _refreshTokenRepository.FindAsync(t => t.Token == request.RefreshToken);
        var dbRefreshToken = tokens.FirstOrDefault();

        if (dbRefreshToken == null)
        {
            _logger.LogWarning("Intento de renovación fallido: El Refresh Token provisto no existe.");
            return null;
        }

        if (dbRefreshToken.IsUsed)
        {
            _logger.LogCritical("¡Alerta de Seguridad! Intento de reutilización del Refresh Token '{Token}' para el usuario '{UserId}'. Revocando todas las sesiones activas.", dbRefreshToken.Token, dbRefreshToken.UserId);
            
            var userTokens = await _refreshTokenRepository.FindAsync(t => t.UserId == dbRefreshToken.UserId && !t.IsRevoked && !t.IsUsed);
            foreach (var t in userTokens)
            {
                t.IsRevoked = true;
            }
            await _unitOfWork.SaveChangesAsync(cancellationToken);
            return null;
        }

        if (dbRefreshToken.IsRevoked || dbRefreshToken.IsExpired)
        {
            _logger.LogWarning("Intento de renovación fallido: El Refresh Token '{Token}' está revocado o expirado.", dbRefreshToken.Token);
            return null;
        }

        var user = await _userManager.FindByIdAsync(dbRefreshToken.UserId.ToString());
        if (user == null || !user.IsActive || user.IsDeleted)
        {
            _logger.LogWarning("Intento de renovación fallido: El usuario asociado '{UserId}' está inactivo, borrado o no existe.", dbRefreshToken.UserId);
            return null;
        }

        dbRefreshToken.IsUsed = true;

        var roles = await _userManager.GetRolesAsync(user);
        var roleName = roles.FirstOrDefault() ?? "User";
        var permissions = await _permissionRepository.GetPermissionsByRoleNameAsync(roleName);

        var (newAccessToken, expiryMinutes) = _jwtProvider.GenerateToken(user, roleName, permissions);
        var expiration = DateTime.UtcNow.AddMinutes(expiryMinutes);

        var newRefreshTokenValue = Guid.NewGuid().ToString();
        var newRefreshToken = new RefreshToken
        {
            UserId = user.Id,
            Token = newRefreshTokenValue,
            JwtId = Guid.NewGuid().ToString(),
            IsUsed = false,
            IsRevoked = false,
            CreatedOnUtc = DateTime.UtcNow,
            ExpiresOnUtc = DateTime.UtcNow.AddDays(7)
        };

        await _refreshTokenRepository.AddAsync(newRefreshToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Refresh Token renovado con éxito para el usuario '{Username}'.", user.UserName);

        return new AuthResponseDto(
            AccessToken: newAccessToken,
            RefreshToken: newRefreshTokenValue,
            Expiration: expiration,
            Username: user.UserName ?? string.Empty,
            RequiresPasswordChange: false
        );
    }
}
