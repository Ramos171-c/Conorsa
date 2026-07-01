using System.Net.Http;

namespace EnterpriseBillingSystem.Wpf.Services.Api;

public class CashApiClient
{
    private readonly HttpClient _httpClient;

    public CashApiClient(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }
}
