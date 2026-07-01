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

public partial class SuppliersViewModel : ViewModelBase
{
    private readonly SupplierApiClient _supplierApiClient;
    private readonly INotificationService _notificationService;

    public string Title => "Módulo de Proveedores";

    [ObservableProperty]
    private string? _searchTerm;

    [ObservableProperty]
    private Guid? _selectedCategoryId;

    [ObservableProperty]
    private string? _selectedStatus;

    [ObservableProperty]
    private int _pageNumber = 1;

    [ObservableProperty]
    private int _pageSize = 10;

    [ObservableProperty]
    private int _totalCount;

    [ObservableProperty]
    private bool _isLoading;

    public ObservableCollection<SupplierDto> Suppliers { get; } = new();
    public ObservableCollection<SupplierCategoryDto> Categories { get; } = new();
    public List<string> Statuses { get; } = new() { "Active", "Inactive", "Suspended" };

    public bool HasPreviousPage => PageNumber > 1;
    public bool HasNextPage => PageNumber * PageSize < TotalCount;

    public SuppliersViewModel(SupplierApiClient supplierApiClient, INotificationService notificationService)
    {
        _supplierApiClient = supplierApiClient;
        _notificationService = notificationService;
    }

    public async Task InitializeAsync()
    {
        await LoadCategoriesAsync();
        await LoadSuppliersAsync();
    }

    private async Task LoadCategoriesAsync()
    {
        try
        {
            var list = await _supplierApiClient.GetSupplierCategoriesAsync();
            Categories.Clear();
            Categories.Add(new SupplierCategoryDto(Guid.Empty, "Todas las Categorías", null));
            foreach (var c in list)
            {
                Categories.Add(c);
            }
            SelectedCategoryId = Guid.Empty;
        }
        catch (Exception ex)
        {
            _notificationService.ShowError($"Error al cargar categorías de proveedores: {ex.Message}");
        }
    }

    [RelayCommand]
    public async Task LoadSuppliersAsync()
    {
        IsLoading = true;
        try
        {
            Guid? catFilter = SelectedCategoryId == Guid.Empty ? null : SelectedCategoryId;
            var result = await _supplierApiClient.GetSuppliersPagedAsync(PageNumber, PageSize, SearchTerm, catFilter, SelectedStatus);
            Suppliers.Clear();
            if (result?.Items != null)
            {
                foreach (var supplier in result.Items)
                {
                    Suppliers.Add(supplier);
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
            _notificationService.ShowError($"Error al cargar proveedores: {ex.Message}");
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
            await LoadSuppliersAsync();
        }
    }

    [RelayCommand]
    private async Task PreviousPageAsync()
    {
        if (HasPreviousPage)
        {
            PageNumber--;
            await LoadSuppliersAsync();
        }
    }

    [RelayCommand]
    private async Task RefreshAsync()
    {
        PageNumber = 1;
        await LoadSuppliersAsync();
    }

    [RelayCommand]
    private async Task CreateSupplierAsync()
    {
        var editorViewModel = new SupplierEditorViewModel(_supplierApiClient, _notificationService);
        await editorViewModel.InitializeAsync();

        var editorWindow = new Views.Suppliers.SupplierEditorDialog
        {
            DataContext = editorViewModel,
            Owner = System.Windows.Application.Current.MainWindow
        };

        editorViewModel.RequestClose += () => editorWindow.Close();
        editorWindow.ShowDialog();

        await LoadSuppliersAsync();
    }

    [RelayCommand]
    private async Task ViewSupplierAsync(SupplierDto supplier)
    {
        if (supplier == null) return;

        IsLoading = true;
        try
        {
            var fullSupplier = await _supplierApiClient.GetSupplierByIdAsync(supplier.Id);
            if (fullSupplier == null)
            {
                _notificationService.ShowError("No se pudieron cargar los detalles del proveedor.");
                return;
            }

            var editorViewModel = new SupplierEditorViewModel(_supplierApiClient, _notificationService, fullSupplier);
            await editorViewModel.InitializeAsync();

            var editorWindow = new Views.Suppliers.SupplierEditorDialog
            {
                DataContext = editorViewModel,
                Owner = System.Windows.Application.Current.MainWindow
            };

            editorViewModel.RequestClose += () => editorWindow.Close();
            editorWindow.ShowDialog();
        }
        catch (Exception ex)
        {
            _notificationService.ShowError($"Error al abrir vista de detalles: {ex.Message}");
        }
        finally
        {
            IsLoading = false;
        }
    }

    partial void OnSearchTermChanged(string? value)
    {
        PageNumber = 1;
        _ = LoadSuppliersAsync();
    }

    partial void OnSelectedCategoryIdChanged(Guid? value)
    {
        PageNumber = 1;
        _ = LoadSuppliersAsync();
    }

    partial void OnSelectedStatusChanged(string? value)
    {
        PageNumber = 1;
        _ = LoadSuppliersAsync();
    }
}
