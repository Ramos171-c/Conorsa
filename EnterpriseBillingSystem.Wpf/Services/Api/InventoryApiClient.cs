using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading.Tasks;
using EnterpriseBillingSystem.Wpf.Models;

namespace EnterpriseBillingSystem.Wpf.Services.Api;

public class InventoryApiClient
{
    private readonly HttpClient _httpClient;

    public InventoryApiClient(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public async Task<List<WarehouseDto>> GetWarehousesAsync()
    {
        var response = await _httpClient.GetFromJsonAsync<List<WarehouseDto>>("Inventory/warehouses");
        return response ?? new List<WarehouseDto>();
    }

    public async Task<Guid> ReceiveItemAsync(object command)
    {
        var response = await _httpClient.PostAsJsonAsync("Inventory/receive", command);
        if (!response.IsSuccessStatusCode)
        {
            var errorContent = await response.Content.ReadAsStringAsync();
            string errorMessage = $"Error {(int)response.StatusCode}";
            try
            {
                using var jsonDoc = System.Text.Json.JsonDocument.Parse(errorContent);
                if (jsonDoc.RootElement.TryGetProperty("message", out var msgProp))
                {
                    errorMessage = msgProp.GetString() ?? errorMessage;
                }
                else if (jsonDoc.RootElement.TryGetProperty("detail", out var detailProp))
                {
                    errorMessage = detailProp.GetString() ?? errorMessage;
                }
            }
            catch {}
            throw new Exception(errorMessage);
        }
        return await response.Content.ReadFromJsonAsync<Guid>();
    }

    public async Task<PagedResult<InventoryDto>?> GetStockInquiryAsync(Guid? branchWarehouseId, Guid? productId, int page, int pageSize)
    {
        var url = $"Inventory/stock?pageNumber={page}&pageSize={pageSize}";
        if (branchWarehouseId.HasValue)
        {
            url += $"&branchWarehouseId={branchWarehouseId.Value}";
        }
        if (productId.HasValue)
        {
            url += $"&productId={productId.Value}";
        }
        return await _httpClient.GetFromJsonAsync<PagedResult<InventoryDto>>(url);
    }

    public async Task<Guid> AdjustInventoryAsync(object command)
    {
        var response = await _httpClient.PostAsJsonAsync("Inventory/adjust", command);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<Guid>();
    }

    public async Task<Guid> TransferInventoryAsync(object command)
    {
        var response = await _httpClient.PostAsJsonAsync("Inventory/transfer", command);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<Guid>();
    }

    public async Task<PagedResult<KardexDto>?> GetKardexAsync(
        Guid branchWarehouseId, 
        Guid productId, 
        DateTime? startDate, 
        DateTime? endDate, 
        int page, 
        int pageSize)
    {
        var url = $"Inventory/kardex?branchWarehouseId={branchWarehouseId}&productId={productId}&pageNumber={page}&pageSize={pageSize}";
        if (startDate.HasValue)
        {
            url += $"&startDate={startDate.Value:yyyy-MM-dd}";
        }
        if (endDate.HasValue)
        {
            url += $"&endDate={endDate.Value:yyyy-MM-dd}";
        }
        return await _httpClient.GetFromJsonAsync<PagedResult<KardexDto>>(url);
    }

    public async Task<InventoryDashboardKpisDto?> GetDashboardKpisAsync()
    {
        return await _httpClient.GetFromJsonAsync<InventoryDashboardKpisDto>("Inventory/dashboard");
    }
}
