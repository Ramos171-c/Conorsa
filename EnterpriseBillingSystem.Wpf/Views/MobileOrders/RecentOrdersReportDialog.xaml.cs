using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using System.Windows;
using Microsoft.Extensions.DependencyInjection;
using EnterpriseBillingSystem.Wpf.Models;
using EnterpriseBillingSystem.Wpf.Services.Api;
using EnterpriseBillingSystem.Wpf.Services.Dialogs;
using EnterpriseBillingSystem.Wpf.Services.Export;

namespace EnterpriseBillingSystem.Wpf.Views.MobileOrders
{
    public partial class RecentOrdersReportDialog : Window, INotifyPropertyChanged
    {
        private readonly SalesApiClient _salesApiClient;
        private readonly CustomerApiClient? _customerApiClient;
        private readonly INotificationService _notificationService;
        private readonly string _targetStatus;

        private bool _isLoading;
        private DateTime? _fromDate;
        private DateTime? _toDate;
        private RouteDto? _selectedRoute;

        public ObservableCollection<RouteDto> Routes { get; } = new();

        public string DialogTitle => _targetStatus.Equals("EnProceso", StringComparison.OrdinalIgnoreCase)
            ? "Consolidado de Carga para Despacho (En Proceso ➔ En Camino)"
            : "Consolidación de Pedidos para Compra (Recibido ➔ En Proceso)";

        public string ConfirmButtonText => _targetStatus.Equals("EnProceso", StringComparison.OrdinalIgnoreCase)
            ? "🚚 Confirmar Despacho (Pasar a En Camino)"
            : "Confirmar Resumen (Pasar a En Proceso)";

        public string ConfirmButtonBackground => _targetStatus.Equals("EnProceso", StringComparison.OrdinalIgnoreCase)
            ? "#0284C7"
            : "#2563EB";

        public DateTime? FromDate
        {
            get => _fromDate;
            set
            {
                if (_fromDate != value)
                {
                    _fromDate = value;
                    OnPropertyChanged();
                }
            }
        }

        public DateTime? ToDate
        {
            get => _toDate;
            set
            {
                if (_toDate != value)
                {
                    _toDate = value;
                    OnPropertyChanged();
                }
            }
        }

        public RouteDto? SelectedRoute
        {
            get => _selectedRoute;
            set
            {
                if (_selectedRoute != value)
                {
                    _selectedRoute = value;
                    OnPropertyChanged();
                }
            }
        }

        public bool IsDarkTheme => false;

        public bool IsLoading
        {
            get => _isLoading;
            set
            {
                if (_isLoading != value)
                {
                    _isLoading = value;
                    OnPropertyChanged();
                    OnPropertyChanged(nameof(IsLoadingVisibility));
                    OnPropertyChanged(nameof(ShowEmptyMessage));
                    OnPropertyChanged(nameof(ShowEmptyMessageVisibility));
                }
            }
        }

        public string GeneralObservations => TxtGeneralObservations?.Text ?? string.Empty;

        public bool HasData => ConsolidatedProducts.Count > 0;
        public bool ShowEmptyMessage => !IsLoading && ConsolidatedProducts.Count == 0;

        public Visibility IsLoadingVisibility => IsLoading ? Visibility.Visible : Visibility.Collapsed;
        public Visibility ShowEmptyMessageVisibility => ShowEmptyMessage ? Visibility.Visible : Visibility.Collapsed;

        public decimal TotalItems => ConsolidatedProducts.Sum(p => p.TotalQuantity);
        public decimal TotalGrossPurchaseCost => ConsolidatedProducts.Sum(p => p.GrossPurchaseCost);
        public decimal TotalGrossSales => ConsolidatedProducts.Sum(p => p.GrossSalesAmount);

        public decimal TotalDeducted => ConsolidatedProducts.Sum(p => p.DeductedFromInventory);
        public decimal TotalInventoryDeductedPurchaseCost => ConsolidatedProducts.Sum(p => p.InventoryDeductedPurchaseCost);
        public decimal TotalInventoryDeductedSales => ConsolidatedProducts.Sum(p => p.InventoryDeductedSalesAmount);

        public decimal TotalNetToOrder => ConsolidatedProducts.Sum(p => p.NetQuantityToOrder);
        public decimal TotalEstimatedCost => ConsolidatedProducts.Sum(p => p.TotalPurchaseCost);
        public decimal TotalEstimatedSales => ConsolidatedProducts.Sum(p => p.DisplayTotalSales);
        public decimal TotalProfitMargin => ConsolidatedProducts.Sum(p => p.DisplayProfit);
        public decimal ProfitMarginPercentage => TotalEstimatedSales > 0 ? (TotalProfitMargin / TotalEstimatedSales) * 100m : 0m;

