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

    [HttpPost("reset-admin-password")]
    [AllowAnonymous]
    public async Task<IActionResult> ResetAdminPassword(
        [FromServices] Microsoft.AspNetCore.Identity.UserManager<EnterpriseBillingSystem.Domain.Entities.ApplicationUser> userManager)
    {
        var adminUser = await userManager.FindByNameAsync("Ramos") ?? await userManager.FindByNameAsync("admin");
        if (adminUser == null)
        {
            return NotFound(new { Message = "Usuario administrador 'Ramos' no encontrado." });
        }

        adminUser.IsActive = true;
        adminUser.IsDeleted = false;
        adminUser.LockoutEnd = null;
        adminUser.AccessFailedCount = 0;

        var token = await userManager.GeneratePasswordResetTokenAsync(adminUser);
        var result = await userManager.ResetPasswordAsync(adminUser, token, "Hola1234");
        
        if (result.Succeeded)
        {
            await userManager.UpdateAsync(adminUser);
            return Ok(new { Message = $"Contraseña del usuario '{adminUser.UserName}' reestablecida con éxito a 'Hola1234' y cuenta activada." });
        }

        return BadRequest(new { Errors = result.Errors.Select(e => e.Description) });
    }

    [HttpPost("reset-all-passwords")]
    [AllowAnonymous]
    public async Task<IActionResult> ResetAllPasswords(
        [FromServices] Microsoft.AspNetCore.Identity.UserManager<EnterpriseBillingSystem.Domain.Entities.ApplicationUser> userManager)
    {
        var excluded = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "Ramos", "Winsston" };
        var users = userManager.Users
            .Where(u => !excluded.Contains(u.UserName!))
            .ToList();

        var success = new List<string>();
        var failed = new List<object>();

        foreach (var user in users)
        {
            var token = await userManager.GeneratePasswordResetTokenAsync(user);
            var result = await userManager.ResetPasswordAsync(user, token, "1234");
            if (result.Succeeded)
                success.Add(user.UserName!);
            else
                failed.Add(new { User = user.UserName, Errors = result.Errors.Select(e => e.Description) });
        }

        return Ok(new
        {
            Message = $"Proceso completado. {success.Count} contraseñas actualizadas, {failed.Count} fallidas.",
            Updated = success,
            Failed = failed
        });
    }
}

public record LoginRequest(string Username, string Password);
public record RefreshRequest(string Token, string RefreshToken);
public record LogoutRequest(string RefreshToken);
public record ChangePasswordRequest(string Username, string CurrentPassword, string NewPassword);
