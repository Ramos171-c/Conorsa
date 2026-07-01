using System.Net.Http;

namespace EnterpriseBillingSystem.Wpf.Services.Api;

public class AccountsPayableApiClient
{
    private readonly HttpClient _httpClient;

    public AccountsPayableApiClient(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }
}
