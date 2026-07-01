using System.Net.Http;

namespace EnterpriseBillingSystem.Wpf.Services.Api;

public class AccountingApiClient
{
    private readonly HttpClient _httpClient;

    public AccountingApiClient(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }
}
