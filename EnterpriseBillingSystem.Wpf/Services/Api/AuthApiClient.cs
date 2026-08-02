using System;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading.Tasks;
using EnterpriseBillingSystem.Wpf.Models;

namespace EnterpriseBillingSystem.Wpf.Services.Api;

public class AuthApiClient
{
    private readonly HttpClient _httpClient;

    public AuthApiClient(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public async Task<LoginResponse?> LoginAsync(string username, string password)
    {
        HttpResponseMessage response;
        try
        {
            response = await _httpClient.PostAsJsonAsync("auth/login", new LoginRequest { Username = username, Password = password });
        }
        catch (HttpRequestException ex)
        {
            throw new Exception("No se pudo conectar al servidor. Verifique que la aplicación WebApi esté iniciada y el servicio SQL Server esté activo.", ex);
        }

        if (response.IsSuccessStatusCode)
        {
            return await response.Content.ReadFromJsonAsync<LoginResponse>();
        }

        if (response.StatusCode == System.Net.HttpStatusCode.Unauthorized || response.StatusCode == System.Net.HttpStatusCode.BadRequest)
        {
            throw new UnauthorizedAccessException("Usuario o contraseña incorrectos.");
        }

        throw new Exception($"Error del servidor: {(int)response.StatusCode} ({response.ReasonPhrase}). Verifique la conexión con la base de datos.");
    }

    public async Task<CurrentUserDto?> GetMeAsync()
    {
        var response = await _httpClient.GetAsync("auth/me");
        if (response.IsSuccessStatusCode)
        {
            return await response.Content.ReadFromJsonAsync<CurrentUserDto>();
        }
        return null;
    }
}
