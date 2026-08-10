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
        var response = await _httpClient.PostAsJsonAsync("auth/login", new LoginRequest { Username = username, Password = password });
        if (response.IsSuccessStatusCode)
        {
            return await response.Content.ReadFromJsonAsync<LoginResponse>();
        }

        var errorContent = await response.Content.ReadAsStringAsync();
        if (!string.IsNullOrWhiteSpace(errorContent))
        {
            // Intentar desentrañar JSON de error si viene en formato { message: "..." }
            try
            {
                using var doc = System.Text.Json.JsonDocument.Parse(errorContent);
                if (doc.RootElement.TryGetProperty("message", out var msgProp))
                {
                    throw new HttpRequestException(msgProp.GetString());
                }
                if (doc.RootElement.TryGetProperty("detail", out var detailProp))
                {
                    throw new HttpRequestException(detailProp.GetString());
                }
            }
            catch (System.Text.Json.JsonException)
            {
                // No es JSON, usar el texto directo
            }
            throw new HttpRequestException(errorContent.Length > 150 ? errorContent.Substring(0, 150) + "..." : errorContent);
        }

        throw new HttpRequestException($"No se pudo iniciar sesión. (Código HTTP {response.StatusCode})");
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
