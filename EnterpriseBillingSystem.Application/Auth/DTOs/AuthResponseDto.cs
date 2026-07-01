using System;

namespace EnterpriseBillingSystem.Application.Auth.DTOs;

public record AuthResponseDto(
    string AccessToken,
    string RefreshToken,
    DateTime Expiration,
    string Username,
    bool RequiresPasswordChange
);
