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
        response.EnsureSuccessStatusCode();
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
