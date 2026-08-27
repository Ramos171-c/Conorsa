using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using EnterpriseBillingSystem.Wpf.Models;
using EnterpriseBillingSystem.Wpf.Services.Api;
using EnterpriseBillingSystem.Wpf.Services.Dialogs;

namespace EnterpriseBillingSystem.Wpf.ViewModels;

public partial class MobileOrdersViewModel : ViewModelBase
{
    private readonly SalesApiClient _salesApiClient;
    private readonly CustomerApiClient _customerApiClient;
    private readonly INotificationService _notificationService;

    [ObservableProperty]
    private int _pageNumber = 1;

    [ObservableProperty]
    private int _pageSize = 10;

    [ObservableProperty]
    private int _totalCount;

    [ObservableProperty]
    private bool _isLoading;

    [ObservableProperty]
    private bool _isConsolidationLoading;

    [ObservableProperty]
    private string? _selectedStatus;

    [ObservableProperty]
    private bool _connectionFailed;

    [ObservableProperty]
    private string _errorMessage = string.Empty;

    public ObservableCollection<SalesOrderListItemDto> Orders { get; } = new();
    public ObservableCollection<string> Statuses { get; } = new() { "-- Todos --", "Recibido", "EnProceso", "Completado", "Anulado", "SolicitudAnulacion" };
    public ObservableCollection<RouteDto> Routes { get; } = new();

    [ObservableProperty]
    private RouteDto? _selectedRoute;

    // Consolidated verification list
    public ObservableCollection<VerifiableProduct> ConsolidatedProducts { get; } = new();

    public bool HasPreviousPage => PageNumber > 1;
    public bool HasNextPage => PageNumber * PageSize < TotalCount;

    public bool CanDispatchConsolidated => SelectedStatus == "Recibido" && ConsolidatedProducts.Count > 0;

    public MobileOrdersViewModel(SalesApiClient salesApiClient, CustomerApiClient customerApiClient, INotificationService notificationService)
    {
        _salesApiClient = salesApiClient;
        _customerApiClient = customerApiClient;
        _notificationService = notificationService;
        SelectedStatus = "Recibido"; // Default to Recibido so they see pending orders to validate
    }

    public async Task InitializeAsync()
    {
        await LoadRoutesAsync();
        await LoadOrdersAsync();
    }

    private async Task LoadRoutesAsync()
    {
        try
        {
            var routes = await _customerApiClient.GetRoutesAsync();
            Routes.Clear();
            Routes.Add(new RouteDto(Guid.Empty, "ALL", "-- Todas --", true));
            foreach (var r in routes.Where(x => x.IsActive))
            {
                Routes.Add(r);
            }
            SelectedRoute = Routes[0];
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error al cargar rutas: {ex.Message}");
        }
    }

    private async Task<T> RetryOnConnectionErrorAsync<T>(Func<Task<T>> action, int maxRetries = 3, int delayMilliseconds = 1000)
    {
        int retryCount = 0;
        while (true)
        {
            try
            {
                return await action();
            }
            catch (Exception ex) when (retryCount < maxRetries && (ex is System.Net.Http.HttpRequestException || ex is System.IO.IOException || ex is TimeoutException || ex is TaskCanceledException))
            {
                retryCount++;
                await Task.Delay(delayMilliseconds * retryCount); // Exponential backoff
            }
        }
    }

