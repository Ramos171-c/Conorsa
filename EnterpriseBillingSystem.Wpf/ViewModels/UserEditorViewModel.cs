using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using EnterpriseBillingSystem.Wpf.Models;
using EnterpriseBillingSystem.Wpf.Services.Api;
using EnterpriseBillingSystem.Wpf.Services.Dialogs;

namespace EnterpriseBillingSystem.Wpf.ViewModels;

public partial class UserEditorViewModel : ViewModelBase
{
    private readonly UserApiClient _userApiClient;
    private readonly INotificationService _notificationService;
    private readonly UserDto? _userToEdit;

    public event Action? RequestClose;

    [ObservableProperty]
    private string _username = string.Empty;

    [ObservableProperty]
    private string _password = string.Empty;

    [ObservableProperty]
    private string? _email = string.Empty;

    [ObservableProperty]
    private string _firstName = string.Empty;

    [ObservableProperty]
    private string _lastName = string.Empty;

    [ObservableProperty]
    private Guid _selectedBranchId;

    [ObservableProperty]
    private string _selectedRole = "VENDEDOR";

    [ObservableProperty]
    private bool _isActive = true;

    [ObservableProperty]
    private string? _cedula;

    [ObservableProperty]
    private string? _phoneNumber;

    [ObservableProperty]
    private string? _address;

    [ObservableProperty]
    private string? _municipality;

    [ObservableProperty]
    private string? _city;

    [ObservableProperty]
    private string? _emergencyContactName;

    [ObservableProperty]
    private string? _emergencyContactPhone;

    [ObservableProperty]
    private Guid? _selectedRouteId;

    [ObservableProperty]
    private bool _isEmployeeActive = true;

    [ObservableProperty]
    private bool _isEditMode;

    [ObservableProperty]
    private bool _isSaving;

    public ObservableCollection<BranchLookupDto> Branches { get; } = new();
    public ObservableCollection<RouteLookupDto> Routes { get; } = new();
    public List<string> Roles { get; } = new() { "ADMINISTRADOR", "SUPERVISOR", "CAJERO", "VENDEDOR", "CONTADOR", "TESORERO" };

    public UserEditorViewModel(UserApiClient userApiClient, INotificationService notificationService, UserDto? userToEdit = null)
    {
        _userApiClient = userApiClient;
        _notificationService = notificationService;
        _userToEdit = userToEdit;
        IsEditMode = userToEdit != null;

        if (userToEdit != null)
        {
            Username = userToEdit.Username;
            Email = userToEdit.Email;
            FirstName = userToEdit.FirstName;
            LastName = userToEdit.LastName;
            SelectedBranchId = userToEdit.DefaultBranchId;
            SelectedRole = Roles.Contains(userToEdit.Role) ? userToEdit.Role : "VENDEDOR";
            IsActive = userToEdit.IsActive;
            Cedula = userToEdit.Cedula;
            PhoneNumber = userToEdit.PhoneNumber;
            Address = userToEdit.Address;
            Municipality = userToEdit.Municipality;
            City = userToEdit.City;
            EmergencyContactName = userToEdit.EmergencyContactName;
            EmergencyContactPhone = userToEdit.EmergencyContactPhone;
            SelectedRouteId = userToEdit.RouteId;
            IsEmployeeActive = userToEdit.IsEmployeeActive;
        }
    }

    public async Task InitializeAsync()
    {
        await LoadBranchesAsync();
        await LoadRoutesAsync();
    }

    private async Task LoadBranchesAsync()
    {
        try
        {
            var list = await _userApiClient.GetBranchesAsync();
            Branches.Clear();
            foreach (var b in list)
            {
                Branches.Add(b);
            }
            if (Branches.Count > 0 && SelectedBranchId == Guid.Empty)
            {
                SelectedBranchId = Branches[0].Id;
            }
        }
        catch (Exception ex)
        {
            _notificationService.ShowError($"Error al cargar sucursales: {ex.Message}");
        }
    }

    private async Task LoadRoutesAsync()
    {
        try
        {
            var list = await _userApiClient.GetRoutesAsync();
            Routes.Clear();
            foreach (var r in list)
            {
                Routes.Add(r);
            }
        }
        catch (Exception ex)
        {
            _notificationService.ShowError($"Error al cargar rutas: {ex.Message}");
        }
    }

    [RelayCommand]
    private async Task SaveAsync()
    {
        if (string.IsNullOrWhiteSpace(Username))
        {
            _notificationService.ShowWarning("El nombre de usuario es requerido.");
            return;
        }
        if (!IsEditMode && string.IsNullOrWhiteSpace(Password))
        {
            _notificationService.ShowWarning("La contraseña es requerida.");
            return;
        }
        if (string.IsNullOrWhiteSpace(FirstName))
        {
            _notificationService.ShowWarning("El nombre es requerido.");
            return;
        }
        if (string.IsNullOrWhiteSpace(LastName))
        {
            _notificationService.ShowWarning("El apellido es requerido.");
            return;
        }
        if (SelectedBranchId == Guid.Empty)
        {
            _notificationService.ShowWarning("La sucursal predeterminada es requerida.");
            return;
        }

        IsSaving = true;
        try
        {
            if (IsEditMode)
            {
                var cmd = new UpdateUserCommandDto(
                    Id: _userToEdit!.Id,
                    Email: Email,
                    FirstName: FirstName,
                    LastName: LastName,
                    DefaultBranchId: SelectedBranchId,
                    Role: SelectedRole,
                    IsActive: IsActive,
                    Password: string.IsNullOrWhiteSpace(Password) ? null : Password,
                    Cedula: Cedula,
                    PhoneNumber: PhoneNumber,
                    Address: Address,
                    Municipality: Municipality,
                    City: City,
                    EmergencyContactName: EmergencyContactName,
                    EmergencyContactPhone: EmergencyContactPhone,
                    RouteId: SelectedRouteId,
                    IsEmployeeActive: IsEmployeeActive
                );
                var success = await _userApiClient.UpdateUserAsync(_userToEdit.Id, cmd);
                if (success)
                {
                    _notificationService.ShowSuccess("Trabajador actualizado exitosamente.");
                    RequestClose?.Invoke();
                }
                else
                {
                    _notificationService.ShowError("No se pudo actualizar el trabajador.");
                }
            }
            else
            {
                var cmd = new CreateUserCommandDto(
                    Username: Username,
                    Password: Password,
                    Email: Email,
                    FirstName: FirstName,
                    LastName: LastName,
                    DefaultBranchId: SelectedBranchId,
                    Role: SelectedRole,
                    Cedula: Cedula,
                    PhoneNumber: PhoneNumber,
                    Address: Address,
                    Municipality: Municipality,
                    City: City,
                    EmergencyContactName: EmergencyContactName,
                    EmergencyContactPhone: EmergencyContactPhone,
                    RouteId: SelectedRouteId,
                    IsEmployeeActive: IsEmployeeActive
                );
                var id = await _userApiClient.CreateUserAsync(cmd);
                if (id != Guid.Empty)
                {
                    _notificationService.ShowSuccess("Trabajador registrado exitosamente.");
                    RequestClose?.Invoke();
                }
            }
        }
        catch (Exception ex)
        {
            _notificationService.ShowError($"Error al guardar: {ex.Message}");
        }
        finally
        {
            IsSaving = false;
        }
    }

    [RelayCommand]
    private void Cancel()
    {
        RequestClose?.Invoke();
    }
}
