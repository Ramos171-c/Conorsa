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

public partial class ReceiptDetailItemViewModel : ObservableObject
{
    [ObservableProperty]
    private Guid _productId;

    [ObservableProperty]
    private string _productName = string.Empty;

    [ObservableProperty]
    private Guid _presentationId;

    [ObservableProperty]
    private string _presentationName = string.Empty;

    [ObservableProperty]
    private decimal _quantity = 1;

    [ObservableProperty]
    private decimal _unitPrice;

    [ObservableProperty]
    private decimal _requestedQuantity;

    [ObservableProperty]
    private decimal _pendingQuantity;

    public decimal SubTotal => Quantity * UnitPrice;

    partial void OnQuantityChanged(decimal value) => OnPropertyChanged(nameof(SubTotal));
    partial void OnUnitPriceChanged(decimal value) => OnPropertyChanged(nameof(SubTotal));
}

public partial class ReceiptEditorViewModel : ViewModelBase
{
    private readonly PurchaseApiClient _purchaseApiClient;
    private readonly SupplierApiClient _supplierApiClient;
    private readonly InventoryApiClient _inventoryApiClient;
    private readonly ProductApiClient _productApiClient;
    private readonly INotificationService _notificationService;

    public event Action? RequestClose;

    [ObservableProperty]
    private Guid _selectedSupplierId;

    [ObservableProperty]
    private Guid? _selectedPurchaseOrderId;

    [ObservableProperty]
    private bool _isPurchaseOrderSelected;

    public ObservableCollection<PurchaseOrderListItemDto> PurchaseOrders { get; } = new();

    [ObservableProperty]
    private Guid _selectedWarehouseId;

    [ObservableProperty]
    private DateTime _receiptDate = DateTime.Today;

    [ObservableProperty]
    private string? _referenceDocument;

    [ObservableProperty]
    private string? _notes;

    [ObservableProperty]
    private ProductDto? _selectedProduct;

    [ObservableProperty]
    private ProductPresentationDto? _selectedPresentation;

    [ObservableProperty]
    private decimal _quantityToAdd = 1;

    [ObservableProperty]
    private decimal _unitPriceToAdd;

    [ObservableProperty]
    private bool _isSaving;

    public ObservableCollection<SupplierDto> Suppliers { get; } = new();
    public ObservableCollection<WarehouseDto> Warehouses { get; } = new();
    public ObservableCollection<ProductDto> Products { get; } = new();
    public ObservableCollection<ProductPresentationDto> Presentations { get; } = new();
    public ObservableCollection<ReceiptDetailItemViewModel> Details { get; } = new();

    public decimal Total => Details.Sum(x => x.SubTotal);

    public ReceiptEditorViewModel(
        PurchaseApiClient purchaseApiClient,
        SupplierApiClient supplierApiClient,
        InventoryApiClient inventoryApiClient,
        ProductApiClient productApiClient,
        INotificationService notificationService)
    {
        _purchaseApiClient = purchaseApiClient;
        _supplierApiClient = supplierApiClient;
        _inventoryApiClient = inventoryApiClient;
        _productApiClient = productApiClient;
        _notificationService = notificationService;

        Details.CollectionChanged += (s, e) => OnPropertyChanged(nameof(Total));
    }

    public async Task InitializeAsync()
    {
        try
        {
            // Load active suppliers
            var suppliersResult = await _supplierApiClient.GetSuppliersPagedAsync(1, 1000, null);
            Suppliers.Clear();
            if (suppliersResult?.Items != null)
            {
                foreach (var s in suppliersResult.Items.Where(x => x.Status == "Active"))
                {
                    Suppliers.Add(s);
                }
            }
            if (Suppliers.Count > 0)
            {
                SelectedSupplierId = Suppliers[0].Id;
            }

            // Load warehouses
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

            // Load products
            var productsResult = await _productApiClient.GetProductsPagedAsync(1, 1000);
            Products.Clear();
            if (productsResult?.Items != null)
            {
                foreach (var p in productsResult.Items.Where(x => x.IsActive && x.TrackInventory && x.ProductType != 2))
                {
                    Products.Add(p);
                }
            }
            if (Products.Count > 0)
            {
                SelectedProduct = Products[0];
            }
        }
        catch (Exception ex)
        {
            _notificationService.ShowError($"Error al inicializar formulario de compra: {ex.Message}");
        }
    }

