using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using EnterpriseBillingSystem.Wpf.Models;
using EnterpriseBillingSystem.Wpf.Services.Api;
using EnterpriseBillingSystem.Wpf.Services.Authentication;
using EnterpriseBillingSystem.Wpf.Services.Dialogs;

namespace EnterpriseBillingSystem.Wpf.ViewModels;

public partial class PosViewModel : ViewModelBase
{
    private readonly PosApiClient _posApiClient;
    private readonly ProductApiClient _productApiClient;
    private readonly CurrentUserService _currentUserService;
    private readonly INotificationService _notificationService;
    private readonly Services.IReceiptPrinterService _printerService;



    [ObservableProperty]
    private string _cashierName = string.Empty;

    [ObservableProperty]
    private string _branchName = string.Empty;

    [ObservableProperty]
    private string _activeRegisterName = "Sin Caja Activa";

    [ObservableProperty]
    private bool _isCashSessionActive;

    [ObservableProperty]
    private string? _sessionWarningMessage;

    // Daily Stats
    [ObservableProperty]
    private decimal _dailySalesTotal;

    [ObservableProperty]
    private int _dailySalesCount;

    // Cart and totals
    public ObservableCollection<CartItemViewModel> CartItems { get; } = new();

    [ObservableProperty]
    private decimal _subtotal;

    [ObservableProperty]
    private decimal _taxes;

    [ObservableProperty]
    private decimal _discounts;

    [ObservableProperty]
    private decimal _total;

    // Selected customer
    [ObservableProperty]
    private CustomerSearchResultDto? _selectedCustomer;

    [ObservableProperty]
    private CustomerType _selectedCustomerType = CustomerType.Natural;

    public Array CustomerTypes => Enum.GetValues(typeof(CustomerType));

    [ObservableProperty]
    private string _customerSearchText = string.Empty;

    public ObservableCollection<CustomerSearchResultDto> CustomerSearchResults { get; } = new();

    // Product search
    [ObservableProperty]
    private string _productSearchText = string.Empty;

    public ObservableCollection<ProductSearchResultDto> ProductSearchResults { get; } = new();

    // Sale type
    [ObservableProperty]
    private bool _isCreditSale;

    [ObservableProperty]
    private int _creditDays = 15;

    // Payment methods catalog
    public ObservableCollection<PaymentMethodDto> PaymentMethods { get; } = new();

    [ObservableProperty]
    private PaymentMethodDto? _selectedPaymentMethod;

    // Payment Overlay Properties
    [ObservableProperty]
    private bool _isPaymentPanelOpen;

    [ObservableProperty]
    private decimal _paymentReceivedCash;

    [ObservableProperty]
    private decimal _paymentReceivedCard;

    [ObservableProperty]
    private decimal _paymentReceivedTransfer;

    [ObservableProperty]
    private decimal _paymentTotalReceived;

    [ObservableProperty]
    private decimal _paymentChange;

    [ObservableProperty]
    private bool _isBusy;

    private Guid? _defaultWarehouseId;
    private ActiveCashSessionDto? _activeCashSession;

    public PosViewModel(
        PosApiClient posApiClient,
        ProductApiClient productApiClient,
        CurrentUserService currentUserService,
        INotificationService notificationService,
        Services.IReceiptPrinterService printerService)
    {
        _posApiClient = posApiClient;
        _productApiClient = productApiClient;
        _currentUserService = currentUserService;
        _notificationService = notificationService;
        _printerService = printerService;

        CashierName = _currentUserService.CurrentUser?.Username ?? "Cajero";
        BranchName = "Casa Matriz";

        PropertyChanged += (s, e) =>
        {
            if (e.PropertyName == nameof(PaymentReceivedCash) ||
                e.PropertyName == nameof(PaymentReceivedCard) ||
                e.PropertyName == nameof(PaymentReceivedTransfer))
            {
                RecalculatePayment();
            }
        };
    }

    partial void OnSelectedCustomerChanged(CustomerSearchResultDto? value)
    {
        var pricingType = value?.CustomerPricingProfileType ?? CustomerPricingType.Retail;
        foreach (var item in CartItems)
        {
            item.ActivePricingType = pricingType;
        }
        RecalculateCartTotals();
        SelectedCustomerType = value?.CustomerType ?? CustomerType.Natural;
    }

