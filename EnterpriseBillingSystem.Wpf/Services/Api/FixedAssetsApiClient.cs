using System.Net.Http;

namespace EnterpriseBillingSystem.Wpf.Services.Api;

public class FixedAssetsApiClient
{
    private readonly HttpClient _httpClient;

    public FixedAssetsApiClient(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }
}
