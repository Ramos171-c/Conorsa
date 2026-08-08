using System;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using EnterpriseBillingSystem.Wpf.Models;
using EnterpriseBillingSystem.Wpf.Services.Api;
using EnterpriseBillingSystem.Wpf.Services.Dialogs;

namespace EnterpriseBillingSystem.Wpf.ViewModels;

public partial class InventoryStockViewModel : ViewModelBase
{
    private readonly InventoryApiClient _inventoryApiClient;
    private readonly ProductApiClient _productApiClient;
    private readonly INotificationService _notificationService;

    [ObservableProperty]
    private Guid? _selectedWarehouseId;

    [ObservableProperty]
    private Guid? _selectedProductId;

    [ObservableProperty]
    private Guid? _selectedCategoryId;

    [ObservableProperty]
    private string _searchTerm = string.Empty;

    [ObservableProperty]
    private int _pageNumber = 1;

    [ObservableProperty]
    private int _pageSize = 10;

    [ObservableProperty]
    private int _totalCount;

    [ObservableProperty]
    private bool _isLoading;

    public ObservableCollection<InventoryDto> StockItems { get; } = new();
    public ObservableCollection<WarehouseDto> Warehouses { get; } = new();
    public ObservableCollection<ProductDto> Products { get; } = new();
    public ObservableCollection<CategoryDto> Categories { get; } = new();

    public bool HasPreviousPage => PageNumber > 1;
    public bool HasNextPage => PageNumber * PageSize < TotalCount;

    public InventoryStockViewModel(
        InventoryApiClient inventoryApiClient,
        ProductApiClient productApiClient,
        INotificationService notificationService)
    {
        _inventoryApiClient = inventoryApiClient;
        _productApiClient = productApiClient;
        _notificationService = notificationService;
    }

    public async Task InitializeAsync()
    {
        await LoadFiltersAsync();
        await LoadStockAsync();
    }

    [RelayCommand]
    private async Task LoadFiltersAsync()
    {
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

            var categoriesResult = await _productApiClient.GetCategoriesAsync(1, 1000);
            Categories.Clear();
            Categories.Add(new CategoryDto(Guid.Empty, "-- Todas las Categorías --", null, null, true));
            if (categoriesResult?.Items != null)
            {
                foreach (var c in categoriesResult.Items)
                {
                    Categories.Add(c);
                }
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
        }
        catch
        {
            // Fail silently
        }
    }

    [RelayCommand]
    public async Task LoadStockAsync()
    {
        IsLoading = true;
        try
        {
            var warehouseId = SelectedWarehouseId == Guid.Empty ? null : SelectedWarehouseId;
            var productId = SelectedProductId == Guid.Empty ? null : SelectedProductId;
            var categoryId = SelectedCategoryId == Guid.Empty ? null : SelectedCategoryId;
            var search = string.IsNullOrWhiteSpace(SearchTerm) ? null : SearchTerm.Trim();

            var result = await _inventoryApiClient.GetStockInquiryAsync(warehouseId, productId, PageNumber, PageSize, categoryId, search);
            StockItems.Clear();
            if (result?.Items != null)
            {
                foreach (var item in result.Items)
                {
                    StockItems.Add(item);
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
            _notificationService.ShowError($"Error al consultar existencias: {ex.Message}");
        }
        finally
        {
            IsLoading = false;
        }
    }

    [RelayCommand]
    private async Task OpenBulkAdjustmentAsync()
    {
        if (!SelectedWarehouseId.HasValue || SelectedWarehouseId.Value == Guid.Empty)
        {
            _notificationService.ShowWarning("Seleccione una bodega para realizar el saneo masivo de inventario.");
            return;
        }

        var dialog = new Views.Inventory.BulkInventoryAdjustmentDialog(_inventoryApiClient, _productApiClient, _notificationService, SelectedWarehouseId.Value)
        {
            Owner = System.Windows.Application.Current.MainWindow
        };

        var result = dialog.ShowDialog();
        if (result == true || dialog.WasSaved)
        {
            await LoadStockAsync();
        }
    }

    [RelayCommand]
    private async Task EditStockAsync(InventoryDto item)
    {
        if (item == null) return;

        var (isConfirmed, inputText) = Views.Dialogs.CustomInputDialog.Show(
            $"Ingrese la cantidad física real en bodega para:\n{item.ProductInternalCode} - {item.ProductName}\n\nCantidad actual: {item.PhysicalStock:N2} {item.UnitOfMeasure}",
            "Ajustar Existencia Física",
            item.PhysicalStock.ToString("N2"));

        if (!isConfirmed || string.IsNullOrWhiteSpace(inputText)) return;

        if (!decimal.TryParse(inputText, out decimal newStock) || newStock < 0)
        {
            _notificationService.ShowError("Debe ingresar un valor numérico válido mayor o igual a 0.");
            return;
        }

        decimal diff = newStock - item.PhysicalStock;
        if (diff == 0)
        {
            _notificationService.ShowSuccess("La cantidad ingresada es igual a la existencia actual. No se realizaron cambios.");
            return;
        }

        IsLoading = true;
        try
        {
            var command = new
            {
                BranchWarehouseId = item.BranchWarehouseId,
                ProductId = item.ProductId,
                Quantity = Math.Abs(diff),
                IsPositive = diff > 0,
                ProductPresentationId = (Guid?)null,
                ReferenceDocument = "Ajuste Directo Conteo Físico",
                Notes = $"Ajuste manual directo de {item.PhysicalStock:N2} a {newStock:N2} {item.UnitOfMeasure}"
            };

            await _inventoryApiClient.AdjustInventoryAsync(command);
            _notificationService.ShowSuccess($"Existencia de {item.ProductInternalCode} actualizada a {newStock:N2} {item.UnitOfMeasure} exitosamente.");
            await LoadStockAsync();
        }
        catch (Exception ex)
        {
            _notificationService.ShowError($"Error al ajustar existencia: {ex.Message}");
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
            await LoadStockAsync();
        }
    }

    [RelayCommand]
    private async Task PreviousPageAsync()
    {
        if (HasPreviousPage)
        {
            PageNumber--;
            await LoadStockAsync();
        }
    }

    [RelayCommand]
    private async Task RefreshAsync()
    {
        PageNumber = 1;
        await LoadStockAsync();
    }

    partial void OnSelectedWarehouseIdChanged(Guid? value)
    {
        PageNumber = 1;
        _ = LoadStockAsync();
    }

    partial void OnSelectedProductIdChanged(Guid? value)
    {
        PageNumber = 1;
        _ = LoadStockAsync();
    }

    partial void OnSelectedCategoryIdChanged(Guid? value)
    {
        PageNumber = 1;
        _ = LoadStockAsync();
    }

    partial void OnSearchTermChanged(string value)
    {
        PageNumber = 1;
        _ = LoadStockAsync();
    }
}
