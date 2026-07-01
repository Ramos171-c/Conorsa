using System;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using EnterpriseBillingSystem.Wpf.Models;
using EnterpriseBillingSystem.Wpf.Services.Api;
using EnterpriseBillingSystem.Wpf.Services.Dialogs;

namespace EnterpriseBillingSystem.Wpf.ViewModels;

public partial class SupplierEditorViewModel : ViewModelBase
{
    private readonly SupplierApiClient _supplierApiClient;
    private readonly INotificationService _notificationService;
    private readonly SupplierDetailDto? _supplierToView;

    public event Action? RequestClose;

    [ObservableProperty]
    private string _identificationNumber = string.Empty;

    [ObservableProperty]
    private IdentificationType _selectedIdentificationType = IdentificationType.RUC;

    [ObservableProperty]
    private string _name = string.Empty;

    [ObservableProperty]
    private string? _legalName;

    [ObservableProperty]
    private Guid _selectedCategoryId;

    [ObservableProperty]
    private string? _phone;

    [ObservableProperty]
    private string? _email;

    [ObservableProperty]
    private string? _address;

    [ObservableProperty]
    private string? _contactName;

    [ObservableProperty]
    private bool _isReadOnly;

    [ObservableProperty]
    private bool _isSaving;

    public ObservableCollection<SupplierCategoryDto> Categories { get; } = new();
    public Array IdentificationTypes => Enum.GetValues(typeof(IdentificationType));

    public SupplierEditorViewModel(SupplierApiClient supplierApiClient, INotificationService notificationService, SupplierDetailDto? supplierToView = null)
    {
        _supplierApiClient = supplierApiClient;
        _notificationService = notificationService;
        _supplierToView = supplierToView;
        IsReadOnly = supplierToView != null;

        if (supplierToView != null)
        {
            IdentificationNumber = supplierToView.IdentificationNumber;
            if (Enum.TryParse<IdentificationType>(supplierToView.IdentificationType, out var type))
            {
                SelectedIdentificationType = type;
            }
            Name = supplierToView.Name;
            LegalName = supplierToView.LegalName;
            SelectedCategoryId = supplierToView.SupplierCategoryId;
            Phone = supplierToView.Phone;
            Email = supplierToView.Email;
            Address = supplierToView.Address;
            ContactName = supplierToView.ContactName;
        }
    }

    public async Task InitializeAsync()
    {
        await LoadCategoriesAsync();
    }

    private async Task LoadCategoriesAsync()
    {
        try
        {
            var list = await _supplierApiClient.GetSupplierCategoriesAsync();
            Categories.Clear();
            foreach (var c in list)
            {
                Categories.Add(c);
            }

            if (Categories.Count > 0 && SelectedCategoryId == Guid.Empty)
            {
                SelectedCategoryId = Categories[0].Id;
            }
        }
        catch (Exception ex)
        {
            _notificationService.ShowError($"Error al cargar categorías de proveedores: {ex.Message}");
        }
    }

    [RelayCommand]
    private async Task SaveAsync()
    {
        if (IsReadOnly) return;

        if (string.IsNullOrWhiteSpace(IdentificationNumber))
        {
            _notificationService.ShowWarning("El número de identificación es requerido.");
            return;
        }

        if (string.IsNullOrWhiteSpace(Name))
        {
            _notificationService.ShowWarning("El nombre del proveedor es requerido.");
            return;
        }

        if (SelectedCategoryId == Guid.Empty)
        {
            _notificationService.ShowWarning("La categoría de proveedor es requerida.");
            return;
        }

        IsSaving = true;
        try
        {
            var command = new CreateSupplierCommandDto(
                IdentificationNumber: IdentificationNumber,
                IdentificationType: SelectedIdentificationType,
                Name: Name,
                LegalName: LegalName,
                SupplierCategoryId: SelectedCategoryId,
                Phone: Phone,
                Email: Email,
                Address: Address,
                ContactName: ContactName
            );

            var id = await _supplierApiClient.CreateSupplierAsync(command);
            if (id != Guid.Empty)
            {
                _notificationService.ShowSuccess("Proveedor registrado exitosamente.");
                RequestClose?.Invoke();
            }
            else
            {
                _notificationService.ShowError("Error al registrar el proveedor.");
            }
        }
        catch (Exception ex)
        {
            _notificationService.ShowError($"Error al guardar proveedor: {ex.Message}");
        }
        finally
        {
            IsSaving = false;
        }
    }
}
