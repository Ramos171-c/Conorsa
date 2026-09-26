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
    private string _title = "Registrar Cliente";

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

    [ObservableProperty]
    private Guid? _selectedRouteId;

    [ObservableProperty]
    private string _phoneNumber = string.Empty;

    [ObservableProperty]
    private string _emailAddress = string.Empty;

    [ObservableProperty]
    private string _addressLine1 = string.Empty;

    [ObservableProperty]
    private string _neighborhood = string.Empty;

    [ObservableProperty]
    private string _city = string.Empty;

    [ObservableProperty]
    private string _country = "Nicaragua";

    public ObservableCollection<CustomerCategoryDto> Categories { get; } = new();
    public ObservableCollection<CustomerPricingProfileDto> PricingProfiles { get; } = new();
    public ObservableCollection<RouteDto> Routes { get; } = new();

    public Array IdentificationTypes => Enum.GetValues(typeof(IdentificationType));
    public Array CustomerTypes => Enum.GetValues(typeof(CustomerType));
    public Array CustomerStatuses => Enum.GetValues(typeof(CustomerStatus));

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
        Title = IsEditMode ? "Editar Cliente" : "Registrar Cliente";

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
            SelectedRouteId = customerToEdit.RouteId;

            var firstAddr = customerToEdit.Addresses.FirstOrDefault();
            if (firstAddr != null)
            {
                AddressLine1 = firstAddr.AddressLine1;
                Neighborhood = firstAddr.Neighborhood ?? string.Empty;
                City = firstAddr.City;
                Country = string.IsNullOrWhiteSpace(firstAddr.Country) ? "Nicaragua" : firstAddr.Country;
            }

            var firstPhone = customerToEdit.Phones.FirstOrDefault();
            if (firstPhone != null)
            {
                PhoneNumber = firstPhone.PhoneNumber;
            }

            var firstEmail = customerToEdit.Emails.FirstOrDefault();
            if (firstEmail != null)
            {
                EmailAddress = firstEmail.EmailAddress;
            }

            foreach (var a in customerToEdit.Addresses)
            {
                Addresses.Add(new EditableAddress
                {
                    Id = a.Id,
                    AddressLine1 = a.AddressLine1,
                    AddressLine2 = a.AddressLine2,
                    Neighborhood = a.Neighborhood,
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
        else
        {
            IdentificationNumber = "CLI-" + DateTime.Now.ToString("yyyyMMddHHmmss");
        }
    }

    public async Task InitializeAsync()
    {
        try
        {
            var cats = await _customerApiClient.GetCategoriesAsync();
            Categories.Clear();
            foreach (var c in cats) Categories.Add(c);

            var profiles = await _customerApiClient.GetPricingProfilesAsync();
            PricingProfiles.Clear();
            foreach (var p in profiles) PricingProfiles.Add(p);

            var routes = await _customerApiClient.GetRoutesAsync();
            Routes.Clear();
            foreach (var r in routes) Routes.Add(r);

            if (!IsEditMode)
            {
                if (Categories.Any()) SelectedCategoryId = Categories.First().Id;
                if (PricingProfiles.Any())
                {
                    var defaultProfile = PricingProfiles.FirstOrDefault(p => p.Type == CustomerPricingType.Retail) ?? PricingProfiles.First();
                    SelectedPricingProfileId = defaultProfile.Id;
                }
            }
        }
        catch (Exception ex)
        {
            _notificationService.ShowError("Error al cargar opciones: " + ex.Message);
        }
    }

    [RelayCommand]
    private async Task SaveAsync()
    {
        if (string.IsNullOrWhiteSpace(Name))
        {
            _notificationService.ShowWarning("El nombre del cliente es requerido.");
            return;
        }

        if (string.IsNullOrWhiteSpace(IdentificationNumber))
        {
            IdentificationNumber = "CLI-" + DateTime.Now.ToString("yyyyMMddHHmmss");
        }

        if (SelectedCategoryId == Guid.Empty && Categories.Any())
        {
            SelectedCategoryId = Categories.First().Id;
        }

        if (SelectedPricingProfileId == Guid.Empty && PricingProfiles.Any())
        {
            SelectedPricingProfileId = PricingProfiles.First().Id;
        }

        IsSaving = true;
        try
        {
            var addressesList = new List<UpdateCustomerAddressInput>();
            var createAddressesList = new List<CreateCustomerAddressInput>();

            if (!string.IsNullOrWhiteSpace(AddressLine1) || !string.IsNullOrWhiteSpace(City) || !string.IsNullOrWhiteSpace(Neighborhood))
            {
                var existingAddrId = Addresses.FirstOrDefault()?.Id ?? Guid.Empty;
                var addr1 = string.IsNullOrWhiteSpace(AddressLine1) ? "Dirección principal" : AddressLine1.Trim();
                var cityVal = string.IsNullOrWhiteSpace(City) ? "Managua" : City.Trim();
                var countryVal = string.IsNullOrWhiteSpace(Country) ? "Nicaragua" : Country.Trim();
                var neighborhoodVal = string.IsNullOrWhiteSpace(Neighborhood) ? null : Neighborhood.Trim();

                if (IsEditMode)
                {
                    addressesList.Add(new UpdateCustomerAddressInput(
                        Id: existingAddrId,
                        AddressLine1: addr1,
                        AddressLine2: null,
                        Neighborhood: neighborhoodVal,
                        City: cityVal,
                        State: null,
                        ZipCode: null,
                        Country: countryVal,
                        AddressType: "Principal",
                        IsDefault: true
                    ));
                }
                else
                {
                    createAddressesList.Add(new CreateCustomerAddressInput(
                        AddressLine1: addr1,
                        AddressLine2: null,
                        Neighborhood: neighborhoodVal,
                        City: cityVal,
                        State: null,
                        ZipCode: null,
                        Country: countryVal,
                        AddressType: "Principal",
                        IsDefault: true
                    ));
                }
            }

            var phonesList = new List<UpdateCustomerPhoneInput>();
            var createPhonesList = new List<CreateCustomerPhoneInput>();
            if (!string.IsNullOrWhiteSpace(PhoneNumber))
            {
                var existingPhoneId = Phones.FirstOrDefault()?.Id ?? Guid.Empty;
                if (IsEditMode)
                {
                    phonesList.Add(new UpdateCustomerPhoneInput(
                        Id: existingPhoneId,
                        PhoneNumber: PhoneNumber.Trim(),
                        PhoneType: "Móvil",
                        IsDefault: true
                    ));
                }
                else
                {
                    createPhonesList.Add(new CreateCustomerPhoneInput(
                        PhoneNumber: PhoneNumber.Trim(),
                        PhoneType: "Móvil",
                        IsDefault: true
                    ));
                }
            }

            var emailsList = new List<UpdateCustomerEmailInput>();
            var createEmailsList = new List<CreateCustomerEmailInput>();
            if (!string.IsNullOrWhiteSpace(EmailAddress))
            {
                var existingEmailId = Emails.FirstOrDefault()?.Id ?? Guid.Empty;
                if (IsEditMode)
                {
                    emailsList.Add(new UpdateCustomerEmailInput(
                        Id: existingEmailId,
                        EmailAddress: EmailAddress.Trim(),
                        EmailType: "Facturación",
                        IsDefault: true
                    ));
                }
                else
                {
                    createEmailsList.Add(new CreateCustomerEmailInput(
                        EmailAddress: EmailAddress.Trim(),
                        EmailType: "Facturación",
                        IsDefault: true
                    ));
                }
            }

            if (IsEditMode)
            {
                var command = new UpdateCustomerCommandDto(
                    Id: _customerToEdit!.Id,
                    IdentificationNumber: IdentificationNumber,
                    IdentificationType: SelectedIdentificationType,
                    CustomerType: SelectedCustomerType,
                    Name: Name.Trim(),
                    LegalName: string.IsNullOrWhiteSpace(LegalName) ? null : LegalName.Trim(),
                    CustomerCategoryId: SelectedCategoryId,
                    CustomerPricingProfileId: SelectedPricingProfileId,
                    CreditLimit: CreditLimit,
                    CreditDays: CreditDays,
                    CanUseCredit: CanUseCredit,
                    IsTaxExempt: IsTaxExempt,
                    DefaultDiscountPercentage: DefaultDiscountPercentage,
                    Status: SelectedStatus,
                    Addresses: addressesList,
                    Phones: phonesList,
                    Emails: emailsList,
                    Contacts: new List<UpdateCustomerContactInput>(),
                    RouteId: SelectedRouteId
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
                    Name: Name.Trim(),
                    LegalName: string.IsNullOrWhiteSpace(LegalName) ? null : LegalName.Trim(),
                    CustomerCategoryId: SelectedCategoryId,
                    CustomerPricingProfileId: SelectedPricingProfileId,
                    CreditLimit: CreditLimit,
                    CreditDays: CreditDays,
                    CanUseCredit: CanUseCredit,
                    IsTaxExempt: IsTaxExempt,
                    DefaultDiscountPercentage: DefaultDiscountPercentage,
                    Addresses: createAddressesList,
                    Phones: createPhonesList,
                    Emails: createEmailsList,
                    Contacts: new List<CreateCustomerContactInput>(),
                    RouteId: SelectedRouteId
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
            _notificationService.ShowError("Error al guardar cliente: " + ex.Message);
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
    private string? _neighborhood;

    [ObservableProperty]
    private string _city = string.Empty;

    [ObservableProperty]
    private string? _state;

    [ObservableProperty]
    private string? _zipCode;

    [ObservableProperty]
    private string _country = "Nicaragua";

    [ObservableProperty]
    private string _addressType = "Principal";

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
