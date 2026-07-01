using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using EnterpriseBillingSystem.Wpf.Models;
using EnterpriseBillingSystem.Wpf.Services.Api;
using EnterpriseBillingSystem.Wpf.Services.Dialogs;

namespace EnterpriseBillingSystem.Wpf.ViewModels;

public partial class CustomersViewModel : ViewModelBase
{
    private readonly CustomerApiClient _customerApiClient;
    private readonly INotificationService _notificationService;

    public string Title => "Módulo de Clientes";

    [ObservableProperty]
    private string? _searchTerm;

    [ObservableProperty]
    private Guid? _selectedCategoryId;

    [ObservableProperty]
    private CustomerStatus? _selectedStatus;

    [ObservableProperty]
    private int _pageNumber = 1;

    [ObservableProperty]
    private int _pageSize = 10;

    [ObservableProperty]
    private int _totalCount;

    [ObservableProperty]
    private bool _isLoading;

    public ObservableCollection<CustomerDto> Customers { get; } = new();
    public ObservableCollection<CustomerCategoryDto> Categories { get; } = new();
    public Array Statuses => Enum.GetValues(typeof(CustomerStatus));

    public bool HasPreviousPage => PageNumber > 1;
    public bool HasNextPage => PageNumber * PageSize < TotalCount;

    public CustomersViewModel(CustomerApiClient customerApiClient, INotificationService notificationService)
    {
        _customerApiClient = customerApiClient;
        _notificationService = notificationService;
    }

    public async Task InitializeAsync()
    {
        await LoadCategoriesAsync();
        await LoadCustomersAsync();
    }

    private async Task LoadCategoriesAsync()
    {
        try
        {
            var list = await _customerApiClient.GetCategoriesAsync();
            Categories.Clear();
            // Add a placeholder for "All Categories"
            Categories.Add(new CustomerCategoryDto(Guid.Empty, "Todas las Categorías", null, 0));
            foreach (var c in list)
            {
                Categories.Add(c);
            }
            SelectedCategoryId = Guid.Empty;
        }
        catch (Exception ex)
        {
            _notificationService.ShowError($"Error al cargar categorías de clientes: {ex.Message}");
        }
    }

    [RelayCommand]
    public async Task LoadCustomersAsync()
    {
        IsLoading = true;
        try
        {
            Guid? catFilter = SelectedCategoryId == Guid.Empty ? null : SelectedCategoryId;
            var result = await _customerApiClient.GetCustomersPagedAsync(PageNumber, PageSize, SearchTerm, catFilter, SelectedStatus);
            Customers.Clear();
            if (result?.Items != null)
            {
                foreach (var customer in result.Items)
                {
                    Customers.Add(customer);
                }
                TotalCount = result.TotalCount;
            }
            else
            {
                TotalCount = 0;
            }

            OnPropertyChanged(nameof(HasPreviousPage));
            OnPropertyChanged(nameof(HasNextPage));
        }
        catch (Exception ex)
        {
            _notificationService.ShowError($"Error al cargar clientes: {ex.Message}");
        }
        finally
        {
            IsLoading = false;
        }
    }

    [RelayCommand]
    private async Task NextPageAsync()
    {
        if (HasNextPage)
        {
            PageNumber++;
            await LoadCustomersAsync();
        }
    }

    [RelayCommand]
    private async Task PreviousPageAsync()
    {
        if (HasPreviousPage)
        {
            PageNumber--;
            await LoadCustomersAsync();
        }
    }

    [RelayCommand]
    private async Task RefreshAsync()
    {
        PageNumber = 1;
        await LoadCustomersAsync();
    }

    [RelayCommand]
    private async Task CreateCustomerAsync()
    {
        var editorViewModel = new CustomerEditorViewModel(_customerApiClient, _notificationService);
        await editorViewModel.InitializeAsync();

        var editorWindow = new Views.Customers.CustomerEditorDialog
        {
            DataContext = editorViewModel,
            Owner = System.Windows.Application.Current.MainWindow
        };

        editorViewModel.RequestClose += () => editorWindow.Close();
        editorWindow.ShowDialog();

        await LoadCustomersAsync();
    }

    [RelayCommand]
    private async Task EditCustomerAsync(CustomerDto customer)
    {
        if (customer == null) return;

        // Fetch full customer details to edit (including sub-items)
        IsLoading = true;
        try
        {
            var fullCustomer = await _customerApiClient.GetCustomerByIdAsync(customer.Id);
            if (fullCustomer == null)
            {
                _notificationService.ShowError("No se pudieron cargar los detalles completos del cliente.");
                return;
            }

            var editorViewModel = new CustomerEditorViewModel(_customerApiClient, _notificationService, fullCustomer);
            await editorViewModel.InitializeAsync();

            var editorWindow = new Views.Customers.CustomerEditorDialog
            {
                DataContext = editorViewModel,
                Owner = System.Windows.Application.Current.MainWindow
            };

            editorViewModel.RequestClose += () => editorWindow.Close();
            editorWindow.ShowDialog();

            await LoadCustomersAsync();
        }
        catch (Exception ex)
        {
            _notificationService.ShowError($"Error al abrir editor de cliente: {ex.Message}");
        }
        finally
        {
            IsLoading = false;
        }
    }

    [RelayCommand]
    private async Task DeleteCustomerAsync(CustomerDto customer)
    {
        if (customer == null) return;

        var confirm = Views.Dialogs.CustomMessageBox.Show(
            $"¿Está seguro de que desea eliminar al cliente {customer.Name}?",
            "Confirmar Eliminación",
            System.Windows.MessageBoxButton.YesNo,
            System.Windows.MessageBoxImage.Warning);

        if (confirm == System.Windows.MessageBoxResult.Yes)
        {
            IsLoading = true;
            try
            {
                var success = await _customerApiClient.DeleteCustomerAsync(customer.Id);
                if (success)
                {
                    _notificationService.ShowSuccess("Cliente eliminado exitosamente.");
                    await LoadCustomersAsync();
                }
                else
                {
                    _notificationService.ShowError("Error al eliminar al cliente.");
                }
            }
            catch (Exception ex)
            {
                _notificationService.ShowError($"Error al eliminar cliente: {ex.Message}");
            }
            finally
            {
                IsLoading = false;
            }
        }
    }

    [RelayCommand]
    private async Task ToggleStatusAsync(CustomerDto customer)
    {
        if (customer == null) return;

        IsLoading = true;
        try
        {
            bool success;
            if (customer.Status == CustomerStatus.Active)
            {
                success = await _customerApiClient.BlockCustomerAsync(customer.Id);
                if (success) _notificationService.ShowSuccess("Cliente bloqueado exitosamente.");
            }
            else
            {
                success = await _customerApiClient.ActivateCustomerAsync(customer.Id);
                if (success) _notificationService.ShowSuccess("Cliente activado exitosamente.");
            }

            if (success)
            {
                await LoadCustomersAsync();
            }
            else
            {
                _notificationService.ShowError("Error al cambiar el estado del cliente.");
            }
        }
        catch (Exception ex)
        {
            _notificationService.ShowError($"Error al cambiar estado del cliente: {ex.Message}");
        }
        finally
        {
            IsLoading = false;
        }
    }

    partial void OnSearchTermChanged(string? value)
    {
        PageNumber = 1;
        _ = LoadCustomersAsync();
    }

    partial void OnSelectedCategoryIdChanged(Guid? value)
    {
        PageNumber = 1;
        _ = LoadCustomersAsync();
    }

    partial void OnSelectedStatusChanged(CustomerStatus? value)
    {
        PageNumber = 1;
        _ = LoadCustomersAsync();
    }
}
