using System.Net.Http;

namespace EnterpriseBillingSystem.Wpf.Services.Api;

public class AccountsReceivableApiClient
{
    private readonly HttpClient _httpClient;

    public AccountsReceivableApiClient(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }
}
