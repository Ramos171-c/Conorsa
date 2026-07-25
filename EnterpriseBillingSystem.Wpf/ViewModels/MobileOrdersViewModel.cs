using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.DependencyInjection;
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
    public ObservableCollection<string> Statuses { get; } = new() { "-- Todos --", "Recibido", "EnProceso", "EnCamino", "Completado", "Anulado", "SolicitudAnulacion" };
    public ObservableCollection<RouteDto> Routes { get; } = new();

    [ObservableProperty]
    private RouteDto? _selectedRoute;

    // Consolidated verification list
    public ObservableCollection<VerifiableProduct> ConsolidatedProducts { get; } = new();

    public bool HasPreviousPage => PageNumber > 1;
    public bool HasNextPage => PageNumber * PageSize < TotalCount;
    public int TotalPages => PageSize > 0 ? (int)Math.Ceiling((double)TotalCount / PageSize) : 1;
    public string PageDisplay => $"Página {PageNumber} de {(TotalPages > 0 ? TotalPages : 1)} (Total: {TotalCount} pedidos)";

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

            // 2. Load consolidated products with retry policy
            var consolidated = await RetryOnConnectionErrorAsync(() => _salesApiClient.GetConsolidatedProductsAsync(null, statusFilter, null, null, routeFilter));
            ConsolidatedProducts.Clear();
            foreach (var item in consolidated)
            {
                ConsolidatedProducts.Add(new VerifiableProduct(item));
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

            // 2. Parallel bulk confirmation (Max concurrency 5) for ultra-fast processing
            var resultsBag = new System.Collections.Concurrent.ConcurrentBag<bool>();
            await Parallel.ForEachAsync(result.Items, new ParallelOptions { MaxDegreeOfParallelism = 5 }, async (order, ct) =>
            {
                try
                {
                    var ok = await _salesApiClient.ConfirmSalesOrderAsync(order.Id);
                    resultsBag.Add(ok);
                }
                catch
                {
                    resultsBag.Add(false);
                }
            });

            int successCount = resultsBag.Count(x => x);
            int errorCount = resultsBag.Count(x => !x);

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

    [ObservableProperty]
    private DateTime? _fromDate;

    [ObservableProperty]
    private DateTime? _toDate;

    public bool IsRecibidoSelected => string.IsNullOrWhiteSpace(SelectedStatus) || SelectedStatus == "-- Todos --" || SelectedStatus.Equals("Recibido", StringComparison.OrdinalIgnoreCase);
    public bool IsEnProcesoSelected => !string.IsNullOrWhiteSpace(SelectedStatus) && SelectedStatus.Equals("EnProceso", StringComparison.OrdinalIgnoreCase);
    public bool IsEnCaminoSelected => !string.IsNullOrWhiteSpace(SelectedStatus) && SelectedStatus.Equals("EnCamino", StringComparison.OrdinalIgnoreCase);

    [RelayCommand]
    private async Task OpenRecentOrdersReportAsync()
    {
        string statusToFilter = string.IsNullOrWhiteSpace(SelectedStatus) || SelectedStatus == "-- Todos --" ? "Recibido" : SelectedStatus;
        var dialog = new Views.MobileOrders.RecentOrdersReportDialog(_salesApiClient, _customerApiClient, _notificationService, statusToFilter)
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
    private async Task OpenDispatchConsolidationAsync()
    {
        var dialog = new Views.MobileOrders.RecentOrdersReportDialog(_salesApiClient, _customerApiClient, _notificationService, "EnProceso")
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
    private async Task BatchPrintDeliveryTicketsAsync()
    {
        IsLoading = true;
        try
        {
            Guid? routeFilter = (SelectedRoute == null || SelectedRoute.Id == Guid.Empty) ? null : SelectedRoute.Id;
            
            // Get all orders in status EnCamino (status filter = "EnCamino")
            var pagedResult = await _salesApiClient.GetSalesOrdersPagedAsync(1, 9999, routeFilter, "EnCamino", FromDate, ToDate);
            if (pagedResult?.Items == null || !pagedResult.Items.Any())
            {
                _notificationService.ShowWarning("No se encontraron pedidos en estado 'En Camino' para imprimir.");
                return;
            }

            var confirm = System.Windows.MessageBox.Show(
                $"Se van a generar e imprimir masivamente {pagedResult.Items.Count()} factura(s) / ticket(s) de entrega en estado 'En Camino'.\n\n¿Desea continuar?",
                "Impresión Masiva de Entregas",
                System.Windows.MessageBoxButton.YesNo,
                System.Windows.MessageBoxImage.Question);

            if (confirm != System.Windows.MessageBoxResult.Yes) return;

            var printDialog = new System.Windows.Controls.PrintDialog();
            if (printDialog.ShowDialog() != true) return;

            var flowDoc = new System.Windows.Documents.FlowDocument
            {
                PagePadding = new System.Windows.Thickness(30),
                ColumnWidth = double.PositiveInfinity,
                FontFamily = new System.Windows.Media.FontFamily("Courier New"),
                FontSize = 12,
                TextAlignment = System.Windows.TextAlignment.Left
            };

            foreach (var itemHeader in pagedResult.Items)
            {
                var fullOrder = await _salesApiClient.GetSalesOrderByIdAsync(itemHeader.Id);
                if (fullOrder == null || fullOrder.Details == null || !fullOrder.Details.Any()) continue;

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
                headerPara.Inlines.Add(new System.Windows.Documents.Run("FACTURA / TICKET DE ENTREGA (EN CAMINO)\n"));
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

                // Order Lines
                var itemsPara = new System.Windows.Documents.Paragraph();
                itemsPara.Inlines.Add(new System.Windows.Documents.Run("PRODUCTOS CARGADOS A ENTREGAR\n"));
                itemsPara.Inlines.Add(new System.Windows.Documents.Run("-----------------------------------------\n"));
                
                foreach (var detail in fullOrder.Details)
                {
                    if (detail.Quantity <= 0) continue;
                    itemsPara.Inlines.Add(new System.Windows.Documents.Run($"{detail.ProductName}\n"));
                    string qtyUom = $"{detail.Quantity:N2} {detail.UnitOfMeasure}";
                    string net = $"C${detail.NetAmount:N2}";
                    itemsPara.Inlines.Add(new System.Windows.Documents.Run($"   {qtyUom.PadRight(22)} {net.PadLeft(14)}\n"));
                }
                itemsPara.Inlines.Add(new System.Windows.Documents.Run("-----------------------------------------\n"));
                sec.Blocks.Add(itemsPara);

                // Totals
                var totalsPara = new System.Windows.Documents.Paragraph { TextAlignment = System.Windows.TextAlignment.Right };
                totalsPara.Inlines.Add(new System.Windows.Documents.Run($"Subtotal:     C${fullOrder.SubTotal:N2}\n"));
                if (fullOrder.DiscountAmount > 0)
                {
                    totalsPara.Inlines.Add(new System.Windows.Documents.Run($"Descuento:   -C${fullOrder.DiscountAmount:N2}\n"));
                }
                totalsPara.Inlines.Add(new System.Windows.Documents.Run($"TOTAL:        C${fullOrder.TotalAmount:N2}\n"));
                
                decimal totalUsd = fullOrder.TotalAmount / 36.5m;
                totalsPara.Inlines.Add(new System.Windows.Documents.Run($"TOTAL USD:     ${totalUsd:N2}\n"));
                totalsPara.Inlines.Add(new System.Windows.Documents.Run("=========================================\n"));
                sec.Blocks.Add(totalsPara);

                flowDoc.Blocks.Add(sec);
            }

            flowDoc.PageWidth = printDialog.PrintableAreaWidth;
            flowDoc.PageHeight = printDialog.PrintableAreaHeight;
            
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
    private async Task ConfirmOrderAsync(object? parameter)
    {
        if (parameter is not SalesOrderListItemDto order) return;

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
    private async Task ShipOrderAsync(object? parameter)
    {
        if (parameter is not SalesOrderListItemDto order) return;

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
    private async Task DeliverOrderAsync(object? parameter)
    {
        if (parameter is not SalesOrderListItemDto order) return;

        var confirm = Views.Dialogs.CustomMessageBox.Show(
            $"¿Está seguro de que desea marcar como entregado el pedido {order.OrderNumber}? Esto cambiará su estado a Completado.",
            "Marcar como Entregado",
            System.Windows.MessageBoxButton.YesNo,
            System.Windows.MessageBoxImage.Question);

        if (confirm != System.Windows.MessageBoxResult.Yes) return;

        IsLoading = true;
        try
        {
            var success = await _salesApiClient.UpdateSalesOrderStatusAsync(order.Id, 6);
            if (success)
            {
                _notificationService.ShowSuccess($"Pedido {order.OrderNumber} marcado como completado.");
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
    private async Task RecoverZeroedOrdersAsync()
    {
        var confirm = Views.Dialogs.CustomMessageBox.Show(
            "¿Desea escanear y recuperar los pedidos que quedaron en $0.00 a su estado 'En Proceso' con sus productos originales?",
            "Recuperar Pedidos en $0.00",
            System.Windows.MessageBoxButton.YesNo,
            System.Windows.MessageBoxImage.Question);

        if (confirm != System.Windows.MessageBoxResult.Yes) return;

        IsLoading = true;
        try
        {
            var productApiClient = App.AppHost?.Services.GetService<ProductApiClient>();
            var productsResult = productApiClient != null ? await productApiClient.GetProductsPagedAsync(1, 5000) : null;
            var productList = productsResult?.Items ?? new List<ProductDto>();

            var ordersEnCamino = await _salesApiClient.GetSalesOrdersPagedAsync(1, 9999, status: "EnCamino");
            var ordersEnProceso = await _salesApiClient.GetSalesOrdersPagedAsync(1, 9999, status: "EnProceso");
            var ordersRecibido = await _salesApiClient.GetSalesOrdersPagedAsync(1, 9999, status: "Recibido");

            var allOrders = new List<SalesOrderListItemDto>();
            if (ordersEnCamino?.Items != null) allOrders.AddRange(ordersEnCamino.Items);
            if (ordersEnProceso?.Items != null) allOrders.AddRange(ordersEnProceso.Items);
            if (ordersRecibido?.Items != null) allOrders.AddRange(ordersRecibido.Items);

            var zeroedOrders = allOrders.Where(o => o.TotalAmount == 0).ToList();

            int recoveredCount = 0;

            foreach (var orderHeader in zeroedOrders)
            {
                var fullOrder = await _salesApiClient.GetSalesOrderByIdAsync(orderHeader.Id);
                if (fullOrder == null || string.IsNullOrWhiteSpace(fullOrder.Notes)) continue;

                if (!fullOrder.Notes.Contains("[Faltantes]:") && !fullOrder.Notes.Contains("[AJUSTE")) continue;

                var matches = System.Text.RegularExpressions.Regex.Matches(
                    fullOrder.Notes,
                    @"-\s*([^:]+):[^\(]*?\((?:Solicitado|solicitado):\s*([\d\.,]+)",
                    System.Text.RegularExpressions.RegexOptions.IgnoreCase);

                if (matches.Count == 0)
                {
                    matches = System.Text.RegularExpressions.Regex.Matches(
                        fullOrder.Notes,
                        @"-\s*([^:]+):\s*Faltó\s*([\d\.,]+)",
                        System.Text.RegularExpressions.RegexOptions.IgnoreCase);
                }

                List<SalesOrderDetailRequestDto> restoredDetails = new();

                foreach (System.Text.RegularExpressions.Match match in matches)
                {
                    string prodName = match.Groups[1].Value.Trim();
                    string qtyStr = match.Groups[2].Value.Trim().Replace(",", "");

                    if (decimal.TryParse(qtyStr, System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out decimal qty) && qty > 0)
                    {
                        var prod = productList.FirstOrDefault(p =>
                            p.Name.Equals(prodName, StringComparison.OrdinalIgnoreCase) ||
                            (!string.IsNullOrEmpty(p.Description) && p.Description.Equals(prodName, StringComparison.OrdinalIgnoreCase)));

                        if (prod != null)
                        {
                            restoredDetails.Add(new SalesOrderDetailRequestDto(
                                ProductId: prod.Id,
                                UnitOfMeasureId: prod.DefaultUnitOfMeasureId,
                                Quantity: qty,
                                UnitPrice: prod.DefaultSalePrice > 0 ? prod.DefaultSalePrice : prod.DefaultPrice,
                                DiscountPercentage: 0,
                                TaxPercentage: 0
                            ));
                        }
                    }
                }

                if (restoredDetails.Any())
                {
                    string cleanedNotes = System.Text.RegularExpressions.Regex.Replace(fullOrder.Notes, @"\n?\[Faltantes\]:.*", "", System.Text.RegularExpressions.RegexOptions.Singleline).Trim();
                    cleanedNotes = System.Text.RegularExpressions.Regex.Replace(cleanedNotes, @"\n?\[AJUSTE.*", "", System.Text.RegularExpressions.RegexOptions.Singleline).Trim();

                    var updateCmd = new UpdateSalesOrderCommandDto(
                        Id: fullOrder.Id,
                        CustomerId: fullOrder.CustomerId,
                        OrderDate: fullOrder.OrderDate,
                        Notes: cleanedNotes,
                        Details: restoredDetails
                    );

                    var updated = await _salesApiClient.UpdateSalesOrderAsync(fullOrder.Id, updateCmd);
                    if (updated)
                    {
                        await _salesApiClient.UpdateSalesOrderStatusAsync(fullOrder.Id, 4);
                        recoveredCount++;
                    }
                }
            }

            _notificationService.ShowSuccess($"Recuperación completada. Se restauraron {recoveredCount} pedidos a su valor original en estado 'En Proceso'.");
            await LoadOrdersAsync();
        }
        catch (Exception ex)
        {
            _notificationService.ShowError($"Error al recuperar pedidos: {ex.Message}");
        }
        finally
        {
            IsLoading = false;
        }
    }

    [RelayCommand]
    private async Task CancelOrderAsync(object? parameter)
    {
        if (parameter is not SalesOrderListItemDto order) return;

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
    private async Task ApproveCancellationAsync(object? parameter)
    {
        if (parameter is not SalesOrderListItemDto order) return;

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
    private async Task RejectCancellationAsync(object? parameter)
    {
        if (parameter is not SalesOrderListItemDto order) return;

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

    partial void OnTotalCountChanged(int value)
    {
        OnPropertyChanged(nameof(TotalPages));
        OnPropertyChanged(nameof(PageDisplay));
        OnPropertyChanged(nameof(HasPreviousPage));
        OnPropertyChanged(nameof(HasNextPage));
    }

    partial void OnPageNumberChanged(int value)
    {
        OnPropertyChanged(nameof(TotalPages));
        OnPropertyChanged(nameof(PageDisplay));
        OnPropertyChanged(nameof(HasPreviousPage));
        OnPropertyChanged(nameof(HasNextPage));
    }

    partial void OnSelectedStatusChanged(string? value)
    {
        PageNumber = 1;
        OnPropertyChanged(nameof(IsRecibidoSelected));
        OnPropertyChanged(nameof(IsEnProcesoSelected));
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
    public decimal AvailableStock { get; }
    public decimal DeductedFromInventory { get; }
    public decimal NetQuantityToOrder { get; }
    public decimal TotalNetAmount { get; }
    public string Observation { get; }

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
        AvailableStock = dto.AvailableStock;
        DeductedFromInventory = dto.DeductedFromInventory;
        NetQuantityToOrder = dto.NetQuantityToOrder;
        TotalNetAmount = dto.TotalNetAmount;
        Observation = dto.Observation;
        QuantityLoaded = dto.NetQuantityToOrder > 0 ? dto.NetQuantityToOrder : dto.TotalQuantity;
    }
}
