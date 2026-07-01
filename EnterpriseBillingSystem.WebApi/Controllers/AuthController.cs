using System;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using EnterpriseBillingSystem.Application.Auth.Commands;
using EnterpriseBillingSystem.Application.Auth.Queries;
using EnterpriseBillingSystem.Application.Auth.DTOs;

namespace EnterpriseBillingSystem.WebApi.Controllers;

[Route("api/v1/auth")]
public class AuthController : ApiControllerBase
{
    [HttpPost("login")]
    public async Task<ActionResult<AuthResponseDto>> Login([FromBody] LoginRequest request)
    {
        var result = await Mediator.Send(new LoginCommand(request.Username, request.Password));
        if (result == null)
        {
            return Unauthorized(new { Message = "Credenciales incorrectas o sucursal inactiva." });
        }
        return Ok(result);
    }

    [HttpPost("refresh")]
    public async Task<ActionResult<AuthResponseDto>> Refresh([FromBody] RefreshRequest request)
    {
        var result = await Mediator.Send(new RefreshTokenCommand(request.Token, request.RefreshToken));
        if (result == null)
        {
            return BadRequest(new { Message = "Token de renovación inválido, expirado o reutilizado." });
        }
        return Ok(result);
    }

    [Authorize]
    [HttpPost("logout")]
    public async Task<IActionResult> Logout([FromBody] LogoutRequest request)
    {
        var result = await Mediator.Send(new LogoutCommand(request.RefreshToken));
        if (!result)
        {
            return BadRequest(new { Message = "No se pudo cerrar la sesión." });
        }
        return Ok(new { Message = "Sesión cerrada correctamente." });
    }

    [HttpPost("change-password")]
    public async Task<IActionResult> ChangePassword([FromBody] ChangePasswordRequest request)
    {
        var result = await Mediator.Send(new ChangePasswordCommand(request.Username, request.CurrentPassword, request.NewPassword));
        if (!result)
        {
            return BadRequest(new { Message = "No se pudo cambiar la contraseña. Verifique que la contraseña actual sea correcta y cumpla los requisitos de complejidad." });
        }
        return Ok(new { Message = "Contraseña cambiada correctamente." });
    }

    [Authorize]
    [HttpPost("revoke-all")]
    public async Task<IActionResult> RevokeAll()
    {
        var username = User.Identity?.Name;
        if (string.IsNullOrEmpty(username))
        {
            return Unauthorized();
        }

        var result = await Mediator.Send(new RevokeAllSessionsCommand(username));
        if (!result)
        {
            return BadRequest(new { Message = "No se pudieron revocar las sesiones." });
        }
        return Ok(new { Message = "Todas las sesiones activas han sido revocadas con éxito." });
    }

    [Authorize]
    [HttpGet("me")]
    public async Task<ActionResult<UserProfileDto>> GetMe()
    {
        var username = User.Identity?.Name;
        if (string.IsNullOrEmpty(username))
        {
            return Unauthorized();
        }

        var profile = await Mediator.Send(new GetCurrentUserProfileQuery(username));
        if (profile == null)
        {
            return NotFound(new { Message = "Perfil de usuario no encontrado." });
        }
        return Ok(profile);
    }
}

public record LoginRequest(string Username, string Password);
public record RefreshRequest(string Token, string RefreshToken);
public record LogoutRequest(string RefreshToken);
public record ChangePasswordRequest(string Username, string CurrentPassword, string NewPassword);
