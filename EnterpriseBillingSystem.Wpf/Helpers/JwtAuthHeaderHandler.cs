using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Threading;
using System.Threading.Tasks;
using EnterpriseBillingSystem.Wpf.Services.Authentication;

namespace EnterpriseBillingSystem.Wpf.Helpers;

public class JwtAuthHeaderHandler : DelegatingHandler
{
    private readonly CurrentUserService _currentUserService;

    public JwtAuthHeaderHandler(CurrentUserService currentUserService)
    {
        _currentUserService = currentUserService;
    }

    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        if (!string.IsNullOrEmpty(_currentUserService.Token))
        {
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _currentUserService.Token);
        }

        var response = await base.SendAsync(request, cancellationToken);

        // Si recibimos 403, intentamos refrescar los permisos desde /me automáticamente
        // Esto evita que el usuario tenga que cerrar sesión cuando cambian los permisos en el servidor.
        if (response.StatusCode == HttpStatusCode.Forbidden && !string.IsNullOrEmpty(_currentUserService.Token))
        {
            await TryRefreshPermissionsAsync(request.RequestUri?.AbsolutePath, cancellationToken);
        }

        return response;
    }

    private async Task TryRefreshPermissionsAsync(string? requestPath, CancellationToken cancellationToken)
    {
        // Evitar recursión infinita si /me mismo da 403
        if (requestPath != null && requestPath.Contains("auth/me", System.StringComparison.OrdinalIgnoreCase))
            return;

        try
        {
            // Llamar directamente a /me con el token actual para refrescar permisos
            using var meRequest = new HttpRequestMessage(HttpMethod.Get, "api/v1/auth/me");
            meRequest.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _currentUserService.Token);

            var meResponse = await base.SendAsync(meRequest, cancellationToken);
            if (meResponse.IsSuccessStatusCode)
            {
                var profile = await meResponse.Content.ReadFromJsonAsync<UserProfileDto>(cancellationToken: cancellationToken);
                if (profile != null)
                {
                    _currentUserService.Permissions = profile.Permissions;
                    _currentUserService.BranchId = profile.DefaultBranchId;
                }
            }
        }
        catch
        {
            // Silenciar errores de refresco — la operación original ya retornó su respuesta
        }
    }
}

// DTO mínimo para leer el perfil del usuario desde /me
internal record UserProfileDto(
    System.Guid Id,
    string Username,
    string Email,
    string FirstName,
    string LastName,
    string Role,
    System.Guid DefaultBranchId,
    System.Collections.Generic.List<string> Permissions
);
