using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading;
using System.Threading.Tasks;
using EnterpriseBillingSystem.Wpf.Services.Authentication;

namespace EnterpriseBillingSystem.Wpf.Helpers;

public class JwtAuthHeaderHandler : DelegatingHandler
{
    private readonly CurrentUserService _currentUserService;

    public JwtAuthHeaderHandler(CurrentUserService currentUserService)
    {
        _currentUserService = currentUserService;
    }

    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        if (!string.IsNullOrEmpty(_currentUserService.Token))
        {
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _currentUserService.Token);
        }

        return await base.SendAsync(request, cancellationToken);
    }
}
