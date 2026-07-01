using System;

namespace EnterpriseBillingSystem.Wpf.Models;

public class LoginResponse
{
    public string AccessToken { get; set; } = string.Empty;
    public string RefreshToken { get; set; } = string.Empty;
    public DateTime Expiration { get; set; }
    public string Username { get; set; } = string.Empty;
    public bool RequiresPasswordChange { get; set; }
}
