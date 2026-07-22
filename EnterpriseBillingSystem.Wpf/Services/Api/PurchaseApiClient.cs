using System;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading.Tasks;
using EnterpriseBillingSystem.Wpf.Models;

namespace EnterpriseBillingSystem.Wpf.Services.Api;

public class PurchaseApiClient
{
    private readonly HttpClient _httpClient;

    public PurchaseApiClient(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public async Task<PagedResult<PurchaseReceiptListItemDto>?> GetPurchaseReceiptsPagedAsync(
        int page, 
        int pageSize, 
        Guid? supplierId = null, 
        string? status = null)
    {
        var url = $"purchase-receipts?pageNumber={page}&pageSize={pageSize}";
        if (supplierId.HasValue)
        {
            url += $"&supplierId={supplierId.Value}";
        }
        if (!string.IsNullOrWhiteSpace(status))
        {
            url += $"&status={Uri.EscapeDataString(status)}";
        }
        return await _httpClient.GetFromJsonAsync<PagedResult<PurchaseReceiptListItemDto>>(url);
    }

    public async Task<Guid> RegisterPurchaseReceiptAsync(RegisterPurchaseReceiptCommandDto command)
    {
        var response = await _httpClient.PostAsJsonAsync("purchase-receipts", command);
        if (!response.IsSuccessStatusCode)
        {
            var body = await response.Content.ReadAsStringAsync();
            string errorMessage = $"Error {(int)response.StatusCode}";
            try
            {
                using var jsonDoc = System.Text.Json.JsonDocument.Parse(body);
                if (jsonDoc.RootElement.TryGetProperty("message", out var msgProp))
                {
                    errorMessage = msgProp.GetString() ?? errorMessage;
                }
                else if (jsonDoc.RootElement.TryGetProperty("detail", out var detailProp))
                {
                    errorMessage = detailProp.GetString() ?? errorMessage;
                }
                else if (jsonDoc.RootElement.TryGetProperty("title", out var titleProp))
                {
                    errorMessage = titleProp.GetString() ?? errorMessage;
                }
            }
            catch {}
            throw new Exception(errorMessage);
        }
        return await response.Content.ReadFromJsonAsync<Guid>();
    }

    public async Task<PagedResult<PurchaseOrderListItemDto>?> GetPurchaseOrdersPagedAsync(
        int page, 
        int pageSize, 
        Guid? supplierId = null, 
        string? status = null)
    {
        var url = $"purchase-orders?pageNumber={page}&pageSize={pageSize}";
        if (supplierId.HasValue)
        {
            url += $"&supplierId={supplierId.Value}";
        }
        if (!string.IsNullOrWhiteSpace(status))
        {
            url += $"&status={Uri.EscapeDataString(status)}";
        }
        return await _httpClient.GetFromJsonAsync<PagedResult<PurchaseOrderListItemDto>>(url);
    }

    public async Task<PurchaseOrderDetailDto?> GetPurchaseOrderByIdAsync(Guid id)
    {
        return await _httpClient.GetFromJsonAsync<PurchaseOrderDetailDto>($"purchase-orders/{id}");
    }
}
