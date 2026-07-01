using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading.Tasks;
using EnterpriseBillingSystem.Wpf.Models;

namespace EnterpriseBillingSystem.Wpf.Services.Api;

public class UserApiClient
{
    private readonly HttpClient _httpClient;

    public UserApiClient(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public async Task<PagedResult<UserDto>?> GetUsersPagedAsync(int page, int pageSize, string? searchTerm = null)
    {
        var url = $"users?pageNumber={page}&pageSize={pageSize}";
        if (!string.IsNullOrWhiteSpace(searchTerm))
        {
            url += $"&searchTerm={Uri.EscapeDataString(searchTerm)}";
        }
        return await _httpClient.GetFromJsonAsync<PagedResult<UserDto>>(url);
    }

    public async Task<Guid> CreateUserAsync(CreateUserCommandDto command)
    {
        var response = await _httpClient.PostAsJsonAsync("users", command);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<Guid>();
    }

    public async Task<bool> UpdateUserAsync(Guid id, UpdateUserCommandDto command)
    {
        var response = await _httpClient.PutAsJsonAsync($"users/{id}", command);
        if (!response.IsSuccessStatusCode)
        {
            var body = await response.Content.ReadAsStringAsync();
            throw new Exception($"Error {(int)response.StatusCode}: {(string.IsNullOrWhiteSpace(body) ? response.ReasonPhrase : body)}");
        }
        return true;
    }

    public async Task<List<BranchLookupDto>> GetBranchesAsync()
    {
        var response = await _httpClient.GetFromJsonAsync<List<BranchLookupDto>>("system/branches");
        return response ?? new List<BranchLookupDto>();
    }

    public async Task<List<RouteLookupDto>> GetRoutesAsync()
    {
        var response = await _httpClient.GetFromJsonAsync<List<RouteLookupDto>>("routes");
        return response ?? new List<RouteLookupDto>();
    }
}

public record BranchLookupDto(Guid Id, string Code, string Name);
public record RouteLookupDto(Guid Id, string Code, string Name);
