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
            throw new Exception($"No se pudo conectar al servidor ({_httpClient.BaseAddress}). Verifique su conexión de red. Detalle: {ex.Message}", ex);
        }
        catch (Exception ex)
        {
            throw new Exception($"Error de conexión al autenticar: {ex.Message}", ex);
        }

        if (response.IsSuccessStatusCode)
        {
            return await response.Content.ReadFromJsonAsync<LoginResponse>();
        }

        var content = await response.Content.ReadAsStringAsync();
        var cleanMessage = content;

        try
        {
            if (!string.IsNullOrWhiteSpace(content) && content.Contains("{"))
            {
                using var doc = System.Text.Json.JsonDocument.Parse(content.Substring(content.IndexOf('{')));
                if (doc.RootElement.TryGetProperty("message", out var msgProp) || doc.RootElement.TryGetProperty("Message", out msgProp))
                {
                    cleanMessage = msgProp.GetString() ?? content;
                }
                else if (doc.RootElement.TryGetProperty("detail", out var detailProp))
                {
                    cleanMessage = detailProp.GetString() ?? content;
                }
            }
        }
        catch { /* Keep raw content if JSON parsing fails */ }

        if (string.IsNullOrWhiteSpace(cleanMessage))
        {
            cleanMessage = response.ReasonPhrase ?? "Respuesta vacía del servidor";
        }

        throw new Exception($"[(HTTP {(int)response.StatusCode})] {cleanMessage}");
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