    public async Task InitializeAsync()
    {
        IsBusy = true;
        try
        {
            // 1. Validate open session
            _activeCashSession = await _posApiClient.GetActiveCashSessionAsync(CashierName);
            if (_activeCashSession != null)
            {
                IsCashSessionActive = true;
                ActiveRegisterName = _activeCashSession.CashRegisterName;
                SessionWarningMessage = null;
            }
            else
            {
                IsCashSessionActive = false;
                ActiveRegisterName = "Sin Caja Activa";
                SessionWarningMessage = "ADVERTENCIA: No tiene una sesión de caja abierta. Bloqueado cobros al contado.";
                _notificationService.ShowWarning(SessionWarningMessage);
            }

            // 2. Load payment methods
            var methods = await _posApiClient.GetPaymentMethodsAsync();
            PaymentMethods.Clear();
            foreach (var m in methods)
            {
                PaymentMethods.Add(m);
            }
            SelectedPaymentMethod = PaymentMethods.FirstOrDefault(p => p.Code == "EFEC") ?? PaymentMethods.FirstOrDefault();

            // 3. Get branch warehouse
            _defaultWarehouseId = await _posApiClient.GetDefaultBranchWarehouseIdAsync();

            // 4. Default customer CONSUMIDOR FINAL (CUS-000001)
            await SearchCustomersAsync("CUS-000001");
            if (CustomerSearchResults.Any())
            {
                SelectedCustomer = CustomerSearchResults.First();
            }
            else
            {
                SelectedCustomer = new CustomerSearchResultDto
                {
                    Id = Guid.Parse("00000000-0000-0000-0000-000000000000"),
                    Name = "CONSUMIDOR FINAL",
                    CustomerCode = "CUS-000001",
                    CustomerPricingProfileType = CustomerPricingType.Retail
                };
            }
            CustomerSearchResults.Clear();


        }
        catch (Exception ex)
        {
            _notificationService.ShowError($"Error al inicializar POS: {ex.Message}");
        }
        finally
        {
            IsBusy = false;
        }
    }

    private bool _isUpdatingTotals;



    public void AddProductToCart(ProductSearchResultDto product)
    {
        var existing = CartItems.FirstOrDefault(c => c.ProductId == product.Id);
        if (existing != null)
        {
            existing.Quantity++;
            RecalculateCartTotals();
            _notificationService.ShowSuccess($"Agregado: {product.Name}");
        }
        else
        {
            var item = new CartItemViewModel
            {
                ProductId = product.Id,
                UnitOfMeasureId = product.DefaultUnitOfMeasureId,
                ProductCode = product.InternalCode,
                ProductName = product.Name,
                Quantity = 1,
                UnitPrice = product.DefaultSalePrice,
                DiscountPercentage = 0,
                TaxPercentage = product.TaxPercentage,
                BaseAvailableStock = product.AvailableStock,
                ActivePricingType = SelectedCustomer?.CustomerPricingProfileType ?? CustomerPricingType.Retail,
                OnItemChanged = RecalculateCartTotals
            };

            CartItems.Add(item);
            LoadItemPresentationsAsync(item).FireAndForgetSafeAsync();
        }
    }

    private async Task LoadItemPresentationsAsync(CartItemViewModel item)
    {
        try
        {
            var presentations = await _productApiClient.GetPresentationsAsync(item.ProductId);
            item.AvailablePresentations.Clear();
            foreach (var pres in presentations.Where(p => p.IsActive))
            {
                item.AvailablePresentations.Add(pres);
            }

            var defaultPres = item.AvailablePresentations.FirstOrDefault(p => p.IsDefaultSalePresentation)
                              ?? item.AvailablePresentations.FirstOrDefault(p => p.IsBaseUnit)
                              ?? item.AvailablePresentations.FirstOrDefault();

            if (defaultPres != null)
            {
                item.SelectedPresentation = defaultPres;
            }
            else
            {
                item.Recalculate();
            }
        }
        catch (Exception ex)
        {
            _notificationService.ShowError($"Error al cargar presentaciones: {ex.Message}");
        }
    }

