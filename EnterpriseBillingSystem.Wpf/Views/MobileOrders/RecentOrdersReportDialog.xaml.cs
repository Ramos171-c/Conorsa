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

namespace EnterpriseBillingSystem.Wpf.Views.MobileOrders
{
    public partial class RecentOrdersReportDialog : Window, INotifyPropertyChanged
    {
        private readonly SalesApiClient _salesApiClient;
        private readonly INotificationService _notificationService;

        private bool _isLoading;

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

        public bool HasData => ConsolidatedProducts.Count > 0;
        public bool ShowEmptyMessage => !IsLoading && ConsolidatedProducts.Count == 0;

        public decimal TotalItems => ConsolidatedProducts.Sum(p => p.TotalQuantity);
        public decimal TotalEstimatedCost => ConsolidatedProducts.Sum(p => p.TotalCost);

        public ObservableCollection<ConsolidatedProductDto> ConsolidatedProducts { get; } = new();

        public RecentOrdersReportDialog(SalesApiClient salesApiClient, INotificationService notificationService)
        {
            InitializeComponent();
            _salesApiClient = salesApiClient;
            _notificationService = notificationService;

            DataContext = this;

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
                OnPropertyChanged(nameof(TotalEstimatedCost));
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

                _notificationService.ShowSuccess($"Procesamiento completado. {successCount} pedidos procesados y pasados a 'En Proceso' exitosamente." + 
                    (errorCount > 0 ? $" ({errorCount} errores)." : ""));

                // 3. Clear/Refresh report
                await LoadReportAsync();
                
                // Set DialogResult so parent knows it has changed and needs refresh
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

        private void ExportCsv_Click(object sender, RoutedEventArgs e)
        {
            if (ConsolidatedProducts.Count == 0) return;

            var saveFileDialog = new Microsoft.Win32.SaveFileDialog
            {
                Filter = "Archivo CSV (*.csv)|*.csv",
                FileName = $"Resumen_Pedidos_Recibidos_{DateTime.Today:yyyyMMdd}.csv"
            };

            if (saveFileDialog.ShowDialog() == true)
            {
                try
                {
                    using (var writer = new System.IO.StreamWriter(saveFileDialog.FileName, false, System.Text.Encoding.UTF8))
                    {
                        writer.WriteLine("sep=;");
                        writer.WriteLine("Codigo;Producto;U.M.;Cantidad Total;Total Estimado");
                        foreach (var item in ConsolidatedProducts)
                        {
                            var code = EscapeCsv(item.ProductCode);
                            var name = EscapeCsv(item.ProductName);
                            var uom = EscapeCsv(item.UnitOfMeasure);
                            var qty = item.TotalQuantity.ToString("F2", System.Globalization.CultureInfo.InvariantCulture);
                            var amount = item.TotalCost.ToString("F2", System.Globalization.CultureInfo.InvariantCulture);

                            writer.WriteLine($"{code};{name};{uom};{qty};{amount}");
                        }
                    }
                    _notificationService.ShowSuccess("Resumen exportado exitosamente a CSV.");
                }
                catch (Exception ex)
                {
                    _notificationService.ShowError($"Error al exportar a CSV: {ex.Message}");
                }
            }
        }

        private string EscapeCsv(string val)
        {
            if (string.IsNullOrEmpty(val)) return string.Empty;
            val = val.Replace(";", ",");
            if (val.Contains("\"") || val.Contains("\n") || val.Contains("\r"))
            {
                return "\"" + val.Replace("\"", "\"\"") + "\"";
            }
            return val;
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
