using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading.Tasks;

namespace EnterpriseBillingSystem.Wpf.Services.Api;

public class AdministrationApiClient
{
    private readonly HttpClient _httpClient;

    public AdministrationApiClient(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    // ─── CURRENCIES ───────────────────────────────────────────────────────────
    public async Task<List<CurrencyDto>> GetCurrenciesAsync()
    {
        var response = await _httpClient.GetFromJsonAsync<List<CurrencyDto>>("currencies");
        return response ?? new List<CurrencyDto>();
    }

    public async Task<Guid> CreateCurrencyAsync(CreateCurrencyCommandDto command)
    {
        var response = await _httpClient.PostAsJsonAsync("currencies", command);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<Guid>();
    }

    public async Task<bool> UpdateCurrencyAsync(Guid id, UpdateCurrencyCommandDto command)
    {
        var response = await _httpClient.PutAsJsonAsync($"currencies/{id}", command);
        return response.IsSuccessStatusCode;
    }

    public async Task<bool> DeleteCurrencyAsync(Guid id)
    {
        var response = await _httpClient.DeleteAsync($"currencies/{id}");
        return response.IsSuccessStatusCode;
    }

    // ─── PRICING THRESHOLDS ────────────────────────────────────────────────────
    public async Task<List<PricingThresholdDto>> GetPricingThresholdsAsync()
    {
        var response = await _httpClient.GetFromJsonAsync<List<PricingThresholdDto>>("pricing-thresholds");
        return response ?? new List<PricingThresholdDto>();
    }

    public async Task<bool> UpdatePricingThresholdsAsync(UpdatePricingThresholdsCommandDto command)
    {
        var response = await _httpClient.PutAsJsonAsync("pricing-thresholds", command);
        return response.IsSuccessStatusCode;
    }

    // ─── SALES GOALS ──────────────────────────────────────────────────────────
    public async Task<List<SalesGoalDto>> GetSalesGoalsAsync(Guid? userId = null)
    {
        var url = "sales-goals";
        if (userId.HasValue)
        {
            url += $"?userId={userId.Value}";
        }
        var response = await _httpClient.GetFromJsonAsync<List<SalesGoalDto>>(url);
        return response ?? new List<SalesGoalDto>();
    }

    public async Task<Guid> CreateSalesGoalAsync(CreateSalesGoalCommandDto command)
    {
        var response = await _httpClient.PostAsJsonAsync("sales-goals", command);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<Guid>();
    }

    public async Task<bool> UpdateSalesGoalAsync(Guid id, UpdateSalesGoalCommandDto command)
    {
        var response = await _httpClient.PutAsJsonAsync($"sales-goals/{id}", command);
        return response.IsSuccessStatusCode;
    }

    public async Task<bool> DeleteSalesGoalAsync(Guid id)
    {
        var response = await _httpClient.DeleteAsync($"sales-goals/{id}");
        return response.IsSuccessStatusCode;
    }

    // ─── SYSTEM PARAMETERS ─────────────────────────────────────────────────────
    public async Task<string> GetSystemParameterAsync(string key)
    {
        try
        {
            var response = await _httpClient.GetStringAsync($"system/parameters/{key}");
            return response;
        }
        catch
        {
            return string.Empty;
        }
    }

    public async Task<bool> UpdateSystemParameterAsync(string key, string value)
    {
        var response = await _httpClient.PutAsJsonAsync($"system/parameters/{key}", new { Value = value });
        return response.IsSuccessStatusCode;
    }
}

// ─── DTOS ─────────────────────────────────────────────────────────────────────

public record CurrencyDto(
    Guid Id,
    string Code,
    string Name,
    string Symbol,
    decimal ExchangeRate,
    bool IsDefault,
    bool IsActive
);

public record CreateCurrencyCommandDto(
    string Code,
    string Name,
    string Symbol,
    decimal ExchangeRate,
    bool IsDefault,
    bool IsActive
);

public record UpdateCurrencyCommandDto(
    Guid Id,
    string Code,
    string Name,
    string Symbol,
    decimal ExchangeRate,
    bool IsDefault,
    bool IsActive
);

public record PricingThresholdDto(
    Guid Id,
    string LevelName,
    decimal MinimumSubtotal,
    bool IsActive
);

public record ThresholdUpdateInputDto(
    Guid Id,
    decimal MinimumSubtotal,
    bool IsActive
);

public record UpdatePricingThresholdsCommandDto(
    List<ThresholdUpdateInputDto> Thresholds
);

public record SalesGoalDto(
    Guid Id,
    Guid UserId,
    string UserName,
    string UserFullName,
    string PeriodName,
    decimal TargetAmount,
    decimal CurrentAmount,
    double ProgressPercentage,
    decimal RemainingAmount,
    DateTime StartDate,
    DateTime EndDate,
    bool IsActive
);

public record CreateSalesGoalCommandDto(
    Guid UserId,
    string PeriodName,
    decimal TargetAmount,
    DateTime StartDate,
    DateTime EndDate,
    bool IsActive
);

public record UpdateSalesGoalCommandDto(
    Guid Id,
    Guid UserId,
    string PeriodName,
    decimal TargetAmount,
    DateTime StartDate,
    DateTime EndDate,
    bool IsActive
);
