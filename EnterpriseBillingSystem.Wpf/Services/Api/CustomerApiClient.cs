using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading.Tasks;
using EnterpriseBillingSystem.Wpf.Models;

namespace EnterpriseBillingSystem.Wpf.Services.Api;

public class CustomerApiClient
{
    private readonly HttpClient _httpClient;

    public CustomerApiClient(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public async Task<List<CustomerPricingProfileDto>> GetPricingProfilesAsync()
    {
        var response = await _httpClient.GetFromJsonAsync<List<CustomerPricingProfileDto>>("customers/pricing-profiles");
        return response ?? new List<CustomerPricingProfileDto>();
    }

    public async Task UpdatePricingProfileAsync(Guid customerId, Guid profileId)
    {
        var response = await _httpClient.PutAsJsonAsync($"customers/{customerId}/pricing-profile", new { CustomerId = customerId, PricingProfileId = profileId });
        response.EnsureSuccessStatusCode();
    }

    public async Task<PagedResult<CustomerDto>?> GetCustomersPagedAsync(int page, int pageSize, string? search = null, Guid? categoryId = null, CustomerStatus? status = null)
    {
        var url = $"customers?pageNumber={page}&pageSize={pageSize}";
        if (!string.IsNullOrWhiteSpace(search))
        {
            url += $"&searchTerm={Uri.EscapeDataString(search)}";
        }
        if (categoryId.HasValue && categoryId.Value != Guid.Empty)
        {
            url += $"&categoryId={categoryId.Value}";
        }
        if (status.HasValue)
        {
            url += $"&status={(int)status.Value}";
        }
        return await _httpClient.GetFromJsonAsync<PagedResult<CustomerDto>>(url);
    }

    public async Task<CustomerDto?> GetCustomerByIdAsync(Guid id)
    {
        return await _httpClient.GetFromJsonAsync<CustomerDto>($"customers/{id}");
    }

    public async Task<Guid> CreateCustomerAsync(CreateCustomerCommandDto command)
    {
        var response = await _httpClient.PostAsJsonAsync("customers", command);
        response.EnsureSuccessStatusCode();
        var result = await response.Content.ReadFromJsonAsync<Guid>();
        return result;
    }

    public async Task<bool> UpdateCustomerAsync(Guid id, UpdateCustomerCommandDto command)
    {
        var response = await _httpClient.PutAsJsonAsync($"customers/{id}", command);
        return response.IsSuccessStatusCode;
    }

    public async Task<bool> DeleteCustomerAsync(Guid id)
    {
        var response = await _httpClient.DeleteAsync($"customers/{id}");
        return response.IsSuccessStatusCode;
    }

    public async Task<bool> BlockCustomerAsync(Guid id)
    {
        var response = await _httpClient.PostAsync($"customers/{id}/block", null);
        return response.IsSuccessStatusCode;
    }

    public async Task<bool> ActivateCustomerAsync(Guid id)
    {
        var response = await _httpClient.PostAsync($"customers/{id}/activate", null);
        return response.IsSuccessStatusCode;
    }

    public async Task<List<CustomerCategoryDto>> GetCategoriesAsync()
    {
        var response = await _httpClient.GetFromJsonAsync<PagedResult<CustomerCategoryDto>>("customercategories?pageNumber=1&pageSize=100");
        return response?.Items?.ToList() ?? new List<CustomerCategoryDto>();
    }
}