    [RelayCommand]
    public async Task LoadOrdersAsync()
    {
        IsLoading = true;
        IsConsolidationLoading = true;
        ConnectionFailed = false;
        ErrorMessage = string.Empty;
        try
        {
            string? statusFilter = SelectedStatus == "-- Todos --" ? null : SelectedStatus;
            Guid? routeFilter = (SelectedRoute == null || SelectedRoute.Id == Guid.Empty) ? null : SelectedRoute.Id;
            
            // 1. Load paginated list of individual orders with retry policy
            var result = await RetryOnConnectionErrorAsync(() => _salesApiClient.GetSalesOrdersPagedAsync(PageNumber, PageSize, null, statusFilter, null, null, routeFilter));
            
            Orders.Clear();
            if (result?.Items != null)
            {
                foreach (var order in result.Items)
                {
                    Orders.Add(order);
                }
                TotalCount = result.TotalCount;
            }
            else
            {
                TotalCount = 0;
            }

            // 2. Load consolidated products with retry policy (fault tolerant)
            try
            {
                var consolidated = await _salesApiClient.GetConsolidatedProductsAsync(null, statusFilter, null, null, routeFilter);
                ConsolidatedProducts.Clear();
                if (consolidated != null)
                {
                    foreach (var item in consolidated)
                    {
                        ConsolidatedProducts.Add(new VerifiableProduct(item));
                    }
                }
            }
            catch (Exception exCons)
            {
                System.Diagnostics.Debug.WriteLine($"Consolidated loading warning: {exCons.Message}");
            }

            OnPropertyChanged(nameof(HasPreviousPage));
            OnPropertyChanged(nameof(HasNextPage));
            OnPropertyChanged(nameof(CanDispatchConsolidated));
        }
        catch (Exception ex)
        {
            ConnectionFailed = true;
            ErrorMessage = ex.Message;
            _notificationService.ShowError($"Error al cargar pedidos móviles: {ex.Message}");
        }
        finally
        {
            IsLoading = false;
            IsConsolidationLoading = false;
        }
    }

    [RelayCommand]
    private async Task NextPageAsync()
    {
        if (HasNextPage)
        {
            PageNumber++;
            await LoadOrdersAsync();
        }
    }

    [RelayCommand]
    private async Task PreviousPageAsync()
    {
        if (HasPreviousPage)
        {
            PageNumber--;
            await LoadOrdersAsync();
        }
    }

    [RelayCommand]
    private async Task RefreshAsync()
    {
        PageNumber = 1;
        await LoadOrdersAsync();
    }

    [RelayCommand]
    private void VerifyAllConsolidated()
    {
        foreach (var p in ConsolidatedProducts)
        {
            p.IsVerified = true;
        }
    }

    [RelayCommand]
    private async Task DispatchConsolidatedAsync()
    {
        if (ConsolidatedProducts.Count == 0) return;

        // Check verification status
        bool anyUnverified = ConsolidatedProducts.Any(p => !p.IsVerified);
        if (anyUnverified)
        {
            var confirmVerify = Views.Dialogs.CustomMessageBox.Show(
                "Hay productos en la lista que no han sido marcados como verificados. ¿Desea proceder con el despacho de todas formas?",
                "Productos no Verificados",
                System.Windows.MessageBoxButton.YesNo,
                System.Windows.MessageBoxImage.Warning);

            if (confirmVerify != System.Windows.MessageBoxResult.Yes) return;
        }

        var confirmDispatch = Views.Dialogs.CustomMessageBox.Show(
            "¿Está seguro de que desea convalidar la carga y procesar en lote todos los pedidos móviles en estado Recibido?",
            "Convalidar Carga y Procesar",
            System.Windows.MessageBoxButton.YesNo,
            System.Windows.MessageBoxImage.Question);

        if (confirmDispatch != System.Windows.MessageBoxResult.Yes) return;

        IsLoading = true;
        try
        {
            // 1. Get all received orders (non-paginated, up to 9999 items)
            var result = await _salesApiClient.GetSalesOrdersPagedAsync(1, 9999, null, "Recibido");
            if (result?.Items == null || !result.Items.Any())
            {
                _notificationService.ShowWarning("No se encontraron pedidos en estado Recibido para procesar.");
                return;
            }

            int successCount = 0;
            int errorCount = 0;

            // 2. Bulk confirm them (will transition from Recibido -> EnProceso on backend)
            foreach (var order in result.Items)
            {
                try
                {
                    var ok = await _salesApiClient.ConfirmSalesOrderAsync(order.Id);
                    if (ok) successCount++;
                    else errorCount++;
                }
                catch
                {
                    errorCount++;
                }
            }

            _notificationService.ShowSuccess($"Procesamiento completado. {successCount} pedidos procesados exitosamente." + 
                (errorCount > 0 ? $" ({errorCount} errores)." : ""));

            // 3. Reload everything under EnProceso filter
            SelectedStatus = "EnProceso";
            await LoadOrdersAsync();
        }
        catch (Exception ex)
        {
            _notificationService.ShowError($"Error al despachar carga consolidada: {ex.Message}");
        }
        finally
        {
            IsLoading = false;
        }
    }

