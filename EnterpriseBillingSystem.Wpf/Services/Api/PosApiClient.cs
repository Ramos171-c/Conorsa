using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json;
using System.Threading.Tasks;
using EnterpriseBillingSystem.Wpf.Models;

namespace EnterpriseBillingSystem.Wpf.Services.Api;

public class PosApiClient
{
    private readonly HttpClient _httpClient;

    public PosApiClient(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public async Task<List<ProductSearchResultDto>> SearchProductsAsync(string query)
    {
        var url = $"products?searchTerm={Uri.EscapeDataString(query)}&pageSize=20&isForPos=true";
        var response = await _httpClient.GetFromJsonAsync<PagedResult<ProductSearchResultDto>>(url);
        return response?.Items?.ToList() ?? new List<ProductSearchResultDto>();
    }

    public async Task<List<CustomerSearchResultDto>> SearchCustomersAsync(string query)
    {
        var url = $"customers?searchTerm={Uri.EscapeDataString(query)}&pageSize=20";
        var response = await _httpClient.GetFromJsonAsync<PagedResult<CustomerSearchResultDto>>(url);
        return response?.Items?.ToList() ?? new List<CustomerSearchResultDto>();
    }

    public async Task<ActiveCashSessionDto?> GetActiveCashSessionAsync(string username)
    {
        var url = "cash-sessions?status=Open&pageSize=100";
        var response = await _httpClient.GetFromJsonAsync<PagedResult<ActiveCashSessionDto>>(url);
        return response?.Items?.FirstOrDefault(s => s.OpenedByUserName.Equals(username, StringComparison.OrdinalIgnoreCase));
    }

    public async Task<List<PaymentMethodDto>> GetPaymentMethodsAsync()
    {
        var url = "payment-methods?pageSize=100";
        var response = await _httpClient.GetFromJsonAsync<PagedResult<PaymentMethodDto>>(url);
        return response?.Items?.Where(p => p.IsActive).ToList() ?? new List<PaymentMethodDto>();
    }

    public async Task<Guid> CreateInvoiceAsync(object command)
    {
        var response = await _httpClient.PostAsJsonAsync("sales-invoices", command);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<Guid>();
    }

    public async Task PostInvoiceAsync(Guid invoiceId)
    {
        var response = await _httpClient.PostAsync($"sales-invoices/{invoiceId}/post", null);
        response.EnsureSuccessStatusCode();
    }

    public async Task<Guid?> GetDefaultBranchWarehouseIdAsync()
    {
        try
        {
            var url = "Inventory/stock?pageSize=1";
            var response = await _httpClient.GetFromJsonAsync<JsonElement>(url);
            if (response.TryGetProperty("items", out var itemsProp) && itemsProp.ValueKind == JsonValueKind.Array && itemsProp.GetArrayLength() > 0)
            {
                var firstItem = itemsProp[0];
                if (firstItem.TryGetProperty("branchWarehouseId", out var idProp))
                {
                    return idProp.GetGuid();
                }
            }
        }
        catch
        {
            // Fallback or ignore
        }
        return null;
    }
}