    public async Task<bool> AddProductByBarcodeAsync(string barcode)
    {
        try
        {
            var presentation = await _productApiClient.GetPresentationByBarcodeAsync(barcode);
            if (presentation != null)
            {
                var existing = CartItems.FirstOrDefault(c => c.ProductId == presentation.ProductId);
                if (existing != null)
                {
                    if (existing.ProductPresentationId == presentation.Id)
                    {
                        existing.Quantity++;
                    }
                    else
                    {
                        var matchPres = existing.AvailablePresentations.FirstOrDefault(p => p.Id == presentation.Id);
                        if (matchPres != null)
                        {
                            existing.SelectedPresentation = matchPres;
                        }
                        existing.Quantity++;
                    }
                }
                else
                {
                    var item = new CartItemViewModel
                    {
                        ProductId = presentation.ProductId,
                        UnitOfMeasureId = presentation.UnitOfMeasureId,
                        ProductCode = presentation.ProductInternalCode,
                        ProductName = presentation.ProductName,
                        Quantity = 1,
                        UnitPrice = presentation.RetailPrice,
                        DiscountPercentage = 0,
                        TaxPercentage = presentation.TaxPercentage,
                        BaseAvailableStock = 0, // default placeholder
                        ActivePricingType = SelectedCustomer?.CustomerPricingProfileType ?? CustomerPricingType.Retail,
                        OnItemChanged = RecalculateCartTotals
                    };

                    CartItems.Add(item);
                    await LoadItemPresentationsAsync(item);

                    var matchPres = item.AvailablePresentations.FirstOrDefault(p => p.Id == presentation.Id);
                    if (matchPres != null)
                    {
                        item.SelectedPresentation = matchPres;
                    }
                }
                RecalculateCartTotals();
                _notificationService.ShowSuccess($"Agregado: {presentation.ProductName} ({presentation.Name})");
                return true;
            }
        }
        catch (Exception ex)
        {
            _notificationService.ShowError($"Error al escanear presentación: {ex.Message}");
        }
        return false;
    }

    public void RemoveProductFromCart(CartItemViewModel item)
    {
        CartItems.Remove(item);
        RecalculateCartTotals();
    }

    public void RecalculateCartTotals()
    {
        if (_isUpdatingTotals) return;
        _isUpdatingTotals = true;
        try
        {
            foreach (var item in CartItems)
            {
                item.Recalculate();
            }

            Subtotal = Math.Round(CartItems.Sum(c => c.Subtotal), 4);
            Taxes = Math.Round(CartItems.Sum(c => c.TaxAmount), 4);
            Discounts = Math.Round(CartItems.Sum(c => c.DiscountAmount), 4);
            Total = Math.Round(CartItems.Sum(c => c.Total), 4);
        }
        finally
        {
            _isUpdatingTotals = false;
        }
    }

    private void RecalculatePayment()
    {
        PaymentTotalReceived = PaymentReceivedCash + PaymentReceivedCard + PaymentReceivedTransfer;
        PaymentChange = Math.Max(0, PaymentTotalReceived - Total);
    }

