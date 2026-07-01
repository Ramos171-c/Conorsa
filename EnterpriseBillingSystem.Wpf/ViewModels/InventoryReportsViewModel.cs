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

public record ValuationItem(
    string WarehouseCode,
    string WarehouseName,
    string ProductCode,
    string ProductName,
    decimal PhysicalStock,
    decimal PurchasePrice,
    decimal TotalValue
);

public partial class InventoryReportsViewModel : ViewModelBase
{
    private readonly InventoryApiClient _inventoryApiClient;
    private readonly ProductApiClient _productApiClient;
    private readonly INotificationService _notificationService;

    // Filters
    [ObservableProperty]
    private Guid _selectedWarehouseId;

    [ObservableProperty]
    private Guid _selectedProductId;

    [ObservableProperty]
    private DateTime? _startDate;

    [ObservableProperty]
    private DateTime? _endDate;

    [ObservableProperty]
    private bool _isLoading;

    [ObservableProperty]
    private decimal _totalValuationSum;

    // Lists for dropdowns
    public ObservableCollection<WarehouseDto> Warehouses { get; } = new();
    public ObservableCollection<ProductDto> Products { get; } = new();

    // Reports data sources
    public ObservableCollection<InventoryDto> StockReportItems { get; } = new();
    public ObservableCollection<ValuationItem> ValuationReportItems { get; } = new();
    public ObservableCollection<KardexDto> MovementReportItems { get; } = new();

    public InventoryReportsViewModel(
        InventoryApiClient inventoryApiClient,
        ProductApiClient productApiClient,
        INotificationService notificationService)
    {
        _inventoryApiClient = inventoryApiClient;
        _productApiClient = productApiClient;
        _notificationService = notificationService;

        // Default dates
        StartDate = DateTime.UtcNow.AddMonths(-1);
        EndDate = DateTime.UtcNow;
    }

    public async Task InitializeAsync()
    {
        IsLoading = true;
        try
        {
            var warehouses = await _inventoryApiClient.GetWarehousesAsync();
            Warehouses.Clear();
            Warehouses.Add(new WarehouseDto(Guid.Empty, "-- Todas las Bodegas --", "-- Todas --", null, true));
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
            Products.Add(new ProductDto(
                Id: Guid.Empty,
                InternalCode: "-- Todos los Productos --",
                Barcode: null,
                Name: "-- Todos --",
                Description: null,
                ProductType: 1,
                ProductStatus: 2,
                TrackInventory: true,
                RequiresSerialNumber: false,
                RequiresBatchControl: false,
                CategoryId: Guid.Empty,
                CategoryName: string.Empty,
                BrandId: null,
                BrandName: null,
                DefaultUnitOfMeasureId: Guid.Empty,
                DefaultUnitOfMeasureCode: string.Empty,
                DefaultPurchasePrice: 0,
                DefaultSalePrice: 0,
                CurrentCost: 0,
                ImagePath: null,
                IsCatalogVisible: true,
                IsSoldOut: false,
                SoldOutAt: null,
                SoldOutBy: null,
                MinimumStock: 0,
                IsFavorite: false,
                FavoriteOrder: 0,
                AllowPromotions: false,
                HighlightInCatalog: false,
                ShortDescription: null,
                CatalogBadge: null,
                DisplayOrder: 0,
                AutoMarkSoldOut: false,
                IsActive: true,
                Presentations: new List<ProductPresentationDto>(),
                DefaultPresentation: null,
                DefaultPrice: 0,
                ImageUrl: null,
                Availability: string.Empty,
                Taxes: new List<TaxDto>(),
                BranchProducts: new List<BranchProductDto>()
            ));
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
            _notificationService.ShowError($"Error al inicializar filtros: {ex.Message}");
        }
        finally
        {
            IsLoading = false;
        }
    }

    [RelayCommand]
    private async Task LoadStockReportAsync()
    {
        IsLoading = true;
        try
        {
            var warehouseId = SelectedWarehouseId == Guid.Empty ? (Guid?)null : SelectedWarehouseId;
            var productId = SelectedProductId == Guid.Empty ? (Guid?)null : SelectedProductId;

            var result = await _inventoryApiClient.GetStockInquiryAsync(warehouseId, productId, 1, 1000);
            StockReportItems.Clear();
            if (result?.Items != null)
            {
                foreach (var item in result.Items)
                {
                    StockReportItems.Add(item);
                }
            }
        }
        catch (Exception ex)
        {
            _notificationService.ShowError($"Error al cargar existencias: {ex.Message}");
        }
        finally
        {
            IsLoading = false;
        }
    }

    [RelayCommand]
    private async Task LoadValuationReportAsync()
    {
        IsLoading = true;
        try
        {
            var warehouseId = SelectedWarehouseId == Guid.Empty ? (Guid?)null : SelectedWarehouseId;
            var productId = SelectedProductId == Guid.Empty ? (Guid?)null : SelectedProductId;

            var result = await _inventoryApiClient.GetStockInquiryAsync(warehouseId, productId, 1, 1000);
            ValuationReportItems.Clear();
            decimal sum = 0;

            if (result?.Items != null)
            {
                foreach (var item in result.Items)
                {
                    // Fetch full product to get CurrentCost (Costo Base)
                    var fullProd = await _productApiClient.GetProductByIdAsync(item.ProductId);
                    var cost = fullProd?.CurrentCost ?? 0m;
                    var total = item.PhysicalStock * cost;

                    var valItem = new ValuationItem(
                        item.WarehouseCode,
                        item.WarehouseName,
                        item.ProductInternalCode,
                        item.ProductName,
                        item.PhysicalStock,
                        cost,
                        total
                    );

                    ValuationReportItems.Add(valItem);
                    sum += total;
                }
            }

            TotalValuationSum = sum;
        }
        catch (Exception ex)
        {
            _notificationService.ShowError($"Error al cargar valorización: {ex.Message}");
        }
        finally
        {
            IsLoading = false;
        }
    }

    [RelayCommand]
    private async Task LoadMovementReportAsync()
    {
        if (SelectedWarehouseId == Guid.Empty)
        {
            _notificationService.ShowWarning("Debe seleccionar una bodega específica para consultar movimientos.");
            return;
        }

        if (SelectedProductId == Guid.Empty)
        {
            _notificationService.ShowWarning("Debe seleccionar un producto específico para consultar movimientos.");
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
                1,
                1000
            );

            MovementReportItems.Clear();
            if (result?.Items != null)
            {
                foreach (var item in result.Items)
                {
                    MovementReportItems.Add(item);
                }
            }
        }
        catch (Exception ex)
        {
            _notificationService.ShowError($"Error al cargar movimientos: {ex.Message}");
        }
        finally
        {
            IsLoading = false;
        }
    }
}
