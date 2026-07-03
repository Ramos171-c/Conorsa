using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Threading.Tasks;
using EnterpriseBillingSystem.Wpf.Models;

namespace EnterpriseBillingSystem.Wpf.Services.Api;

public class ProductApiClient
{
    private readonly HttpClient _httpClient;

    public ProductApiClient(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public async Task<PagedResult<ProductDto>?> GetProductsPagedAsync(int page, int pageSize, string? term = null, Guid? categoryId = null)
    {
        var url = $"products?pageNumber={page}&pageSize={pageSize}";
        if (!string.IsNullOrWhiteSpace(term))
        {
            url += $"&searchTerm={Uri.EscapeDataString(term)}";
        }
        if (categoryId.HasValue)
        {
            url += $"&categoryId={categoryId.Value}";
        }
        return await _httpClient.GetFromJsonAsync<PagedResult<ProductDto>>(url);
    }

    public async Task<ProductDto?> GetProductByIdAsync(Guid id)
    {
        return await _httpClient.GetFromJsonAsync<ProductDto>($"products/{id}");
    }

    public async Task<Guid> CreateProductAsync(object command)
    {
        var response = await _httpClient.PostAsJsonAsync("products", command);
        if (!response.IsSuccessStatusCode)
        {
            var body = await response.Content.ReadAsStringAsync();
            throw new Exception($"Error {(int)response.StatusCode}: {(string.IsNullOrWhiteSpace(body) ? response.ReasonPhrase : body)}");
        }
        return await response.Content.ReadFromJsonAsync<Guid>();
    }

    public async Task<bool> UpdateProductAsync(Guid id, object command)
    {
        var response = await _httpClient.PutAsJsonAsync($"products/{id}", command);
        if (!response.IsSuccessStatusCode)
        {
            var body = await response.Content.ReadAsStringAsync();
            throw new Exception($"Error {(int)response.StatusCode}: {(string.IsNullOrWhiteSpace(body) ? response.ReasonPhrase : body)}");
        }
        return true;
    }

    public async Task<PagedResult<CategoryDto>?> GetCategoriesAsync(int page = 1, int pageSize = 1000)
    {
        return await _httpClient.GetFromJsonAsync<PagedResult<CategoryDto>>($"categories?pageNumber={page}&pageSize={pageSize}");
    }

    public async Task<PagedResult<TaxDto>?> GetTaxesAsync(int page = 1, int pageSize = 1000)
    {
        return await _httpClient.GetFromJsonAsync<PagedResult<TaxDto>>($"taxes?pageNumber={page}&pageSize={pageSize}");
    }

    public async Task<PagedResult<UnitOfMeasureDto>?> GetUnitsOfMeasureAsync(int page = 1, int pageSize = 1000)
    {
        return await _httpClient.GetFromJsonAsync<PagedResult<UnitOfMeasureDto>>($"unitsofmeasure?pageNumber={page}&pageSize={pageSize}");
    }

    public async Task<List<ProductPriceHistoryDto>> GetPriceHistoryAsync(Guid id)
    {
        var result = await _httpClient.GetFromJsonAsync<List<ProductPriceHistoryDto>>($"products/{id}/price-history");
        return result ?? new List<ProductPriceHistoryDto>();
    }

    public async Task<List<LowStockProductDto>> GetLowStockProductsAsync()
    {
        var result = await _httpClient.GetFromJsonAsync<List<LowStockProductDto>>("products/low-stock");
        return result ?? new List<LowStockProductDto>();
    }

    public async Task<string> UploadImageAsync(Guid id, byte[] fileBytes, string fileName)
    {
        using var content = new MultipartFormDataContent();
        var fileContent = new ByteArrayContent(fileBytes);
        fileContent.Headers.ContentType = MediaTypeHeaderValue.Parse("image/jpeg"); // Fallback
        if (fileName.EndsWith(".png", StringComparison.OrdinalIgnoreCase))
            fileContent.Headers.ContentType = MediaTypeHeaderValue.Parse("image/png");
        else if (fileName.EndsWith(".gif", StringComparison.OrdinalIgnoreCase))
            fileContent.Headers.ContentType = MediaTypeHeaderValue.Parse("image/gif");
        else if (fileName.EndsWith(".webp", StringComparison.OrdinalIgnoreCase))
            fileContent.Headers.ContentType = MediaTypeHeaderValue.Parse("image/webp");

        content.Add(fileContent, "file", fileName);

        var response = await _httpClient.PostAsync($"products/{id}/image", content);
        response.EnsureSuccessStatusCode();

        var result = await response.Content.ReadFromJsonAsync<ImageUploadResult>();
        return result?.ImageUrl ?? string.Empty;
    }

    public async Task<bool> DeleteImageAsync(Guid id)
    {
        var response = await _httpClient.DeleteAsync($"products/{id}/image");
        return response.IsSuccessStatusCode;
    }

    public async Task<List<ProductPresentationDto>> GetPresentationsAsync(Guid id)
    {
        var result = await _httpClient.GetFromJsonAsync<List<ProductPresentationDto>>($"products/{id}/presentations");
        return result ?? new List<ProductPresentationDto>();
    }

    public async Task<Guid> AddPresentationAsync(Guid id, ProductPresentationInputDto input)
    {
        var response = await _httpClient.PostAsJsonAsync($"products/{id}/presentations", input);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<Guid>();
    }

    public async Task<bool> UpdatePresentationAsync(Guid presentationId, ProductPresentationInputDto input)
    {
        var response = await _httpClient.PutAsJsonAsync($"products/presentations/{presentationId}", input);
        return response.IsSuccessStatusCode;
    }

    public async Task<bool> DeletePresentationAsync(Guid presentationId)
    {
        var response = await _httpClient.DeleteAsync($"products/presentations/{presentationId}");
        return response.IsSuccessStatusCode;
    }

    public async Task<ProductPresentationDto?> GetPresentationByBarcodeAsync(string barcode)
    {
        try
        {
            var response = await _httpClient.GetAsync($"products/presentations/barcode/{barcode}");
            if (response.StatusCode == System.Net.HttpStatusCode.NotFound) return null;
            response.EnsureSuccessStatusCode();
            return await response.Content.ReadFromJsonAsync<ProductPresentationDto>();
        }
        catch
        {
            return null;
        }
    }

    public async Task<List<CustomerPricingTierDto>> GetPricingTiersAsync(Guid? branchId = null)
    {
        var url = "products/pricing-tiers";
        if (branchId.HasValue)
        {
            url += $"?branchId={branchId.Value}";
        }
        var result = await _httpClient.GetFromJsonAsync<List<CustomerPricingTierDto>>(url);
        return result ?? new List<CustomerPricingTierDto>();
    }

    private class ImageUploadResult
    {
        public string ImageUrl { get; set; } = string.Empty;
    }
}
