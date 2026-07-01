using System.Threading.Tasks;
using EnterpriseBillingSystem.Wpf.Models;

namespace EnterpriseBillingSystem.Wpf.Services.Authentication;

public interface IAuthenticationService
{
    Task<LoginResponse?> LoginAsync(string username, string password);
    Task LogoutAsync();
    bool IsAuthenticated();
    Task<bool> AutoLoginAsync();
}
