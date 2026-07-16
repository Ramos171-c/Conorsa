using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Threading;
using System.Threading.Tasks;
using EnterpriseBillingSystem.Wpf.Services.Authentication;
using EnterpriseBillingSystem.Wpf.Services.Storage;
using CommunityToolkit.Mvvm.Messaging;

namespace EnterpriseBillingSystem.Wpf.Helpers;

public class JwtAuthHeaderHandler : DelegatingHandler
{
    private readonly CurrentUserService _currentUserService;
    private readonly ILocalStorageService _localStorageService;

    public JwtAuthHeaderHandler(CurrentUserService currentUserService, ILocalStorageService localStorageService)
    {
        _currentUserService = currentUserService;
        _localStorageService = localStorageService;
    }

    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        if (!string.IsNullOrEmpty(_currentUserService.Token))
        {
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _currentUserService.Token);
        }

        HttpResponseMessage? response = null;
        System.Exception? lastException = null;
        int maxRetries = 3;

        for (int attempt = 1; attempt <= maxRetries; attempt++)
        {
            var reqToExecute = request;
            if (attempt > 1)
            {
                reqToExecute = await CloneHttpRequestMessageAsync(request);
            }

            try
            {
                response = await base.SendAsync(reqToExecute, cancellationToken);

                // If transient server status error, retry
                if (response.StatusCode == HttpStatusCode.RequestTimeout ||
                    response.StatusCode == HttpStatusCode.BadGateway ||
                    response.StatusCode == HttpStatusCode.ServiceUnavailable ||
                    response.StatusCode == HttpStatusCode.GatewayTimeout)
                {
                    if (attempt < maxRetries)
                    {
                        response.Dispose();
                        await Task.Delay(1000 * attempt, cancellationToken);
                        continue;
                    }
                }

                break; // Stop retrying on success or non-transient status code
            }
            catch (System.Exception ex) when (attempt < maxRetries && IsTransientException(ex))
            {
                lastException = ex;
                await Task.Delay(1000 * attempt, cancellationToken);
            }
        }

        if (response == null)
        {
            if (lastException != null)
            {
                throw new HttpRequestException("Error de conexión con el servidor tras varios intentos.", lastException);
            }
            throw new HttpRequestException("Error de conexión con el servidor tras varios intentos.");
        }

        // Si recibimos 401 (No autorizado/Token expirado), forzamos el cierre de sesión y redirección
        if (response.StatusCode == HttpStatusCode.Unauthorized && !string.IsNullOrEmpty(_currentUserService.Token))
        {
            var isLoginRequest = request.RequestUri?.AbsolutePath.Contains("auth/login", System.StringComparison.OrdinalIgnoreCase) ?? false;
            if (!isLoginRequest)
            {
                _currentUserService.Token = null;
                _currentUserService.CurrentUser = null;
                _currentUserService.Permissions.Clear();
                _currentUserService.BranchId = null;

                await _localStorageService.ClearAsync("token");

                // Redirigir a la ventana de Login
                CommunityToolkit.Mvvm.Messaging.WeakReferenceMessenger.Default.Send(new LogoutMessage());
            }
        }

        // Si recibimos 403, intentamos refrescar los permisos desde /me automáticamente
        // Esto evita que el usuario tenga que cerrar sesión cuando cambian los permisos en el servidor.
        if (response.StatusCode == HttpStatusCode.Forbidden && !string.IsNullOrEmpty(_currentUserService.Token))
        {
            await TryRefreshPermissionsAsync(request.RequestUri?.AbsolutePath, cancellationToken);
        }

        return response;
    }

    private static bool IsTransientException(System.Exception ex)
    {
        if (ex is HttpRequestException) return true;
        if (ex is TaskCanceledException) return true; // Timeout
        if (ex is System.IO.IOException) return true;
        if (ex is System.Net.Sockets.SocketException) return true;

        var inner = ex.InnerException;
        while (inner != null)
        {
            if (inner is System.Net.Sockets.SocketException ||
                inner is System.IO.IOException ||
                inner is HttpRequestException)
            {
                return true;
            }
            inner = inner.InnerException;
        }

        return false;
    }

    private static async Task<HttpRequestMessage> CloneHttpRequestMessageAsync(HttpRequestMessage req)
    {
        var clone = new HttpRequestMessage(req.Method, req.RequestUri)
        {
            Version = req.Version
        };

        // Copy content
        if (req.Content != null)
        {
            var ms = new System.IO.MemoryStream();
            await req.Content.CopyToAsync(ms);
            ms.Position = 0;
            clone.Content = new StreamContent(ms);

            foreach (var h in req.Content.Headers)
            {
                clone.Content.Headers.TryAddWithoutValidation(h.Key, h.Value);
            }
        }

        // Copy request headers
        foreach (var h in req.Headers)
        {
            clone.Headers.TryAddWithoutValidation(h.Key, h.Value);
        }

        // Copy options
        foreach (var prop in req.Options)
        {
            clone.Options.Set(new HttpRequestOptionsKey<object?>(prop.Key), prop.Value);
        }

        return clone;
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