    [RelayCommand]
    private async Task OpenRecentOrdersReportAsync()
    {
        var dialog = new Views.MobileOrders.RecentOrdersReportDialog(_salesApiClient, _notificationService)
        {
            Owner = System.Windows.Application.Current.MainWindow
        };
        var result = dialog.ShowDialog();
        if (result == true)
        {
            await LoadOrdersAsync();
        }
    }

    [RelayCommand]
    private async Task OpenRouteConsolidationEnProcesoAsync()
    {
        var dialog = new Views.MobileOrders.RecentOrdersReportDialog(_salesApiClient, _notificationService)
        {
            Owner = System.Windows.Application.Current.MainWindow
        };
        var result = dialog.ShowDialog();
        if (result == true)
        {
            await LoadOrdersAsync();
        }
    }

    [RelayCommand]
    private async Task OpenRouteConsolidationEnCaminoAsync()
    {
        var dialog = new Views.MobileOrders.RecentOrdersReportDialog(_salesApiClient, _notificationService)
        {
            Owner = System.Windows.Application.Current.MainWindow
        };
        dialog.ShowDialog();
    }

    [RelayCommand]
    private void OpenReturnsReport()
    {
        try
        {
            var pdfUrl = "http://167.99.13.177:8080/api/v1/route-liquidations/returns-report/pdf";
            System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
            {
                FileName = pdfUrl,
                UseShellExecute = true
            });
        }
        catch (Exception ex)
        {
            _notificationService.ShowError($"Error al abrir el reporte de devoluciones: {ex.Message}");
        }
    }

    [RelayCommand]
    private void OpenSellerReport()
    {
        try
        {
            var pdfUrl = "http://167.99.13.177:8080/api/v1/sales-orders/seller-report/pdf";
            System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
            {
                FileName = pdfUrl,
                UseShellExecute = true
            });
        }
        catch (Exception ex)
        {
            _notificationService.ShowError($"Error al abrir el reporte de vendedores: {ex.Message}");
        }
    }

    [RelayCommand]
    private void OpenShortagesReport()
    {
        try
        {
            var pdfUrl = "http://167.99.13.177:8080/api/v1/sales-orders/shortages-report/pdf";
            System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
            {
                FileName = pdfUrl,
                UseShellExecute = true
            });
        }
        catch (Exception ex)
        {
            _notificationService.ShowError($"Error al abrir el reporte de faltantes y pérdidas: {ex.Message}");
        }
    }

    [RelayCommand]
    private async Task BatchPrintDeliveryTicketsAsync()
    {
        IsLoading = true;
        try
        {
            Guid? routeFilter = (SelectedRoute == null || SelectedRoute.Id == Guid.Empty) ? null : SelectedRoute.Id;
            
            // Get all orders in status EnProceso
            var pagedResult = await _salesApiClient.GetSalesOrdersPagedAsync(1, 9999, routeFilter, "EnProceso");
            if (pagedResult?.Items == null || !pagedResult.Items.Any())
            {
                _notificationService.ShowWarning("No se encontraron pedidos activos en proceso para imprimir.");
                return;
            }

            var confirm = System.Windows.MessageBox.Show(
                $"Se van a generar e imprimir masivamente {pagedResult.Items.Count()} factura(s) / ticket(s) de entrega.\n\n¿Desea continuar?",
                "Impresión Masiva de Entregas",
                System.Windows.MessageBoxButton.YesNo,
                System.Windows.MessageBoxImage.Question);

            if (confirm != System.Windows.MessageBoxResult.Yes) return;

            var printDialog = new System.Windows.Controls.PrintDialog();
            if (printDialog.ShowDialog() != true) return;

            var flowDoc = new System.Windows.Documents.FlowDocument
            {
                PagePadding = new System.Windows.Thickness(5, 5, 5, 5),
                ColumnWidth = double.PositiveInfinity,
                FontFamily = new System.Windows.Media.FontFamily("Courier New"),
                FontSize = 11,
                TextAlignment = System.Windows.TextAlignment.Left
            };

            int totalLineCount = 0;

            foreach (var itemHeader in pagedResult.Items)
            {
                var fullOrder = await _salesApiClient.GetSalesOrderByIdAsync(itemHeader.Id);
                if (fullOrder == null || fullOrder.Details == null || !fullOrder.Details.Any()) continue;

                var validDetails = fullOrder.Details.Where(d => d.Quantity > 0).ToList();
                if (!validDetails.Any()) continue;

                totalLineCount += 14 + (validDetails.Count() * 2);

                CustomerDto? customer = null;
                try
                {
                    customer = await _customerApiClient.GetCustomerByIdAsync(fullOrder.CustomerId);
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"Error fetching customer in batch print: {ex.Message}");
                }

                var sec = new System.Windows.Documents.Section
                {
                    BreakPageBefore = flowDoc.Blocks.Any()
                };

                // Title Header
                var headerPara = new System.Windows.Documents.Paragraph(new System.Windows.Documents.Run("Dulce y caramelos\n"))
                {
                    FontSize = 18,
                    FontWeight = System.Windows.FontWeights.Bold,
                    TextAlignment = System.Windows.TextAlignment.Center
                };
                headerPara.Inlines.Add(new System.Windows.Documents.Run("FACTURA / TICKET DE ENTREGA\n"));
                headerPara.Inlines.Add(new System.Windows.Documents.Run("=========================================\n"));
                sec.Blocks.Add(headerPara);

                // Customer Details
                var custPara = new System.Windows.Documents.Paragraph();
                custPara.Inlines.Add(new System.Windows.Documents.Run($"Pedido No:   {fullOrder.OrderNumber}\n"));
                custPara.Inlines.Add(new System.Windows.Documents.Run($"Fecha:       {fullOrder.OrderDate:dd/MM/yyyy HH:mm}\n"));
                custPara.Inlines.Add(new System.Windows.Documents.Run($"Cliente:     {fullOrder.CustomerName} ({fullOrder.CustomerCode})\n"));
                
                if (customer != null)
                {
                    custPara.Inlines.Add(new System.Windows.Documents.Run($"Ruta:        {customer.RouteName ?? "No asignada"}\n"));
                    var address = customer.Addresses?.FirstOrDefault(a => a.IsDefault) ?? customer.Addresses?.FirstOrDefault();
                    if (address != null)
                    {
                        custPara.Inlines.Add(new System.Windows.Documents.Run($"Dirección:   {address.AddressLine1}, {address.City}\n"));
                    }
                    var phone = customer.Phones?.FirstOrDefault()?.PhoneNumber;
                    if (!string.IsNullOrEmpty(phone))
                    {
                        custPara.Inlines.Add(new System.Windows.Documents.Run($"Teléfono:    {phone}\n"));
                    }
                }
                custPara.Inlines.Add(new System.Windows.Documents.Run("=========================================\n"));
                sec.Blocks.Add(custPara);

                // Order Lines (Only items with Quantity > 0)
                decimal orderSubtotal = 0;
                decimal orderDiscount = 0;
                decimal orderTax = 0;

                var itemsPara = new System.Windows.Documents.Paragraph();
                itemsPara.Inlines.Add(new System.Windows.Documents.Run("PRODUCTOS CARGADOS A ENTREGAR\n"));
                itemsPara.Inlines.Add(new System.Windows.Documents.Run("-----------------------------------------\n"));
                
                foreach (var detail in validDetails)
                {
                    decimal lineDiscount = detail.DiscountAmount;
                    decimal lineTax = detail.TaxAmount;
                    decimal lineNet = detail.NetAmount;

                    orderSubtotal += detail.Quantity * detail.UnitPrice;
                    orderDiscount += lineDiscount;
                    orderTax += lineTax;

                    itemsPara.Inlines.Add(new System.Windows.Documents.Run($"{detail.ProductName}\n"));
                    string qtyUom = $"{detail.Quantity:N2} {detail.UnitOfMeasure}";
                    string net = $"C${lineNet:N2}";
                    itemsPara.Inlines.Add(new System.Windows.Documents.Run($"   {qtyUom.PadRight(22)} {net.PadLeft(14)}\n"));
                }
                itemsPara.Inlines.Add(new System.Windows.Documents.Run("-----------------------------------------\n"));
                sec.Blocks.Add(itemsPara);

                // Totals
                decimal orderTotal = orderSubtotal - orderDiscount + orderTax;
                var totalsPara = new System.Windows.Documents.Paragraph { TextAlignment = System.Windows.TextAlignment.Right };
                totalsPara.Inlines.Add(new System.Windows.Documents.Run($"Subtotal:     C${orderSubtotal:N2}\n"));
                if (orderDiscount > 0)
                {
                    totalsPara.Inlines.Add(new System.Windows.Documents.Run($"Descuento:   -C${orderDiscount:N2}\n"));
                }
                totalsPara.Inlines.Add(new System.Windows.Documents.Run($"TOTAL:        C${orderTotal:N2}\n"));
                
                decimal totalUsd = orderTotal / 36.5m;
                totalsPara.Inlines.Add(new System.Windows.Documents.Run($"TOTAL USD:     ${totalUsd:N2}\n"));
                totalsPara.Inlines.Add(new System.Windows.Documents.Run("=========================================\n"));
                sec.Blocks.Add(totalsPara);

                flowDoc.Blocks.Add(sec);
            }

            flowDoc.PageWidth = printDialog.PrintableAreaWidth > 0 ? printDialog.PrintableAreaWidth : 280;
            // Set dynamic height so thermal printers do NOT feed standard 11-inch letter paper height!
            double calculatedHeight = (totalLineCount * 18) + 40;
            flowDoc.PageHeight = Math.Max(100, calculatedHeight);
            
            var documentPaginator = ((System.Windows.Documents.IDocumentPaginatorSource)flowDoc).DocumentPaginator;
            printDialog.PrintDocument(documentPaginator, $"Facturas_Entrega_Masivas_EnCamino_{DateTime.Now:yyyyMMdd}");

            _notificationService.ShowSuccess($"Impresión masiva completada. {pagedResult.Items.Count()} facturas de entrega enviadas a la impresora.");
        }
        catch (Exception ex)
        {
            _notificationService.ShowError($"Error en la impresión masiva: {ex.Message}");
        }
        finally
        {
            IsLoading = false;
        }
    }

    [RelayCommand]
    private async Task ViewOrderDetailsAsync(object? parameter)
    {
        if (parameter is not SalesOrderListItemDto order) return;

        IsLoading = true;
        try
        {
            var fullOrder = await _salesApiClient.GetSalesOrderByIdAsync(order.Id);
            if (fullOrder == null)
            {
                _notificationService.ShowError("No se pudieron obtener los detalles del pedido.");
                return;
            }

            var detailViewModel = new MobileOrderDetailViewModel(_salesApiClient, _customerApiClient, _notificationService, fullOrder);
            var detailDialog = new Views.MobileOrders.MobileOrderDetailDialog
            {
                DataContext = detailViewModel,
                Owner = System.Windows.Application.Current.MainWindow
            };

            detailViewModel.RequestClose += () => detailDialog.Close();
            detailViewModel.OrderActionTaken += async () => await LoadOrdersAsync();

            detailDialog.ShowDialog();
        }
        catch (Exception ex)
        {
            _notificationService.ShowError($"Error al abrir el detalle del pedido: {ex.Message}");
        }
        finally
        {
            IsLoading = false;
        }
    }

    [RelayCommand]
    private async Task ConfirmOrderAsync(SalesOrderListItemDto order)
    {
        if (order == null) return;

        var confirm = Views.Dialogs.CustomMessageBox.Show(
            $"¿Está seguro de que desea procesar el pedido {order.OrderNumber}? Esto cambiará su estado a En Proceso.",
            "Procesar Pedido",
            System.Windows.MessageBoxButton.YesNo,
            System.Windows.MessageBoxImage.Question);

        if (confirm != System.Windows.MessageBoxResult.Yes) return;

        IsLoading = true;
        try
        {
            var success = await _salesApiClient.ConfirmSalesOrderAsync(order.Id);
            if (success)
            {
                _notificationService.ShowSuccess($"Pedido {order.OrderNumber} procesado exitosamente.");
                await LoadOrdersAsync();
            }
            else
            {
                _notificationService.ShowError("Error al procesar el pedido.");
            }
        }
        catch (Exception ex)
        {
            _notificationService.ShowError($"Error al procesar pedido: {ex.Message}");
        }
        finally
        {
            IsLoading = false;
        }
    }

    [RelayCommand]
    private async Task ShipOrderAsync(SalesOrderListItemDto order)
    {
        if (order == null) return;

        var confirm = Views.Dialogs.CustomMessageBox.Show(
            $"¿Está seguro de que desea confirmar el envío del pedido {order.OrderNumber}? Esto cambiará su estado a En Camino.",
            "Confirmar Envío",
            System.Windows.MessageBoxButton.YesNo,
            System.Windows.MessageBoxImage.Question);

        if (confirm != System.Windows.MessageBoxResult.Yes) return;

        IsLoading = true;
        try
        {
            var success = await _salesApiClient.UpdateSalesOrderStatusAsync(order.Id, 5);
            if (success)
            {
                _notificationService.ShowSuccess($"Pedido {order.OrderNumber} en camino.");
                await LoadOrdersAsync();
            }
            else
            {
                _notificationService.ShowError("Error al actualizar el estado del pedido.");
            }
        }
        catch (Exception ex)
        {
            _notificationService.ShowError($"Error al enviar pedido: {ex.Message}");
        }
        finally
        {
            IsLoading = false;
        }
    }

    [RelayCommand]
    private async Task DeliverOrderAsync(SalesOrderListItemDto order)
    {
        if (order == null) return;

        var confirm = Views.Dialogs.CustomMessageBox.Show(
            $"¿Está seguro de que desea confirmar la entrega del pedido {order.OrderNumber}? Esto cambiará su estado a Completado.",
            "Confirmar Entrega",
            System.Windows.MessageBoxButton.YesNo,
            System.Windows.MessageBoxImage.Question);

        if (confirm != System.Windows.MessageBoxResult.Yes) return;

        IsLoading = true;
        try
        {
            var success = await _salesApiClient.UpdateSalesOrderStatusAsync(order.Id, 6);
            if (success)
            {
                _notificationService.ShowSuccess($"Pedido {order.OrderNumber} entregado y completado exitosamente.");
                await LoadOrdersAsync();
            }
            else
            {
                _notificationService.ShowError("Error al actualizar el estado del pedido.");
            }
        }
        catch (Exception ex)
        {
            _notificationService.ShowError($"Error al completar pedido: {ex.Message}");
        }
        finally
        {
            IsLoading = false;
        }
    }

    [RelayCommand]
    private async Task CancelOrderAsync(SalesOrderListItemDto order)
    {
        if (order == null) return;

        var input = Views.Dialogs.CustomInputDialog.Show(
            $"Escriba el motivo de la anulación del pedido {order.OrderNumber}:",
            "Motivo de Anulación",
            "Anulado por el Administrador");

        if (!input.IsConfirmed) return;

        string reason = input.Text;
        if (string.IsNullOrWhiteSpace(reason))
        {
            Views.Dialogs.CustomMessageBox.Show("El motivo es requerido.", "Advertencia", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        IsLoading = true;
        try
        {
            var success = await _salesApiClient.CancelSalesOrderAsync(order.Id, reason);
            if (success)
            {
                _notificationService.ShowSuccess($"Pedido {order.OrderNumber} anulado exitosamente.");
                await LoadOrdersAsync();
            }
            else
            {
                _notificationService.ShowError("Error al anular el pedido.");
            }
        }
        catch (Exception ex)
        {
            _notificationService.ShowError($"Error al anular pedido: {ex.Message}");
        }
        finally
        {
            IsLoading = false;
        }
    }

    [RelayCommand]
    private async Task ApproveCancellationAsync(SalesOrderListItemDto order)
    {
        if (order == null) return;

        var confirm = Views.Dialogs.CustomMessageBox.Show(
            $"¿Está seguro de que desea APROBAR la solicitud de anulación del pedido {order.OrderNumber}? Esto anulará el pedido permanentemente.",
            "Aprobar Anulación",
            System.Windows.MessageBoxButton.YesNo,
            System.Windows.MessageBoxImage.Question);

        if (confirm != System.Windows.MessageBoxResult.Yes) return;

        IsLoading = true;
        try
        {
            var success = await _salesApiClient.CancelSalesOrderAsync(order.Id, "Anulación aprobada por el administrador.");
            if (success)
            {
                _notificationService.ShowSuccess($"Solicitud de anulación aprobada. El pedido {order.OrderNumber} ha sido anulado.");
                await LoadOrdersAsync();
            }
            else
            {
                _notificationService.ShowError("Error al aprobar la anulación.");
            }
        }
        catch (Exception ex)
        {
            _notificationService.ShowError($"Error al aprobar anulación: {ex.Message}");
        }
        finally
        {
            IsLoading = false;
        }
    }

    [RelayCommand]
    private async Task RejectCancellationAsync(SalesOrderListItemDto order)
    {
        if (order == null) return;

        var confirm = Views.Dialogs.CustomMessageBox.Show(
            $"¿Está seguro de que desea RECHAZAR la solicitud de anulación del pedido {order.OrderNumber}? El pedido regresará a estado Recibido.",
            "Rechazar Anulación",
            System.Windows.MessageBoxButton.YesNo,
            System.Windows.MessageBoxImage.Question);

        if (confirm != System.Windows.MessageBoxResult.Yes) return;

        IsLoading = true;
        try
        {
            var success = await _salesApiClient.UpdateSalesOrderStatusAsync(order.Id, 2); // 2 is Recibido
            if (success)
            {
                _notificationService.ShowSuccess($"Solicitud de anulación rechazada. El pedido {order.OrderNumber} ha regresado a estado Recibido.");
                await LoadOrdersAsync();
            }
            else
            {
                _notificationService.ShowError("Error al rechazar la solicitud de anulación.");
            }
        }
        catch (Exception ex)
        {
            _notificationService.ShowError($"Error al rechazar solicitud: {ex.Message}");
        }
        finally
        {
            IsLoading = false;
        }
    }

    partial void OnSelectedStatusChanged(string? value)
    {
        PageNumber = 1;
        _ = LoadOrdersAsync();
    }

    partial void OnSelectedRouteChanged(RouteDto? value)
    {
        PageNumber = 1;
        _ = LoadOrdersAsync();
    }
}

public partial class VerifiableProduct : ObservableObject
{
    public Guid ProductId { get; }
    public string ProductCode { get; }
    public string ProductName { get; }
    public string UnitOfMeasure { get; }
    public decimal TotalQuantity { get; }
    public decimal TotalNetAmount { get; }

    [ObservableProperty]
    private bool _isVerified;

    [ObservableProperty]
    private decimal _quantityLoaded;

    public VerifiableProduct(ConsolidatedProductDto dto)
    {
        ProductId = dto.ProductId;
        ProductCode = dto.ProductCode;
        ProductName = dto.ProductName;
        UnitOfMeasure = dto.UnitOfMeasure;
        TotalQuantity = dto.TotalQuantity;
        TotalNetAmount = dto.TotalNetAmount;
        QuantityLoaded = dto.TotalQuantity;
    }
}
