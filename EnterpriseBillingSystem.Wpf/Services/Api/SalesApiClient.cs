using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading.Tasks;
using EnterpriseBillingSystem.Wpf.Models;

namespace EnterpriseBillingSystem.Wpf.Services.Api;

public class SalesApiClient
{
    private readonly HttpClient _httpClient;

    public SalesApiClient(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public async Task<PagedResult<SalesOrderListItemDto>?> GetSalesOrdersPagedAsync(
        int page, 
        int pageSize, 
        Guid? customerId = null, 
        string? status = null,
        DateTime? fromDate = null,
        DateTime? toDate = null)
    {
        var url = $"sales-orders?pageNumber={page}&pageSize={pageSize}";
        if (customerId.HasValue)
        {
            url += $"&customerId={customerId.Value}";
        }
        if (!string.IsNullOrWhiteSpace(status))
        {
            url += $"&status={Uri.EscapeDataString(status)}";
        }
        if (fromDate.HasValue)
        {
            url += $"&fromDate={fromDate.Value:yyyy-MM-ddTHH:mm:ss}";
        }
        if (toDate.HasValue)
        {
            url += $"&toDate={toDate.Value:yyyy-MM-ddTHH:mm:ss}";
        }
        return await _httpClient.GetFromJsonAsync<PagedResult<SalesOrderListItemDto>>(url);
    }

    public async Task<SalesOrderDetailDto?> GetSalesOrderByIdAsync(Guid id)
    {
        return await _httpClient.GetFromJsonAsync<SalesOrderDetailDto>($"sales-orders/{id}");
    }

    public async Task<bool> ConfirmSalesOrderAsync(Guid id)
    {
        var response = await _httpClient.PostAsync($"sales-orders/{id}/confirm", null);
        return response.IsSuccessStatusCode;
    }

    public async Task<bool> CancelSalesOrderAsync(Guid id, string cancellationReason)
    {
        var command = new CancelSalesOrderCommandDto(id, cancellationReason);
        var response = await _httpClient.PostAsJsonAsync($"sales-orders/{id}/cancel", command);
        return response.IsSuccessStatusCode;
    }

    public async Task<List<ConsolidatedProductDto>> GetConsolidatedProductsAsync(
        Guid? customerId = null, 
        string? status = null,
        DateTime? fromDate = null,
        DateTime? toDate = null)
    {
        var url = "sales-orders/consolidated-products?";
        if (customerId.HasValue)
        {
            url += $"customerId={customerId.Value}&";
        }
        if (!string.IsNullOrWhiteSpace(status))
        {
            url += $"status={Uri.EscapeDataString(status)}&";
        }
        if (fromDate.HasValue)
        {
            url += $"fromDate={fromDate.Value:yyyy-MM-ddTHH:mm:ss}&";
        }
        if (toDate.HasValue)
        {
            url += $"toDate={toDate.Value:yyyy-MM-ddTHH:mm:ss}&";
        }
        url = url.TrimEnd('&', '?');

        var response = await _httpClient.GetFromJsonAsync<List<ConsolidatedProductDto>>(url);
        return response ?? new List<ConsolidatedProductDto>();
    }

    public async Task<bool> UpdateSalesOrderStatusAsync(Guid id, int statusValue)
    {
        var response = await _httpClient.PutAsJsonAsync($"sales-orders/{id}/status", statusValue);
        return response.IsSuccessStatusCode;
    }

    public async Task<bool> ReturnSalesOrderAsync(Guid id, ReturnSalesOrderCommandDto command)
    {
        var response = await _httpClient.PostAsJsonAsync($"sales-orders/{id}/return", command);
        return response.IsSuccessStatusCode;
    }
}
