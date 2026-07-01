using System;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using EnterpriseBillingSystem.Wpf.Models;
using EnterpriseBillingSystem.Wpf.Services.Api;

namespace EnterpriseBillingSystem.Wpf.ViewModels;

public partial class InventoryDashboardViewModel : ViewModelBase
{
    private readonly InventoryApiClient _inventoryApiClient;
    private readonly ProductApiClient _productApiClient;

    [ObservableProperty]
    private int _totalProducts;

    [ObservableProperty]
    private int _activeProducts;

    [ObservableProperty]
    private int _soldOutProducts;

    [ObservableProperty]
    private int _hiddenProducts;

    [ObservableProperty]
    private int _favoriteProducts;

    [ObservableProperty]
    private int _lowStockProducts;

    [ObservableProperty]
    private decimal _inventoryValue;

    [ObservableProperty]
    private int _todayAdjustments;

    [ObservableProperty]
    private int _todayTransfers;

    [ObservableProperty]
    private bool _isLoading;

    public ObservableCollection<LowStockProductDto> LowStockItems { get; } = new();

    public InventoryDashboardViewModel(
        InventoryApiClient inventoryApiClient,
        ProductApiClient productApiClient)
    {
        _inventoryApiClient = inventoryApiClient;
        _productApiClient = productApiClient;
    }

    [RelayCommand]
    public async Task LoadDashboardAsync()
    {
        IsLoading = true;
        try
        {
            var kpis = await _inventoryApiClient.GetDashboardKpisAsync();
            if (kpis != null)
            {
                TotalProducts = kpis.TotalProducts;
                ActiveProducts = kpis.ActiveProducts;
                SoldOutProducts = kpis.SoldOutProducts;
                HiddenProducts = kpis.HiddenProducts;
                FavoriteProducts = kpis.FavoriteProducts;
                LowStockProducts = kpis.LowStockProducts;
                InventoryValue = kpis.InventoryValue;
                TodayAdjustments = kpis.TodayAdjustments;
                TodayTransfers = kpis.TodayTransfers;
            }

            var lowStockList = await _productApiClient.GetLowStockProductsAsync();
            LowStockItems.Clear();
            foreach (var item in lowStockList)
            {
                LowStockItems.Add(item);
            }
        }
        catch (Exception)
        {
            // Fail silently or handle
        }
        finally
        {
            IsLoading = false;
        }
    }
}
