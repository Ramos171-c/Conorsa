using System;
using System.Threading.Tasks;

namespace EnterpriseBillingSystem.Application.Common.Interfaces;

public interface IAuthService
{
    Task<LoginResponseDto?> LoginAsync(string username, string password);
    Task<LoginResponseDto?> RefreshTokenAsync(string token, string refreshToken);
}

public record LoginResponseDto(string Token, string RefreshToken, DateTime Expiration, string Username, string BranchId);
