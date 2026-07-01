using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using EnterpriseBillingSystem.Wpf.Models;
using EnterpriseBillingSystem.Wpf.Services.Api;
using EnterpriseBillingSystem.Wpf.Services.Dialogs;

namespace EnterpriseBillingSystem.Wpf.ViewModels;

public partial class CustomerEditorViewModel : ViewModelBase
{
    private readonly CustomerApiClient _customerApiClient;
    private readonly INotificationService _notificationService;
    private readonly CustomerDto? _customerToEdit;

    public event Action? RequestClose;

    [ObservableProperty]
    private string _identificationNumber = string.Empty;

    [ObservableProperty]
    private IdentificationType _selectedIdentificationType = IdentificationType.Cedula;

    [ObservableProperty]
    private CustomerType _selectedCustomerType = CustomerType.Natural;

    [ObservableProperty]
    private string _name = string.Empty;

    [ObservableProperty]
    private string? _legalName;

    [ObservableProperty]
    private Guid _selectedCategoryId;

    [ObservableProperty]
    private Guid _selectedPricingProfileId;

    [ObservableProperty]
    private decimal _creditLimit;

    [ObservableProperty]
    private int _creditDays;

    [ObservableProperty]
    private bool _canUseCredit;

    [ObservableProperty]
    private bool _isTaxExempt;

    [ObservableProperty]
    private decimal _defaultDiscountPercentage;

    [ObservableProperty]
    private CustomerStatus _selectedStatus = CustomerStatus.Active;

    [ObservableProperty]
    private bool _isEditMode;

    [ObservableProperty]
    private bool _isSaving;

    public ObservableCollection<CustomerCategoryDto> Categories { get; } = new();
    public ObservableCollection<CustomerPricingProfileDto> PricingProfiles { get; } = new();

    public Array IdentificationTypes => Enum.GetValues(typeof(IdentificationType));
    public Array CustomerTypes => Enum.GetValues(typeof(CustomerType));
    public Array CustomerStatuses => Enum.GetValues(typeof(CustomerStatus));

    // List of values for types in form
    public List<string> AddressTypes { get; } = new() { "Fiscal", "Despacho", "Trabajo", "Residencial", "Otro" };
    public List<string> PhoneTypes { get; } = new() { "Móvil", "Trabajo", "Casa", "Fax", "Otro" };
    public List<string> EmailTypes { get; } = new() { "Facturación", "Cobranza", "Personal", "Trabajo", "Otro" };

    // Nested collections
    public ObservableCollection<EditableAddress> Addresses { get; } = new();
    public ObservableCollection<EditablePhone> Phones { get; } = new();
    public ObservableCollection<EditableEmail> Emails { get; } = new();
    public ObservableCollection<EditableContact> Contacts { get; } = new();

    public CustomerEditorViewModel(CustomerApiClient customerApiClient, INotificationService notificationService, CustomerDto? customerToEdit = null)
    {
        _customerApiClient = customerApiClient;
        _notificationService = notificationService;
        _customerToEdit = customerToEdit;
        IsEditMode = customerToEdit != null;

        if (customerToEdit != null)
        {
            IdentificationNumber = customerToEdit.IdentificationNumber;
            SelectedIdentificationType = customerToEdit.IdentificationType;
            SelectedCustomerType = customerToEdit.CustomerType;
            Name = customerToEdit.Name;
            LegalName = customerToEdit.LegalName;
            SelectedCategoryId = customerToEdit.CustomerCategoryId;
            SelectedPricingProfileId = customerToEdit.CustomerPricingProfileId;
            CreditLimit = customerToEdit.CreditLimit;
            CreditDays = customerToEdit.CreditDays;
            CanUseCredit = customerToEdit.CanUseCredit;
            IsTaxExempt = customerToEdit.IsTaxExempt;
            DefaultDiscountPercentage = customerToEdit.DefaultDiscountPercentage;
            SelectedStatus = customerToEdit.Status;

            // Load nested collections
            foreach (var a in customerToEdit.Addresses)
            {
                Addresses.Add(new EditableAddress
                {
                    Id = a.Id,
                    AddressLine1 = a.AddressLine1,
                    AddressLine2 = a.AddressLine2,
                    City = a.City,
                    State = a.State,
                    ZipCode = a.ZipCode,
                    Country = a.Country,
                    AddressType = a.AddressType,
                    IsDefault = a.IsDefault
                });
            }

            foreach (var p in customerToEdit.Phones)
            {
                Phones.Add(new EditablePhone
                {
                    Id = p.Id,
                    PhoneNumber = p.PhoneNumber,
                    PhoneType = p.PhoneType,
                    IsDefault = p.IsDefault
                });
            }

            foreach (var e in customerToEdit.Emails)
            {
                Emails.Add(new EditableEmail
                {
                    Id = e.Id,
                    EmailAddress = e.EmailAddress,
                    EmailType = e.EmailType,
                    IsDefault = e.IsDefault
                });
            }

            foreach (var c in customerToEdit.Contacts)
            {
                Contacts.Add(new EditableContact
                {
                    Id = c.Id,
                    FirstName = c.FirstName,
                    LastName = c.LastName,
                    JobTitle = c.JobTitle,
                    Phone = c.Phone,
                    Email = c.Email,
                    Notes = c.Notes,
                    IsDefault = c.IsDefault
                });
            }
        }
    }

