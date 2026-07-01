using System;
using System.Collections.Concurrent;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using EnterpriseBillingSystem.Application.Common.Interfaces;
using EnterpriseBillingSystem.Infrastructure.Identity;

namespace EnterpriseBillingSystem.Infrastructure.Services;

public class AuthService : IAuthService
{
    private readonly JwtSettings _jwtSettings;
    
    // Almacén simulado para refresh tokens en memoria (clave: refreshToken, valor: username)
    private static readonly ConcurrentDictionary<string, string> RefreshTokens = new();

    public AuthService(IOptions<JwtSettings> jwtSettings)
    {
        _jwtSettings = jwtSettings.Value;
    }

    public Task<LoginResponseDto?> LoginAsync(string username, string password)
    {
        // Validación simulada de usuarios (Admin y Cajero de sucursal)
        if (string.IsNullOrWhiteSpace(username) || string.IsNullOrWhiteSpace(password))
            return Task.FromResult<LoginResponseDto?>(null);

        Guid branchId;
        string role;

        if (username.Equals("admin", StringComparison.OrdinalIgnoreCase) && password == "admin123")
        {
            branchId = Guid.Parse("11111111-1111-1111-1111-111111111111"); // Sucursal Central
            role = "Administrator";
        }
        else if (username.Equals("cajero", StringComparison.OrdinalIgnoreCase) && password == "cajero123")
        {
            branchId = Guid.Parse("22222222-2222-2222-2222-222222222222"); // Sucursal Norte
            role = "Cashier";
        }
        else
        {
            return Task.FromResult<LoginResponseDto?>(null);
        }

        var response = GenerateTokens(username, role, branchId);
        return Task.FromResult<LoginResponseDto?>(response);
    }

    public Task<LoginResponseDto?> RefreshTokenAsync(string token, string refreshToken)
    {
        // Validar si el refresh token existe y es válido
        if (!RefreshTokens.TryGetValue(refreshToken, out var username))
        {
            return Task.FromResult<LoginResponseDto?>(null);
        }

        // Obtener el principal a partir del token expirado sin validar tiempo de expiración
        var tokenHandler = new JwtSecurityTokenHandler();
        SecurityToken securityToken;
        
        var validationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = false, // Ignorar expiración
            ValidateIssuerSigningKey = true,
            ValidIssuer = _jwtSettings.Issuer,
            ValidAudience = _jwtSettings.Audience,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_jwtSettings.Secret))
        };

        try
        {
            var principal = tokenHandler.ValidateToken(token, validationParameters, out securityToken);
            if (securityToken is not JwtSecurityToken jwtSecurityToken || 
                !jwtSecurityToken.Header.Alg.Equals(SecurityAlgorithms.HmacSha256, StringComparison.InvariantCultureIgnoreCase))
            {
                return Task.FromResult<LoginResponseDto?>(null);
            }

            var role = principal.FindFirst(ClaimTypes.Role)?.Value ?? "User";
            var branchIdClaim = principal.FindFirst("branch_id")?.Value;
            var branchId = branchIdClaim != null ? Guid.Parse(branchIdClaim) : Guid.Empty;

            // Revocar el token anterior
            RefreshTokens.TryRemove(refreshToken, out _);

            // Generar nuevos tokens
            var response = GenerateTokens(username, role, branchId);
            return Task.FromResult<LoginResponseDto?>(response);
        }
        catch
        {
            return Task.FromResult<LoginResponseDto?>(null);
        }
    }

    private LoginResponseDto GenerateTokens(string username, string role, Guid branchId)
    {
        var tokenHandler = new JwtSecurityTokenHandler();
        var key = Encoding.UTF8.GetBytes(_jwtSettings.Secret);
        var expiration = DateTime.UtcNow.AddMinutes(_jwtSettings.ExpiryMinutes);

        var tokenDescriptor = new SecurityTokenDescriptor
        {
            Subject = new ClaimsIdentity(new[]
            {
                new Claim(ClaimTypes.Name, username),
                new Claim(ClaimTypes.Role, role),
                new Claim("branch_id", branchId.ToString())
            }),
            Expires = expiration,
            Issuer = _jwtSettings.Issuer,
            Audience = _jwtSettings.Audience,
            SigningCredentials = new SigningCredentials(new SymmetricSecurityKey(key), SecurityAlgorithms.HmacSha256Signature)
        };

        var token = tokenHandler.CreateToken(tokenDescriptor);
        var tokenString = tokenHandler.WriteToken(token);

        // Generar un Refresh Token simple de tipo GUID
        var refreshToken = Guid.NewGuid().ToString();
        RefreshTokens.TryAdd(refreshToken, username);

        return new LoginResponseDto(tokenString, refreshToken, expiration, username, branchId.ToString());
    }
}