    public async Task SearchProductsAsync(string text)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            ProductSearchResults.Clear();
            return;
        }
        try
        {
            var results = await _posApiClient.SearchProductsAsync(text);
            ProductSearchResults.Clear();
            foreach (var r in results)
            {
                ProductSearchResults.Add(r);
            }
        }
        catch
        {
            // Ignore
        }
    }

    public async Task SearchCustomersAsync(string text)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            CustomerSearchResults.Clear();
            return;
        }
        try
        {
            var results = await _posApiClient.SearchCustomersAsync(text);
            CustomerSearchResults.Clear();
            foreach (var r in results)
            {
                CustomerSearchResults.Add(r);
            }
        }
        catch
        {
            // Ignore
        }
    }

    [RelayCommand]
    private void ClearSale()
    {
        CartItems.Clear();
        RecalculateCartTotals();
        IsCreditSale = false;
        PaymentReceivedCash = 0;
        PaymentReceivedCard = 0;
        PaymentReceivedTransfer = 0;
        IsPaymentPanelOpen = false;
        _notificationService.ShowSuccess("Venta cancelada y pantalla limpia.");
    }

    [RelayCommand]
    private void OpenPayment()
    {
        if (!CartItems.Any())
        {
            _notificationService.ShowWarning("El carrito está vacío.");
            return;
        }

        if (!IsCreditSale && !IsCashSessionActive)
        {
            _notificationService.ShowError("Debe tener una sesión de caja abierta para realizar ventas al contado.");
            return;
        }

        if (IsCreditSale)
        {
            if (SelectedCustomer == null || SelectedCustomer.Id == Guid.Empty)
            {
                _notificationService.ShowWarning("Debe seleccionar un cliente real para ventas al crédito.");
                return;
            }

            if (!SelectedCustomer.CanUseCredit)
            {
                _notificationService.ShowError($"El cliente '{SelectedCustomer.Name}' no tiene autorizado crédito.");
                return;
            }

            if (Total > SelectedCustomer.CreditLimit)
            {
                _notificationService.ShowError($"Límite de crédito excedido. Límite: {SelectedCustomer.CreditLimit:C}, Requerido: {Total:C}");
                return;
            }

            // Checkout directly for credit
            ExecuteCheckoutAsync().FireAndForgetSafeAsync();
        }
        else
        {
            // Open payment overlay
            PaymentReceivedCash = Total;
            PaymentReceivedCard = 0;
            PaymentReceivedTransfer = 0;
            IsPaymentPanelOpen = true;
        }
    }

    [RelayCommand]
    private void CancelPayment()
    {
        IsPaymentPanelOpen = false;
    }

    [RelayCommand]
    private async Task ConfirmPaymentAsync()
    {
        if (PaymentTotalReceived < Total)
        {
            _notificationService.ShowWarning("El monto recibido no cubre el total de la factura.");
            return;
        }

        IsPaymentPanelOpen = false;
        await ExecuteCheckoutAsync();
    }

    private async Task ExecuteCheckoutAsync()
    {
        IsBusy = true;
        try
        {
            var customerId = SelectedCustomer?.Id ?? Guid.Empty;
            if (customerId == Guid.Empty)
            {
                var queryCustomers = await _posApiClient.SearchCustomersAsync("CUS-000001");
                customerId = queryCustomers.FirstOrDefault()?.Id ?? Guid.Empty;
                if (customerId == Guid.Empty)
                {
                    _notificationService.ShowError("No se encontró un cliente válido en el sistema.");
                    return;
                }
            }

            var warehouseId = _defaultWarehouseId ?? Guid.Empty;
            if (warehouseId == Guid.Empty)
            {
                // Fallback: Query first stock item to resolve
                var wh = await _posApiClient.GetDefaultBranchWarehouseIdAsync();
                if (wh.HasValue)
                {
                    warehouseId = wh.Value;
                }
                else
                {
                    _notificationService.ShowError("No se pudo resolver la bodega de salida por defecto.");
                    return;
                }
            }

            // 1. Create Invoice Request
            var createCommand = new
            {
                CustomerId = customerId,
                BranchWarehouseId = warehouseId,
                InvoiceDate = DateTime.UtcNow,
                IsCreditSale = IsCreditSale,
                PaymentTermsDays = IsCreditSale ? CreditDays : 0,
                Notes = IsCreditSale ? "Venta al Crédito" : "Facturación rápida POS",
                CustomerType = SelectedCustomerType,
                Details = CartItems.Select(c => new
                {
                    ProductId = c.ProductId,
                    ProductPresentationId = c.ProductPresentationId,
                    Quantity = c.Quantity,
                    UnitPrice = c.UnitPrice,
                    DiscountPercentage = c.DiscountPercentage,
                    TaxPercentage = c.TaxPercentage
                }).ToList()
            };

            var invoiceId = await _posApiClient.CreateInvoiceAsync(createCommand);

            // 2. Post Invoice
            await _posApiClient.PostInvoiceAsync(invoiceId);

            // 3. Print Receipt
            await _printerService.PrintReceiptAsync(invoiceId, "Ticket 80mm");

            // 4. Update Stats
            DailySalesTotal += Total;
            DailySalesCount++;

            _notificationService.ShowSuccess("¡Factura emitida e impresa con éxito!");

            // 5. Clean Screen
            CartItems.Clear();
            RecalculateCartTotals();
            IsCreditSale = false;
            PaymentReceivedCash = 0;
            PaymentReceivedCard = 0;
            PaymentReceivedTransfer = 0;
        }
        catch (Exception ex)
        {
            _notificationService.ShowError($"Error al procesar la venta: {ex.Message}");
        }
        finally
        {
            IsBusy = false;
        }
    }
}

public static class TaskExtensions
{
    public static async void FireAndForgetSafeAsync(this Task task)
    {
        try
        {
            await task;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"FireAndForget exception: {ex}");
        }
    }
}
