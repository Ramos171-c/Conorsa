using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using CommunityToolkit.Mvvm.ComponentModel;
using EnterpriseBillingSystem.Wpf.Models;
using EnterpriseBillingSystem.Wpf.Services.Api;
using EnterpriseBillingSystem.Wpf.Services.Dialogs;

namespace EnterpriseBillingSystem.Wpf.Views.Inventory;

public partial class BulkAdjustmentItemModel : ObservableObject
{
    public Guid ProductId { get; set; }
    public string ProductInternalCode { get; set; } = string.Empty;
    public string ProductName { get; set; } = string.Empty;
    public string UnitOfMeasure { get; set; } = string.Empty;
    public decimal SystemStock { get; set; }

    private string _newStockText = "0.00";
    public string NewStockText
    {
        get => _newStockText;
        set => SetProperty(ref _newStockText, value);
    }
}

public partial class BulkInventoryAdjustmentDialog : Window
{
    private readonly InventoryApiClient _inventoryApiClient;
    private readonly ProductApiClient _productApiClient;
    private readonly INotificationService _notificationService;
    private readonly Guid _branchWarehouseId;

    private List<BulkAdjustmentItemModel> _allItems = new();
    private ObservableCollection<BulkAdjustmentItemModel> _filteredItems = new();

    public bool WasSaved { get; private set; }

    public BulkInventoryAdjustmentDialog(
        InventoryApiClient inventoryApiClient,
        ProductApiClient productApiClient,
        INotificationService notificationService,
        Guid branchWarehouseId)
    {
        InitializeComponent();
        _inventoryApiClient = inventoryApiClient;
        _productApiClient = productApiClient;
        _notificationService = notificationService;
        _branchWarehouseId = branchWarehouseId;

        GridItems.ItemsSource = _filteredItems;
        _ = LoadAllProductsAndStockAsync();
    }

    private async Task LoadAllProductsAndStockAsync()
    {
        try
        {
            // 1. Fetch all products
            var productsRes = await _productApiClient.GetProductsPagedAsync(1, 2000);
            var products = productsRes?.Items?.Where(p => p.TrackInventory).ToList() ?? new List<ProductDto>();

            // 2. Fetch current stock for warehouse
            var stockRes = await _inventoryApiClient.GetStockInquiryAsync(_branchWarehouseId, null, 1, 2000);
            var stockMap = stockRes?.Items?.ToDictionary(s => s.ProductId, s => s.PhysicalStock) ?? new Dictionary<Guid, decimal>();

            _allItems.Clear();
            foreach (var p in products)
            {
                decimal currentPhysical = stockMap.TryGetValue(p.Id, out var s) ? s : 0m;
                _allItems.Add(new BulkAdjustmentItemModel
                {
                    ProductId = p.Id,
                    ProductInternalCode = p.InternalCode,
                    ProductName = !string.IsNullOrWhiteSpace(p.Description) ? p.Description : p.Name,
                    UnitOfMeasure = p.DefaultUnitOfMeasureCode ?? "UND",
                    SystemStock = currentPhysical,
                    NewStockText = currentPhysical.ToString("F2")
                });
            }

            ApplyFilter();
        }
        catch (Exception ex)
        {
            _notificationService.ShowError($"Error al cargar productos para saneo: {ex.Message}");
        }
    }

    private void ApplyFilter()
    {
        var term = TxtSearch.Text?.Trim().ToLowerInvariant() ?? "";
        _filteredItems.Clear();

        var query = string.IsNullOrEmpty(term) 
            ? _allItems 
            : _allItems.Where(i => i.ProductInternalCode.ToLowerInvariant().Contains(term) || i.ProductName.ToLowerInvariant().Contains(term));

        foreach (var item in query)
        {
            _filteredItems.Add(item);
        }

        TxtSummary.Text = $"Mostrando {_filteredItems.Count} de {_allItems.Count} productos";
    }

    private void TxtSearch_TextChanged(object sender, TextChangedEventArgs e)
    {
        ApplyFilter();
    }

    private async void BtnSave_Click(object sender, RoutedEventArgs e)
    {
        var modifiedItems = new List<object>();
        int totalChanged = 0;

        foreach (var item in _allItems)
        {
            if (decimal.TryParse(item.NewStockText, out decimal parsedStock) && parsedStock >= 0)
            {
                if (parsedStock != item.SystemStock)
                {
                    modifiedItems.Add(new
                    {
                        ProductId = item.ProductId,
                        NewPhysicalStock = parsedStock
                    });
                    totalChanged++;
                }
            }
        }

        if (totalChanged == 0)
        {
            _notificationService.ShowWarning("No ha modificado la existencia de ningún producto.");
            return;
        }

        var confirm = Views.Dialogs.CustomMessageBox.Show(
            $"¿Está seguro de aplicar el Saneo e Inventario Físico Real sobre {totalChanged} productos seleccionados?\n\nEsta acción registrará los nuevos saldos contados directamente en la bodega.",
            "Confirmar Saneo Masivo",
            MessageBoxButton.YesNo,
            MessageBoxImage.Question);

        if (confirm != MessageBoxResult.Yes) return;

        BtnSave.IsEnabled = false;
        try
        {
            var command = new
            {
                BranchWarehouseId = _branchWarehouseId,
                Items = modifiedItems,
                Notes = $"Saneo e Inventario Físico Masivo de {totalChanged} productos"
            };

            int resultCount = await _inventoryApiClient.BulkAdjustInventoryAsync(command);
            _notificationService.ShowSuccess($"Saneo de inventario completado con éxito. Se actualizaron {resultCount} productos.");
            WasSaved = true;
            DialogResult = true;
            Close();
        }
        catch (Exception ex)
        {
            _notificationService.ShowError($"Error al guardar saneo masivo: {ex.Message}");
        }
        finally
        {
            BtnSave.IsEnabled = true;
        }
    }

    private void BtnCancel_Click(object sender, RoutedEventArgs e)
    {
        DialogResult = false;
        Close();
    }

    private void CloseButton_Click(object sender, RoutedEventArgs e)
    {
        DialogResult = false;
        Close();
    }
}
