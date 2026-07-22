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
        private readonly INotificationService _notificationService;
        private readonly string _targetStatus;

        private bool _isLoading;

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
        public string TotalNetToOrderDisplay => $"{TotalNetToOrder:N2} pzas a pedir";

        public string TotalEstimatedCostDisplay => $"{TotalEstimatedCost:C2}";
        public string TotalEstimatedSalesDisplay => $"{TotalEstimatedSales:C2}";
        public string TotalProfitMarginDisplay => $"{TotalProfitMargin:C2}";
        public string ProfitMarginPercentageDisplay => $"{ProfitMarginPercentage:N1}%";

        public ObservableCollection<ConsolidatedProductDto> ConsolidatedProducts { get; } = new();

        public RecentOrdersReportDialog(SalesApiClient salesApiClient, INotificationService notificationService, string? targetStatus = "EnProceso")
        {
            InitializeComponent();
            DataContext = this;
            _salesApiClient = salesApiClient;
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
            await LoadReportAsync();
        }

        private async Task LoadReportAsync()
        {
            IsLoading = true;
            try
            {
                var list = await _salesApiClient.GetConsolidatedProductsAsync(null, _targetStatus, null, null);
                
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

            var confirm = Views.Dialogs.CustomMessageBox.Show(
                $"¿Está seguro de que desea confirmar este resumen? Esto procesará los pedidos en estado '{_targetStatus}'.",
                "Confirmar Resumen",
                MessageBoxButton.YesNo,
                MessageBoxImage.Question);

            if (confirm != MessageBoxResult.Yes) return;

            IsLoading = true;
            try
            {
                var result = await _salesApiClient.GetSalesOrdersPagedAsync(1, 9999, null, _targetStatus);
                if (result?.Items == null || !result.Items.Any())
                {
                    _notificationService.ShowWarning($"No se encontraron pedidos en estado '{_targetStatus}' para procesar.");
                    return;
                }

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

                await LoadReportAsync();
                DialogResult = true;
            }
            catch (Exception ex)
            {
                _notificationService.ShowError($"Error al confirmar resumen: {ex.Message}");
            }
            finally
            {
                IsLoading = false;
            }
        }

        private void ExportExcel_Click(object sender, RoutedEventArgs e)
        {
            if (ConsolidatedProducts.Count == 0) return;

            var saveFileDialog = new Microsoft.Win32.SaveFileDialog
            {
                Filter = "Libro de Excel (*.xlsx)|*.xlsx",
                FileName = $"Consolidado_Compras_Bodega_{DateTime.Today:yyyyMMdd}.xlsx"
            };

            if (saveFileDialog.ShowDialog() == true)
            {
                try
                {
                    ExcelExportService.ExportConsolidationToExcel(ConsolidatedProducts, saveFileDialog.FileName, GeneralObservations);
                    _notificationService.ShowSuccess("Consolidado exportado exitosamente a Excel con diseño corporativo y totales recalcados.");
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
