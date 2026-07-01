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

public partial class InventoryTransfersViewModel : ViewModelBase
{
    private readonly InventoryApiClient _inventoryApiClient;
    private readonly ProductApiClient _productApiClient;
    private readonly INotificationService _notificationService;

    [ObservableProperty]
    private Guid _selectedSourceWarehouseId;

    [ObservableProperty]
    private Guid _selectedDestWarehouseId;

    [ObservableProperty]
    private ProductDto? _selectedProduct;

    [ObservableProperty]
    private decimal _quantity;

    [ObservableProperty]
    private string? _notes;

    [ObservableProperty]
    private bool _isLoading;

    public ObservableCollection<WarehouseDto> Warehouses { get; } = new();
    public ObservableCollection<ProductDto> Products { get; } = new();

    public InventoryTransfersViewModel(
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
                SelectedSourceWarehouseId = Warehouses[0].Id;
                if (Warehouses.Count > 1)
                {
                    SelectedDestWarehouseId = Warehouses[1].Id;
                }
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
        }
        catch (Exception ex)
        {
            _notificationService.ShowError($"Error al inicializar filtros de transferencia: {ex.Message}");
        }
        finally
        {
            IsLoading = false;
        }
    }

    [RelayCommand]
    private async Task ExecuteTransferAsync()
    {
        if (SelectedSourceWarehouseId == Guid.Empty)
        {
            _notificationService.ShowWarning("Debe seleccionar una bodega de origen.");
            return;
        }

        if (SelectedDestWarehouseId == Guid.Empty)
        {
            _notificationService.ShowWarning("Debe seleccionar una bodega de destino.");
            return;
        }

        if (SelectedSourceWarehouseId == SelectedDestWarehouseId)
        {
            _notificationService.ShowWarning("La bodega de origen no puede ser igual a la de destino.");
            return;
        }

        if (SelectedProduct == null)
        {
            _notificationService.ShowWarning("Debe seleccionar un producto.");
            return;
        }

        if (Quantity <= 0)
        {
            _notificationService.ShowWarning("La cantidad debe ser mayor a 0.");
            return;
        }

        IsLoading = true;
        try
        {
            var presentation = SelectedProduct.Presentations.FirstOrDefault(p => p.IsBaseUnit)
                            ?? SelectedProduct.Presentations.FirstOrDefault(p => p.IsDefaultSalePresentation)
                            ?? SelectedProduct.Presentations.FirstOrDefault();

            if (presentation == null)
            {
                _notificationService.ShowWarning("El producto no tiene presentaciones configuradas.");
                return;
            }

            var command = new
            {
                FromBranchWarehouseId = SelectedSourceWarehouseId,
                ToBranchWarehouseId = SelectedDestWarehouseId,
                ProductId = SelectedProduct.Id,
                Quantity,
                ProductPresentationId = presentation.Id,
                ReferenceDocument = "Transferencia Manual",
                Notes
            };

            var movementId = await _inventoryApiClient.TransferInventoryAsync(command);
            if (movementId != Guid.Empty)
            {
                _notificationService.ShowSuccess("Transferencia de inventario ejecutada correctamente.");
                
                // Clear inputs
                Quantity = 0;
                Notes = string.Empty;
            }
            else
            {
                _notificationService.ShowError("Error al ejecutar la transferencia de inventario.");
            }
        }
        catch (Exception ex)
        {
            _notificationService.ShowError($"Error al ejecutar transferencia: {ex.Message}");
        }
        finally
        {
            IsLoading = false;
        }
    }
}
