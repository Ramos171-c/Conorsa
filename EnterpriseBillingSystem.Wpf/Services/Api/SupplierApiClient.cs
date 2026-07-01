using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading.Tasks;
using EnterpriseBillingSystem.Wpf.Models;

namespace EnterpriseBillingSystem.Wpf.Services.Api;

public class SupplierApiClient
{
    private readonly HttpClient _httpClient;

    public SupplierApiClient(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public async Task<PagedResult<SupplierDto>?> GetSuppliersPagedAsync(int page, int pageSize, string? search = null, Guid? categoryId = null, string? status = null)
    {
        var url = $"suppliers?pageNumber={page}&pageSize={pageSize}";
        if (!string.IsNullOrWhiteSpace(search))
        {
            url += $"&search={Uri.EscapeDataString(search)}";
        }
        if (categoryId.HasValue && categoryId.Value != Guid.Empty)
        {
            url += $"&categoryId={categoryId.Value}";
        }
        if (!string.IsNullOrWhiteSpace(status))
        {
            url += $"&status={Uri.EscapeDataString(status)}";
        }
        return await _httpClient.GetFromJsonAsync<PagedResult<SupplierDto>>(url);
    }

    public async Task<SupplierDetailDto?> GetSupplierByIdAsync(Guid id)
    {
        return await _httpClient.GetFromJsonAsync<SupplierDetailDto>($"suppliers/{id}");
    }

    public async Task<Guid> CreateSupplierAsync(CreateSupplierCommandDto command)
    {
        var response = await _httpClient.PostAsJsonAsync("suppliers", command);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<Guid>();
    }

    public async Task<List<SupplierCategoryDto>> GetSupplierCategoriesAsync()
    {
        var response = await _httpClient.GetFromJsonAsync<List<SupplierCategoryDto>>("suppliers/categories");
        return response ?? new List<SupplierCategoryDto>();
    }
}
