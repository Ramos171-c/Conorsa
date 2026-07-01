using System.Net.Http;

namespace EnterpriseBillingSystem.Wpf.Services.Api;

public class TreasuryApiClient
{
    private readonly HttpClient _httpClient;

    public TreasuryApiClient(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }
}
