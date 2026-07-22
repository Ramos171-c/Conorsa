using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using System.Windows;
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

        private bool _isLoading;
        private string _generalObservations = string.Empty;

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
                    OnPropertyChanged(nameof(ShowEmptyMessage));
                }
            }
        }

        public string GeneralObservations
        {
            get => _generalObservations;
            set
            {
                if (_generalObservations != value)
                {
                    _generalObservations = value;
                    OnPropertyChanged();
                }
            }
        }

        public bool HasData => ConsolidatedProducts.Count > 0;
        public bool ShowEmptyMessage => !IsLoading && ConsolidatedProducts.Count == 0;

        public decimal TotalItems => ConsolidatedProducts.Sum(p => p.TotalQuantity);
        public decimal TotalGrossPurchaseCost => ConsolidatedProducts.Sum(p => p.GrossPurchaseCost);
        public decimal TotalGrossSales => ConsolidatedProducts.Sum(p => p.GrossSalesAmount);

        public decimal TotalDeducted => ConsolidatedProducts.Sum(p => p.DeductedFromInventory);
        public decimal TotalInventoryDeductedPurchaseCost => ConsolidatedProducts.Sum(p => p.InventoryDeductedPurchaseCost);
        public decimal TotalInventoryDeductedSales => ConsolidatedProducts.Sum(p => p.InventoryDeductedSalesAmount);

        public decimal TotalNetToOrder => ConsolidatedProducts.Sum(p => p.NetQuantityToOrder);
        public decimal TotalEstimatedCost => ConsolidatedProducts.Sum(p => p.TotalPurchaseCost);
        public decimal TotalEstimatedSales => ConsolidatedProducts.Sum(p => p.NetSalesAmount);
        public decimal TotalProfitMargin => TotalEstimatedSales - TotalEstimatedCost;
        public decimal ProfitMarginPercentage => TotalEstimatedSales > 0 ? (TotalProfitMargin / TotalEstimatedSales) * 100m : 0m;

        public string TotalDeductedDisplay => $"{TotalDeducted:N2} pzas";
        public string TotalInventoryDeductedPurchaseCostDisplay => $"Val. Compra: {TotalInventoryDeductedPurchaseCost:C2}";
        public string TotalNetToOrderDisplay => $"{TotalNetToOrder:N2} pzas a pedir";

        public string TotalEstimatedCostDisplay => $"{TotalEstimatedCost:C2}";
        public string TotalEstimatedSalesDisplay => $"{TotalEstimatedSales:C2}";
        public string TotalProfitMarginDisplay => $"{TotalProfitMargin:C2}";
        public string ProfitMarginPercentageDisplay => $"{ProfitMarginPercentage:N1}%";

        public ObservableCollection<ConsolidatedProductDto> ConsolidatedProducts { get; } = new();

        public RecentOrdersReportDialog(SalesApiClient salesApiClient, INotificationService notificationService)
        {
            DataContext = this;
            InitializeComponent();
            _salesApiClient = salesApiClient;
            _notificationService = notificationService;

            Loaded += RecentOrdersReportDialog_Loaded;
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
                // Query only "Recibido" orders with no date filters
                var list = await _salesApiClient.GetConsolidatedProductsAsync(null, "Recibido", null, null);
                
                ConsolidatedProducts.Clear();
                foreach (var item in list)
                {
                    ConsolidatedProducts.Add(item);
                }
                
                OnPropertyChanged(nameof(HasData));
                OnPropertyChanged(nameof(ShowEmptyMessage));
                OnPropertyChanged(nameof(TotalItems));
                OnPropertyChanged(nameof(TotalDeducted));
                OnPropertyChanged(nameof(TotalNetToOrder));
                OnPropertyChanged(nameof(TotalEstimatedCost));
                OnPropertyChanged(nameof(TotalEstimatedSales));
                OnPropertyChanged(nameof(TotalProfitMargin));
                OnPropertyChanged(nameof(ProfitMarginPercentage));
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
                "¿Está seguro de que desea confirmar este resumen? Esto cambiará el estado de TODOS los pedidos en estado 'Recibido' a 'En Proceso'.",
                "Confirmar Resumen",
                MessageBoxButton.YesNo,
                MessageBoxImage.Question);

            if (confirm != MessageBoxResult.Yes) return;

            IsLoading = true;
            try
            {
                var result = await _salesApiClient.GetSalesOrdersPagedAsync(1, 9999, null, "Recibido");
                if (result?.Items == null || !result.Items.Any())
                {
                    _notificationService.ShowWarning("No se encontraron pedidos en estado Recibido para procesar.");
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

                _notificationService.ShowSuccess($"Procesamiento completado. {successCount} pedidos procesados y pasados a 'En Proceso' exitosamente." + 
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