        public string TotalDeductedDisplay => $"{TotalDeducted:N2} pzas";
        public string TotalInventoryDeductedPurchaseCostDisplay => $"Val. Compra: {TotalInventoryDeductedPurchaseCost:C2}";
        public string TotalNetToOrderDisplay => $"{TotalNetToOrder:N2} pzas faltantes";

        public string TotalEstimatedCostDisplay => $"{TotalEstimatedCost:C2}";
        public string TotalEstimatedSalesDisplay => $"{TotalEstimatedSales:C2}";
        public string TotalProfitMarginDisplay => $"{TotalProfitMargin:C2}";
        public string ProfitMarginPercentageDisplay => $"{ProfitMarginPercentage:N1}%";

        public ObservableCollection<ConsolidatedProductDto> ConsolidatedProducts { get; } = new();

        public RecentOrdersReportDialog(SalesApiClient salesApiClient, CustomerApiClient? customerApiClient, INotificationService notificationService, string? targetStatus = "EnProceso")
        {
            InitializeComponent();
            DataContext = this;
            _salesApiClient = salesApiClient;
            _customerApiClient = customerApiClient;
            _notificationService = notificationService;
            _targetStatus = string.IsNullOrWhiteSpace(targetStatus) || targetStatus == "-- Todos --" ? "Recibido" : targetStatus;

            ConsolidatedProducts.CollectionChanged += (s, e) => NotifyTotals();
            Loaded += RecentOrdersReportDialog_Loaded;
        }

        private void NotifyTotals()
        {
            OnPropertyChanged(nameof(HasData));
            OnPropertyChanged(nameof(ShowEmptyMessage));
            OnPropertyChanged(nameof(ShowEmptyMessageVisibility));
            OnPropertyChanged(nameof(TotalItems));
            OnPropertyChanged(nameof(TotalGrossPurchaseCost));
            OnPropertyChanged(nameof(TotalGrossSales));
            OnPropertyChanged(nameof(TotalDeducted));
            OnPropertyChanged(nameof(TotalInventoryDeductedPurchaseCost));
            OnPropertyChanged(nameof(TotalInventoryDeductedSales));
            OnPropertyChanged(nameof(TotalNetToOrder));
            OnPropertyChanged(nameof(TotalEstimatedCost));
            OnPropertyChanged(nameof(TotalEstimatedSales));
            OnPropertyChanged(nameof(TotalProfitMargin));
            OnPropertyChanged(nameof(ProfitMarginPercentage));

            OnPropertyChanged(nameof(TotalDeductedDisplay));
            OnPropertyChanged(nameof(TotalInventoryDeductedPurchaseCostDisplay));
            OnPropertyChanged(nameof(TotalNetToOrderDisplay));
            OnPropertyChanged(nameof(TotalEstimatedCostDisplay));
            OnPropertyChanged(nameof(TotalEstimatedSalesDisplay));
            OnPropertyChanged(nameof(TotalProfitMarginDisplay));
            OnPropertyChanged(nameof(ProfitMarginPercentageDisplay));
        }

        private async void RecentOrdersReportDialog_Loaded(object sender, RoutedEventArgs e)
        {
            await LoadRoutesAsync();
            await LoadReportAsync();
        }

        private async Task LoadRoutesAsync()
        {
            if (_customerApiClient == null) return;
            try
            {
                var routes = await _customerApiClient.GetRoutesAsync();
                Routes.Clear();
                Routes.Add(new RouteDto(Guid.Empty, "ALL", "-- Todas las Rutas --", true));
                foreach (var r in routes.Where(x => x.IsActive))
                {
                    Routes.Add(r);
                }
                SelectedRoute = Routes[0];
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error al cargar rutas en dialogo: {ex.Message}");
            }
        }

        private async void RefreshReport_Click(object sender, RoutedEventArgs e)
        {
            await LoadReportAsync();
        }