    partial void OnSelectedSupplierIdChanged(Guid value)
    {
        Details.Clear();
        _ = LoadPurchaseOrdersAsync(value);
    }

    partial void OnSelectedPurchaseOrderIdChanged(Guid? value)
    {
        IsPurchaseOrderSelected = value.HasValue;
        if (value.HasValue)
        {
            _ = LoadPurchaseOrderDetailsAsync(value.Value);
        }
        else
        {
            Details.Clear();
        }
    }

    private async Task LoadPurchaseOrdersAsync(Guid supplierId)
    {
        try
        {
            PurchaseOrders.Clear();
            SelectedPurchaseOrderId = null;

            if (supplierId == Guid.Empty) return;

            var approvedResult = await _purchaseApiClient.GetPurchaseOrdersPagedAsync(1, 100, supplierId, "Approved");
            var partialResult = await _purchaseApiClient.GetPurchaseOrdersPagedAsync(1, 100, supplierId, "PartiallyReceived");

            var list = new List<PurchaseOrderListItemDto>();
            if (approvedResult?.Items != null) list.AddRange(approvedResult.Items);
            if (partialResult?.Items != null) list.AddRange(partialResult.Items);

            foreach (var po in list)
            {
                PurchaseOrders.Add(po);
            }
        }
        catch (Exception ex)
        {
            _notificationService.ShowError($"Error al cargar órdenes de compra del proveedor: {ex.Message}");
        }
    }

    private async Task LoadPurchaseOrderDetailsAsync(Guid purchaseOrderId)
    {
        IsSaving = true;
        try
        {
            var order = await _purchaseApiClient.GetPurchaseOrderByIdAsync(purchaseOrderId);
            Details.Clear();

            if (order != null)
            {
                foreach (var d in order.Details)
                {
                    if (d.PendingQuantity <= 0) continue;

                    var product = await _productApiClient.GetProductByIdAsync(d.ProductId);
                    var presentation = product?.Presentations?.FirstOrDefault(p => p.UnitOfMeasureId == d.UnitOfMeasureId) 
                                       ?? product?.Presentations?.FirstOrDefault();

                    if (presentation != null)
                    {
                        Details.Add(new ReceiptDetailItemViewModel
                        {
                            ProductId = d.ProductId,
                            ProductName = d.ProductName,
                            PresentationId = presentation.Id,
                            PresentationName = presentation.Name,
                            Quantity = d.PendingQuantity,
                            UnitPrice = d.UnitPrice,
                            RequestedQuantity = d.Quantity,
                            PendingQuantity = d.PendingQuantity
                        });
                    }
                }
            }
        }
        catch (Exception ex)
        {
            _notificationService.ShowError($"Error al cargar detalles de la orden de compra: {ex.Message}");
        }
        finally
        {
            IsSaving = false;
        }
    }

    partial void OnSelectedProductChanged(ProductDto? value)
    {
        Presentations.Clear();
        SelectedPresentation = null;
        UnitPriceToAdd = 0;
        if (value != null)
        {
            if (value.Presentations != null)
            {
                foreach (var pres in value.Presentations.Where(x => x.IsActive && x.AllowPurchase))
                {
                    Presentations.Add(pres);
                }
                if (Presentations.Count > 0)
                {
                    SelectedPresentation = Presentations[0];
                }
            }
        }
    }

    partial void OnSelectedPresentationChanged(ProductPresentationDto? value)
    {
        if (value != null)
        {
            UnitPriceToAdd = value.Cost;
        }
        else
        {
            UnitPriceToAdd = 0;
        }
    }

