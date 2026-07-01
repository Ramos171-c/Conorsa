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

public partial class InventoryMovementsViewModel : ViewModelBase
{
    private readonly InventoryApiClient _inventoryApiClient;
    private readonly ProductApiClient _productApiClient;
    private readonly INotificationService _notificationService;

    [ObservableProperty]
    private Guid _selectedWarehouseId;

    [ObservableProperty]
    private Guid _selectedProductId;

    [ObservableProperty]
    private DateTime? _startDate;

    [ObservableProperty]
    private DateTime? _endDate;

    [ObservableProperty]
    private int _pageNumber = 1;

    [ObservableProperty]
    private int _pageSize = 10;

    [ObservableProperty]
    private int _totalCount;

    [ObservableProperty]
    private bool _isLoading;

    public ObservableCollection<WarehouseDto> Warehouses { get; } = new();
    public ObservableCollection<ProductDto> Products { get; } = new();
    public ObservableCollection<KardexDto> Movements { get; } = new();

    public bool HasPreviousPage => PageNumber > 1;
    public bool HasNextPage => PageNumber * PageSize < TotalCount;

    public InventoryMovementsViewModel(
        InventoryApiClient inventoryApiClient,
        ProductApiClient productApiClient,
        INotificationService notificationService)
    {
        _inventoryApiClient = inventoryApiClient;
        _productApiClient = productApiClient;
        _notificationService = notificationService;

        // Default range: last 30 days
        StartDate = DateTime.Today.AddDays(-30);
        EndDate = DateTime.Today;
    }

    public async Task InitializeAsync()
    {
        IsLoading = true;
        try
        {
            var warehouses = await _inventoryApiClient.GetWarehousesAsync();
            Warehouses.Clear();
            foreach (var w in warehouses)
            {
                Warehouses.Add(w);
            }
            if (Warehouses.Count > 0)
            {
                SelectedWarehouseId = Warehouses[0].Id;
            }

            var productsResult = await _productApiClient.GetProductsPagedAsync(1, 1000);
            Products.Clear();
            if (productsResult?.Items != null)
            {
                foreach (var p in productsResult.Items.Where(x => x.TrackInventory && x.ProductType != 2))
                {
                    Products.Add(p);
                }
            }
            if (Products.Count > 0)
            {
                SelectedProductId = Products[0].Id;
            }
        }
        catch (Exception ex)
        {
            _notificationService.ShowError($"Error al inicializar filtros: {ex.Message}");
        }
        finally
        {
            IsLoading = false;
        }

        await LoadMovementsAsync();
    }

    [RelayCommand]
    public async Task LoadMovementsAsync()
    {
        if (SelectedWarehouseId == Guid.Empty || SelectedProductId == Guid.Empty)
        {
            Movements.Clear();
            TotalCount = 0;
            return;
        }

        IsLoading = true;
        try
        {
            var result = await _inventoryApiClient.GetKardexAsync(
                SelectedWarehouseId, 
                SelectedProductId, 
                StartDate, 
                EndDate, 
                PageNumber, 
                PageSize);

            Movements.Clear();
            if (result?.Items != null)
            {
                foreach (var k in result.Items)
                {
                    Movements.Add(k);
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
            _notificationService.ShowError($"Error al cargar movimientos (Kardex): {ex.Message}");
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
            await LoadMovementsAsync();
        }
    }

    [RelayCommand]
    private async Task PreviousPageAsync()
    {
        if (HasPreviousPage)
        {
            PageNumber--;
            await LoadMovementsAsync();
        }
    }

    partial void OnSelectedWarehouseIdChanged(Guid value)
    {
        PageNumber = 1;
        _ = LoadMovementsAsync();
    }

    partial void OnSelectedProductIdChanged(Guid value)
    {
        PageNumber = 1;
        _ = LoadMovementsAsync();
    }

    partial void OnStartDateChanged(DateTime? value)
    {
        PageNumber = 1;
        _ = LoadMovementsAsync();
    }

    partial void OnEndDateChanged(DateTime? value)
    {
        PageNumber = 1;
        _ = LoadMovementsAsync();
    }
}