        private async Task LoadReportAsync()
        {
            IsLoading = true;
            try
            {
                Guid? routeId = SelectedRoute != null && SelectedRoute.Id != Guid.Empty ? SelectedRoute.Id : null;
                var list = await _salesApiClient.GetConsolidatedProductsAsync(null, _targetStatus, FromDate, ToDate, routeId);
                
                Dictionary<string, string> descriptionMap = new(StringComparer.OrdinalIgnoreCase);
                try
                {
                    var productApiClient = App.AppHost?.Services.GetService<ProductApiClient>();
                    if (productApiClient != null)
                    {
                        var pagedResult = await productApiClient.GetProductsPagedAsync(1, 5000);
                        if (pagedResult?.Items != null)
                        {
                            foreach (var p in pagedResult.Items)
                            {
                                if (!string.IsNullOrWhiteSpace(p.InternalCode) && !string.IsNullOrWhiteSpace(p.Description))
                                {
                                    descriptionMap[p.InternalCode] = p.Description;
                                }
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"Failed to load product descriptions map: {ex.Message}");
                }

                ConsolidatedProducts.Clear();
                foreach (var item in list)
                {
                    string displayName = item.ProductName;
                    if (!string.IsNullOrWhiteSpace(item.ProductCode) && descriptionMap.TryGetValue(item.ProductCode, out var desc) && !string.IsNullOrWhiteSpace(desc))
                    {
                        displayName = desc;
                    }

                    ConsolidatedProducts.Add(item with { ProductName = displayName });
                }
                
                NotifyTotals();
            }
            catch (Exception ex)
            {
                _notificationService.ShowError($"Error al generar el resumen de pedidos: {ex.Message}");
            }
            finally
            {
                IsLoading = false;
            }
        }

        private async void ConfirmResumen_Click(object sender, RoutedEventArgs e)
        {
            if (ConsolidatedProducts.Count == 0) return;

            string actionMsg = _targetStatus.Equals("EnProceso", StringComparison.OrdinalIgnoreCase)
                ? "confirmar el despacho de la carga y cambiar los pedidos a 'En Camino'"
                : $"confirmar este resumen y pasar los pedidos a 'En Proceso'";

            var confirm = Views.Dialogs.CustomMessageBox.Show(
                $"¿Está seguro de que desea {actionMsg}?",
                "Confirmar Operación",
                MessageBoxButton.YesNo,
                MessageBoxImage.Question);

            if (confirm != MessageBoxResult.Yes) return;

            IsLoading = true;
            try
            {
                Guid? routeId = SelectedRoute != null && SelectedRoute.Id != Guid.Empty ? SelectedRoute.Id : null;
                var result = await _salesApiClient.GetSalesOrdersPagedAsync(1, 9999, routeId, _targetStatus, FromDate, ToDate);
                if (result?.Items == null || !result.Items.Any())
                {
                    _notificationService.ShowWarning($"No se encontraron pedidos en estado '{_targetStatus}' para procesar.");
                    return;
                }

                var resultsBag = new System.Collections.Concurrent.ConcurrentBag<bool>();

                if (_targetStatus.Equals("EnProceso", StringComparison.OrdinalIgnoreCase))
                {
                    // Cambiar a EnCamino (5)
                    await Parallel.ForEachAsync(result.Items, new ParallelOptions { MaxDegreeOfParallelism = 5 }, async (order, ct) =>
                    {
                        try
                        {
                            var ok = await _salesApiClient.UpdateSalesOrderStatusAsync(order.Id, 5); // 5 = EnCamino
                            resultsBag.Add(ok);
                        }
                        catch
                        {
                            resultsBag.Add(false);
                        }
                    });
                }
                else
                {
                    // Cambiar a EnProceso (4)
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
                }

                int successCount = resultsBag.Count(x => x);
                int errorCount = resultsBag.Count(x => !x);

                string messageText = _targetStatus.Equals("EnProceso", StringComparison.OrdinalIgnoreCase)
                    ? $"Despacho completado. {successCount} pedidos pasaron a estado 'En Camino'."
                    : $"Procesamiento completado. {successCount} pedidos pasaron a estado 'En Proceso'.";

                _notificationService.ShowSuccess(messageText + (errorCount > 0 ? $" ({errorCount} errores)." : ""));

                await LoadReportAsync();
                DialogResult = true;
            }
            catch (Exception ex)
            {
                _notificationService.ShowError($"Error al procesar: {ex.Message}");
            }
            finally
            {
                IsLoading = false;
            }
        }

        private void ExportExcel_Click(object sender, RoutedEventArgs e)
        {
            if (ConsolidatedProducts.Count == 0) return;

            string defaultFileName = _targetStatus.Equals("EnProceso", StringComparison.OrdinalIgnoreCase)
                ? $"Consolidado_Despacho_EnProceso_{DateTime.Today:yyyyMMdd}.xlsx"
                : $"Consolidado_Compras_Recibido_{DateTime.Today:yyyyMMdd}.xlsx";

            var saveFileDialog = new Microsoft.Win32.SaveFileDialog
            {
                Filter = "Libro de Excel (*.xlsx)|*.xlsx",
                FileName = defaultFileName
            };

            if (saveFileDialog.ShowDialog() == true)
            {
                try
                {
                    ExcelExportService.ExportConsolidationToExcel(ConsolidatedProducts, saveFileDialog.FileName, GeneralObservations);
                    _notificationService.ShowSuccess("Consolidado exportado exitosamente a Excel con comprobación de inventario.");
                }
                catch (Exception ex)
                {
                    _notificationService.ShowError($"Error al exportar a Excel: {ex.Message}");
                }
            }
        }

        private void Close_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }

        public event PropertyChangedEventHandler? PropertyChanged;

        protected void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