    [RelayCommand]
    private void AddDetail()
    {
        if (SelectedProduct == null)
        {
            _notificationService.ShowWarning("Seleccione un producto.");
            return;
        }
        if (SelectedPresentation == null)
        {
            _notificationService.ShowWarning("Seleccione una presentación.");
            return;
        }
        if (QuantityToAdd <= 0)
        {
            _notificationService.ShowWarning("La cantidad debe ser mayor a cero.");
            return;
        }
        if (UnitPriceToAdd < 0)
        {
            _notificationService.ShowWarning("El precio unitario no puede ser negativo.");
            return;
        }

        // Check if already in details
        var existing = Details.FirstOrDefault(x => x.PresentationId == SelectedPresentation.Id);
        if (existing != null)
        {
            existing.Quantity += QuantityToAdd;
            OnPropertyChanged(nameof(Total));
        }
        else
        {
            Details.Add(new ReceiptDetailItemViewModel
            {
                ProductId = SelectedProduct.Id,
                ProductName = SelectedProduct.Name,
                PresentationId = SelectedPresentation.Id,
                PresentationName = SelectedPresentation.Name,
                Quantity = QuantityToAdd,
                UnitPrice = UnitPriceToAdd
            });
        }

        // Reset inputs
        QuantityToAdd = 1;
        if (SelectedPresentation != null)
        {
            UnitPriceToAdd = SelectedPresentation.Cost;
        }
    }

    [RelayCommand]
    private void RemoveDetail(object item)
    {
        if (item is ReceiptDetailItemViewModel detailItem)
        {
            Details.Remove(detailItem);
        }
    }

    [RelayCommand]
    private async Task SaveAsync()
    {
        if (SelectedSupplierId == Guid.Empty)
        {
            _notificationService.ShowWarning("Debe seleccionar un proveedor.");
            return;
        }
        if (SelectedWarehouseId == Guid.Empty)
        {
            _notificationService.ShowWarning("Debe seleccionar una bodega.");
            return;
        }
        if (Details.Count == 0)
        {
            _notificationService.ShowWarning("Debe agregar al menos una línea al detalle.");
            return;
        }

        // Validate quantities if a purchase order is selected
        if (SelectedPurchaseOrderId.HasValue)
        {
            foreach (var d in Details)
            {
                if (d.Quantity <= 0)
                {
                    _notificationService.ShowWarning($"La cantidad recibida para '{d.ProductName}' debe ser mayor a cero.");
                    return;
                }
                if (d.Quantity > d.PendingQuantity)
                {
                    _notificationService.ShowWarning($"La cantidad recibida para '{d.ProductName}' ({d.Quantity}) supera la cantidad pendiente en la orden de compra ({d.PendingQuantity}).");
                    return;
                }
            }
        }

        IsSaving = true;
        try
        {
            var detailsList = Details.Select(d => new ReceiptDetailRequestDto(
                ProductId: d.ProductId,
                ProductPresentationId: d.PresentationId,
                Quantity: d.Quantity,
                UnitPrice: d.UnitPrice
            )).ToList();

            var cmd = new RegisterPurchaseReceiptCommandDto(
                SupplierId: SelectedSupplierId,
                BranchWarehouseId: SelectedWarehouseId,
                PurchaseOrderId: SelectedPurchaseOrderId,
                ReceiptDate: ReceiptDate,
                ReferenceDocument: ReferenceDocument,
                Notes: Notes,
                Details: detailsList
            );

            var id = await _purchaseApiClient.RegisterPurchaseReceiptAsync(cmd);
            if (id != Guid.Empty)
            {
                _notificationService.ShowSuccess("Recepción de compra registrada exitosamente. Se ha sumado el stock al inventario.");
                RequestClose?.Invoke();
            }
            else
            {
                _notificationService.ShowError("No se pudo registrar la recepción de compra.");
            }
        }
        catch (Exception ex)
        {
            _notificationService.ShowError($"Error al guardar recepción: {ex.Message}");
        }
        finally
        {
            IsSaving = false;
        }
    }

    [RelayCommand]
    private void Cancel()
    {
        RequestClose?.Invoke();
    }
}
