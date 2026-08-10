using System;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using EnterpriseBillingSystem.Wpf.Services.Authentication;
using EnterpriseBillingSystem.Wpf.Services.Dialogs;

namespace EnterpriseBillingSystem.Wpf.ViewModels;

public partial class LoginViewModel : ViewModelBase
{
    private readonly IAuthenticationService _authService;
    private readonly INotificationService _notificationService;

    [ObservableProperty]
    private string _username = string.Empty;

    [ObservableProperty]
    private string _password = string.Empty;

    [ObservableProperty]
    private bool _isBusy;

    [ObservableProperty]
    private string? _errorMessage;

    public LoginViewModel(IAuthenticationService authService, INotificationService notificationService)
    {
        _authService = authService;
        _notificationService = notificationService;
    }

    public event Action? LoginSuccess;

    [RelayCommand]
    private async Task LoginAsync()
    {
        if (string.IsNullOrWhiteSpace(Username))
        {
            ErrorMessage = "El usuario es requerido.";
            _notificationService.ShowWarning(ErrorMessage);
            return;
        }

        if (string.IsNullOrWhiteSpace(Password))
        {
            ErrorMessage = "La contraseña es requerida.";
            _notificationService.ShowWarning(ErrorMessage);
            return;
        }

        ErrorMessage = null;
        IsBusy = true;

        try
        {
            var result = await _authService.LoginAsync(Username, Password);
            if (result != null)
            {
                _notificationService.ShowSuccess("¡Sesión iniciada con éxito!");
                LoginSuccess?.Invoke();
            }
            else
            {
                ErrorMessage = "Credenciales incorrectas o usuario no encontrado.";
                _notificationService.ShowError(ErrorMessage);
            }
        }
        catch (System.Net.Http.HttpRequestException ex)
        {
            ErrorMessage = string.IsNullOrWhiteSpace(ex.Message) ? "Error al autenticar con el servidor." : ex.Message;
            _notificationService.ShowError(ErrorMessage);
        }
        catch (Exception ex)
        {
            ErrorMessage = $"Error de conexión con el servidor: {ex.Message}";
            _notificationService.ShowError(ErrorMessage);
        }
        finally
        {
            IsBusy = false;
        }
    }
}