    public async Task InitializeAsync()
    {
        await Task.WhenAll(LoadCategoriesAsync(), LoadPricingProfilesAsync());
    }

    private async Task LoadCategoriesAsync()
    {
        try
        {
            var list = await _customerApiClient.GetCategoriesAsync();
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
            _notificationService.ShowError($"Error al cargar categorías: {ex.Message}");
        }
    }

    private async Task LoadPricingProfilesAsync()
    {
        try
        {
            var list = await _customerApiClient.GetPricingProfilesAsync();
            PricingProfiles.Clear();
            foreach (var p in list)
            {
                if (p.IsActive)
                {
                    PricingProfiles.Add(p);
                }
            }

            if (PricingProfiles.Count > 0 && SelectedPricingProfileId == Guid.Empty)
            {
                SelectedPricingProfileId = PricingProfiles[0].Id;
            }
        }
        catch (Exception ex)
        {
            _notificationService.ShowError($"Error al cargar perfiles de precios: {ex.Message}");
        }
    }

    // Commands to manage addresses
    [RelayCommand]
    private void AddAddress()
    {
        Addresses.Add(new EditableAddress { IsDefault = Addresses.Count == 0 });
    }

    [RelayCommand]
    private void RemoveAddress(EditableAddress? address)
    {
        if (address != null)
        {
            Addresses.Remove(address);
        }
    }

    // Commands to manage phones
    [RelayCommand]
    private void AddPhone()
    {
        Phones.Add(new EditablePhone { IsDefault = Phones.Count == 0 });
    }

    [RelayCommand]
    private void RemovePhone(EditablePhone? phone)
    {
        if (phone != null)
        {
            Phones.Remove(phone);
        }
    }

    // Commands to manage emails
    [RelayCommand]
    private void AddEmail()
    {
        Emails.Add(new EditableEmail { IsDefault = Emails.Count == 0 });
    }

    [RelayCommand]
    private void RemoveEmail(EditableEmail? email)
    {
        if (email != null)
        {
            Emails.Remove(email);
        }
    }

    // Commands to manage contacts
    [RelayCommand]
    private void AddContact()
    {
        Contacts.Add(new EditableContact { IsDefault = Contacts.Count == 0 });
    }

    [RelayCommand]
    private void RemoveContact(EditableContact? contact)
    {
        if (contact != null)
        {
            Contacts.Remove(contact);
        }
    }

