using System;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using EnterpriseBillingSystem.Wpf.Models;
using EnterpriseBillingSystem.Wpf.Services.Api;
using EnterpriseBillingSystem.Wpf.Services.Dialogs;
using Microsoft.Extensions.DependencyInjection;

namespace EnterpriseBillingSystem.Wpf.ViewModels;

public partial class PurchasesViewModel : ViewModelBase
{
    private readonly PurchaseApiClient _purchaseApiClient;
    private readonly SupplierApiClient _supplierApiClient;
    private readonly InventoryApiClient _inventoryApiClient;
    private readonly ProductApiClient _productApiClient;
    private readonly INotificationService _notificationService;
    private readonly IServiceProvider _serviceProvider;

    [ObservableProperty]
    private Guid? _selectedSupplierId;

    [ObservableProperty]
    private int _pageNumber = 1;

    [ObservableProperty]
    private int _pageSize = 10;

    [ObservableProperty]
    private int _totalCount;

    [ObservableProperty]
    private bool _isLoading;

    public ObservableCollection<PurchaseReceiptListItemDto> Receipts { get; } = new();
    public ObservableCollection<SupplierDto> Suppliers { get; } = new();

    public bool HasPreviousPage => PageNumber > 1;
    public bool HasNextPage => PageNumber * PageSize < TotalCount;

    public string Title => "Recepción de Compras / Ingreso de Mercadería";

    public PurchasesViewModel(
        PurchaseApiClient purchaseApiClient,
        SupplierApiClient supplierApiClient,
        InventoryApiClient inventoryApiClient,
        ProductApiClient productApiClient,
        INotificationService notificationService,
        IServiceProvider serviceProvider)
    {
        _purchaseApiClient = purchaseApiClient;
        _supplierApiClient = supplierApiClient;
        _inventoryApiClient = inventoryApiClient;
        _productApiClient = productApiClient;
        _notificationService = notificationService;
        _serviceProvider = serviceProvider;
    }

    public async Task InitializeAsync()
    {
        await LoadSuppliersAsync();
        await LoadReceiptsAsync();
    }

    private async Task LoadSuppliersAsync()
    {
        try
        {
            var result = await _supplierApiClient.GetSuppliersPagedAsync(1, 100);
            Suppliers.Clear();
            Suppliers.Add(new SupplierDto(Guid.Empty, "N/A", "N/A", "N/A", "-- Todos los Proveedores --", null, "N/A", null, null, "Active"));
            if (result?.Items != null)
            {
                foreach (var sup in result.Items)
                {
                    Suppliers.Add(sup);
                }
            }
        }
        catch
        {
            // Fail silently
        }
    }

    [RelayCommand]
    public async Task LoadReceiptsAsync()
    {
        IsLoading = true;
        try
        {
            var supplierId = SelectedSupplierId == Guid.Empty ? null : SelectedSupplierId;
            var result = await _purchaseApiClient.GetPurchaseReceiptsPagedAsync(PageNumber, PageSize, supplierId);
            
            Receipts.Clear();
            if (result?.Items != null)
            {
                foreach (var rec in result.Items)
                {
                    Receipts.Add(rec);
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
            _notificationService.ShowError($"Error al cargar recepciones: {ex.Message}");
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
            await LoadReceiptsAsync();
        }
    }

    [RelayCommand]
    private async Task PreviousPageAsync()
    {
        if (HasPreviousPage)
        {
            PageNumber--;
            await LoadReceiptsAsync();
        }
    }

    [RelayCommand]
    private async Task CreateReceiptAsync()
    {
        var editorViewModel = new ReceiptEditorViewModel(
            _purchaseApiClient, 
            _supplierApiClient, 
            _inventoryApiClient, 
            _productApiClient, 
            _notificationService);

        await editorViewModel.InitializeAsync();

        var editorWindow = new Views.Purchases.ReceiptEditorDialog
        {
            DataContext = editorViewModel,
            Owner = System.Windows.Application.Current.MainWindow
        };

        editorViewModel.RequestClose += () => editorWindow.Close();
        editorWindow.ShowDialog();

        // Reload
        await LoadReceiptsAsync();
    }

    [RelayCommand]
    private async Task RefreshAsync()
    {
        PageNumber = 1;
        await LoadReceiptsAsync();
    }

    partial void OnSelectedSupplierIdChanged(Guid? value)
    {
        PageNumber = 1;
        _ = LoadReceiptsAsync();
    }
}
