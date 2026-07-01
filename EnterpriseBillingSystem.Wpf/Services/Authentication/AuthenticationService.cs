using System;
using System.Threading.Tasks;
using EnterpriseBillingSystem.Wpf.Models;
using EnterpriseBillingSystem.Wpf.Services.Api;
using EnterpriseBillingSystem.Wpf.Services.Storage;

namespace EnterpriseBillingSystem.Wpf.Services.Authentication;

public class AuthenticationService : IAuthenticationService
{
    private readonly AuthApiClient _authApiClient;
    private readonly ILocalStorageService _localStorageService;
    private readonly CurrentUserService _currentUserService;

    public AuthenticationService(
        AuthApiClient authApiClient,
        ILocalStorageService localStorageService,
        CurrentUserService currentUserService)
    {
        _authApiClient = authApiClient;
        _localStorageService = localStorageService;
        _currentUserService = currentUserService;
    }

    public async Task<LoginResponse?> LoginAsync(string username, string password)
    {
        var response = await _authApiClient.LoginAsync(username, password);
        if (response != null && !string.IsNullOrEmpty(response.AccessToken))
        {
            _currentUserService.Token = response.AccessToken;
            
            // Save token
            await _localStorageService.SaveAsync("token", response);

            // Fetch profile info
            var profile = await _authApiClient.GetMeAsync();
            if (profile != null)
            {
                _currentUserService.CurrentUser = profile;
                _currentUserService.Permissions = profile.Permissions;
                _currentUserService.BranchId = profile.DefaultBranchId;
            }
        }
        return response;
    }

    public async Task LogoutAsync()
    {
        await _localStorageService.ClearAsync("token");
        _currentUserService.Token = null;
        _currentUserService.CurrentUser = null;
        _currentUserService.Permissions.Clear();
        _currentUserService.BranchId = null;
    }

    public bool IsAuthenticated()
    {
        return !string.IsNullOrEmpty(_currentUserService.Token);
    }

    public async Task<bool> AutoLoginAsync()
    {
        var savedTokenResponse = await _localStorageService.GetAsync<LoginResponse>("token");
        if (savedTokenResponse != null && !string.IsNullOrEmpty(savedTokenResponse.AccessToken))
        {
            // Set token temporarily to make the me call
            _currentUserService.Token = savedTokenResponse.AccessToken;

            try
            {
                var profile = await _authApiClient.GetMeAsync();
                if (profile != null)
                {
                    _currentUserService.CurrentUser = profile;
                    _currentUserService.Permissions = profile.Permissions;
                    _currentUserService.BranchId = profile.DefaultBranchId;
                    return true;
                }
            }
            catch
            {
                // Failed to verify session
            }
            
            // If verification failed, clear
            await LogoutAsync();
        }
        return false;
    }
}
