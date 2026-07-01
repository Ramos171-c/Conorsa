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

public partial class InventoryAuditsViewModel : ViewModelBase
{
    private readonly ProductApiClient _productApiClient;
    private readonly INotificationService _notificationService;

    [ObservableProperty]
    private Guid _selectedProductId;

    [ObservableProperty]
    private bool _isLoading;

    public ObservableCollection<ProductDto> Products { get; } = new();
    public ObservableCollection<ProductPriceHistoryDto> AuditLogs { get; } = new();

    public InventoryAuditsViewModel(
        ProductApiClient productApiClient,
        INotificationService notificationService)
    {
        _productApiClient = productApiClient;
        _notificationService = notificationService;
    }

    public async Task InitializeAsync()
    {
        IsLoading = true;
        try
        {
            var productsResult = await _productApiClient.GetProductsPagedAsync(1, 1000);
            Products.Clear();
            if (productsResult?.Items != null)
            {
                foreach (var p in productsResult.Items)
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
            _notificationService.ShowError($"Error al inicializar productos: {ex.Message}");
        }
        finally
        {
            IsLoading = false;
        }

        await LoadAuditLogsAsync();
    }

    [RelayCommand]
    public async Task LoadAuditLogsAsync()
    {
        if (SelectedProductId == Guid.Empty)
        {
            AuditLogs.Clear();
            return;
        }

        IsLoading = true;
        try
        {
            var result = await _productApiClient.GetPriceHistoryAsync(SelectedProductId);
            AuditLogs.Clear();
            foreach (var log in result)
            {
                AuditLogs.Add(log);
            }
        }
        catch (Exception ex)
        {
            _notificationService.ShowError($"Error al cargar auditoría de precios: {ex.Message}");
        }
        finally
        {
            IsLoading = false;
        }
    }

    partial void OnSelectedProductIdChanged(Guid value)
    {
        _ = LoadAuditLogsAsync();
    }
}