    [RelayCommand]
    private async Task SaveAsync()
    {
        if (string.IsNullOrWhiteSpace(IdentificationNumber))
        {
            IdentificationNumber = "CLI-" + DateTime.Now.ToString("yyyyMMddHHmmssfff");
        }

        if (string.IsNullOrWhiteSpace(Name))
        {
            _notificationService.ShowWarning("El nombre es requerido.");
            return;
        }

        if (SelectedCategoryId == Guid.Empty)
        {
            _notificationService.ShowWarning("La categoría es requerida.");
            return;
        }

        if (SelectedPricingProfileId == Guid.Empty)
        {
            _notificationService.ShowWarning("El perfil de precios es requerido.");
            return;
        }

        IsSaving = true;
        try
        {
            if (IsEditMode)
            {
                var command = new UpdateCustomerCommandDto(
                    Id: _customerToEdit!.Id,
                    IdentificationNumber: IdentificationNumber,
                    IdentificationType: SelectedIdentificationType,
                    CustomerType: SelectedCustomerType,
                    Name: Name,
                    LegalName: LegalName,
                    CustomerCategoryId: SelectedCategoryId,
                    CustomerPricingProfileId: SelectedPricingProfileId,
                    CreditLimit: 0,
                    CreditDays: 0,
                    CanUseCredit: false,
                    IsTaxExempt: IsTaxExempt,
                    DefaultDiscountPercentage: DefaultDiscountPercentage,
                    Status: SelectedStatus,
                    Addresses: Addresses.Select(a => new UpdateCustomerAddressInput(
                        Id: a.Id,
                        AddressLine1: a.AddressLine1,
                        AddressLine2: a.AddressLine2,
                        City: a.City,
                        State: a.State,
                        ZipCode: a.ZipCode,
                        Country: a.Country,
                        AddressType: a.AddressType,
                        IsDefault: a.IsDefault
                    )).ToList(),
                    Phones: Phones.Select(p => new UpdateCustomerPhoneInput(
                        Id: p.Id,
                        PhoneNumber: p.PhoneNumber,
                        PhoneType: p.PhoneType,
                        IsDefault: p.IsDefault
                    )).ToList(),
                    Emails: Emails.Select(e => new UpdateCustomerEmailInput(
                        Id: e.Id,
                        EmailAddress: e.EmailAddress,
                        EmailType: e.EmailType,
                        IsDefault: e.IsDefault
                    )).ToList(),
                    Contacts: Contacts.Select(c => new UpdateCustomerContactInput(
                        Id: c.Id,
                        FirstName: c.FirstName,
                        LastName: c.LastName,
                        JobTitle: c.JobTitle,
                        Phone: c.Phone,
                        Email: c.Email,
                        Notes: c.Notes,
                        IsDefault: c.IsDefault
                    )).ToList()
                );

                var success = await _customerApiClient.UpdateCustomerAsync(_customerToEdit.Id, command);
                if (success)
                {
                    _notificationService.ShowSuccess("Cliente actualizado exitosamente.");
                    RequestClose?.Invoke();
                }
                else
                {
                    _notificationService.ShowError("Error al actualizar el cliente.");
                }
            }
            else
            {
                var command = new CreateCustomerCommandDto(
                    IdentificationNumber: IdentificationNumber,
                    IdentificationType: SelectedIdentificationType,
                    CustomerType: SelectedCustomerType,
                    Name: Name,
                    LegalName: LegalName,
                    CustomerCategoryId: SelectedCategoryId,
                    CustomerPricingProfileId: SelectedPricingProfileId,
                    CreditLimit: 0,
                    CreditDays: 0,
                    CanUseCredit: false,
                    IsTaxExempt: IsTaxExempt,
                    DefaultDiscountPercentage: DefaultDiscountPercentage,
                    Addresses: Addresses.Select(a => new CreateCustomerAddressInput(
                        AddressLine1: a.AddressLine1,
                        AddressLine2: a.AddressLine2,
                        City: a.City,
                        State: a.State,
                        ZipCode: a.ZipCode,
                        Country: a.Country,
                        AddressType: a.AddressType,
                        IsDefault: a.IsDefault
                    )).ToList(),
                    Phones: Phones.Select(p => new CreateCustomerPhoneInput(
                        PhoneNumber: p.PhoneNumber,
                        PhoneType: p.PhoneType,
                        IsDefault: p.IsDefault
                    )).ToList(),
                    Emails: Emails.Select(e => new CreateCustomerEmailInput(
                        EmailAddress: e.EmailAddress,
                        EmailType: e.EmailType,
                        IsDefault: e.IsDefault
                    )).ToList(),
                    Contacts: Contacts.Select(c => new CreateCustomerContactInput(
                        FirstName: c.FirstName,
                        LastName: c.LastName,
                        JobTitle: c.JobTitle,
                        Phone: c.Phone,
                        Email: c.Email,
                        Notes: c.Notes,
                        IsDefault: c.IsDefault
                    )).ToList()
                );

                var id = await _customerApiClient.CreateCustomerAsync(command);
                if (id != Guid.Empty)
                {
                    _notificationService.ShowSuccess("Cliente creado exitosamente.");
                    RequestClose?.Invoke();
                }
                else
                {
                    _notificationService.ShowError("Error al crear el cliente.");
                }
            }
        }
        catch (Exception ex)
        {
            _notificationService.ShowError($"Error al guardar cliente: {ex.Message}");
        }
        finally
        {
            IsSaving = false;
        }
    }
}

public partial class EditableAddress : ObservableObject
{
    public Guid Id { get; set; } = Guid.Empty;

    [ObservableProperty]
    private string _addressLine1 = string.Empty;

    [ObservableProperty]
    private string? _addressLine2;

    [ObservableProperty]
    private string _city = string.Empty;

    [ObservableProperty]
    private string? _state;

    [ObservableProperty]
    private string? _zipCode;

    [ObservableProperty]
    private string _country = "Nicaragua";

    [ObservableProperty]
    private string _addressType = "Fiscal";

    [ObservableProperty]
    private bool _isDefault;
}

public partial class EditablePhone : ObservableObject
{
    public Guid Id { get; set; } = Guid.Empty;

    [ObservableProperty]
    private string _phoneNumber = string.Empty;

    [ObservableProperty]
    private string _phoneType = "Móvil";

    [ObservableProperty]
    private bool _isDefault;
}

public partial class EditableEmail : ObservableObject
{
    public Guid Id { get; set; } = Guid.Empty;

    [ObservableProperty]
    private string _emailAddress = string.Empty;

    [ObservableProperty]
    private string _emailType = "Facturación";

    [ObservableProperty]
    private bool _isDefault;
}

public partial class EditableContact : ObservableObject
{
    public Guid Id { get; set; } = Guid.Empty;

    [ObservableProperty]
    private string _firstName = string.Empty;

    [ObservableProperty]
    private string _lastName = string.Empty;

    [ObservableProperty]
    private string? _jobTitle;

    [ObservableProperty]
    private string? _phone;

    [ObservableProperty]
    private string? _email;

    [ObservableProperty]
    private string? _notes;

    [ObservableProperty]
    private bool _isDefault;
}
