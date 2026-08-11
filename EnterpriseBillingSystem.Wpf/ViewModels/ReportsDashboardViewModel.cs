using System;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Threading.Tasks;
using System.Windows.Input;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using EnterpriseBillingSystem.Wpf.Models;
using EnterpriseBillingSystem.Wpf.Services.Api;
using EnterpriseBillingSystem.Wpf.Services;
using EnterpriseBillingSystem.Wpf.Services.Dialogs;

namespace EnterpriseBillingSystem.Wpf.ViewModels;

public partial class ReportsDashboardViewModel : ObservableObject
{
    private readonly SalesApiClient _salesApiClient;
    private readonly INotificationService _notificationService;

    [ObservableProperty]
    private bool _isLoading;

    [ObservableProperty]
    private DateTime? _fromDate = DateTime.Today.AddDays(-30);

    [ObservableProperty]
    private DateTime? _toDate = DateTime.Today;

    [ObservableProperty]
    private string _dateRangeLabel = "Cargando...";

    // KPI Cards
    [ObservableProperty]
    private string _totalPresaleValue = "C$ 0.00";

    [ObservableProperty]
    private string _totalPresaleSub = "0 Pedidos";

    [ObservableProperty]
    private string _totalDeliveredValue = "C$ 0.00";

    [ObservableProperty]
    private string _totalDeliveredSub = "Entrega Efectiva";

    [ObservableProperty]
    private string _totalLossValue = "C$ 0.00";

    [ObservableProperty]
    private string _totalLossSub = "0 Piezas Faltantes";

    [ObservableProperty]
    private string _effectivenessValue = "100%";

    [ObservableProperty]
    private string _effectivenessSub = "Cumplimiento Global";

    public ObservableCollection<RouteReturnsChartDto> RouteReturns { get; } = new();

    public ReportsDashboardViewModel(SalesApiClient salesApiClient, INotificationService notificationService)
    {
        _salesApiClient = salesApiClient;
        _notificationService = notificationService;
        _ = LoadDashboardDataAsync();
    }

    [RelayCommand]
    public async Task LoadDashboardDataAsync()
    {
        IsLoading = true;
        try
        {
            var from = FromDate ?? DateTime.Today.AddDays(-30);
            var to = ToDate ?? DateTime.Today;
            
            // Format label
            DateRangeLabel = $"Período: {from:dd/MM/yyyy} al {to:dd/MM/yyyy}";

            var analytics = await _salesApiClient.GetDashboardAnalyticsAsync(from, to);
            if (analytics != null)
            {
                // Directly bind the metrics computed by the API
                TotalPresaleValue = analytics.TotalPresaleKpi?.Value ?? "C$ 0.00";
                TotalPresaleSub = analytics.TotalPresaleKpi?.Subtitle ?? "0 Pedidos";

                TotalDeliveredValue = analytics.TotalDeliveredKpi?.Value ?? "C$ 0.00";
                TotalDeliveredSub = analytics.TotalDeliveredKpi?.Subtitle ?? "Entrega Efectiva";

                TotalLossValue = analytics.TotalShortageLossKpi?.Value ?? "C$ 0.00";
                TotalLossSub = analytics.TotalShortageLossKpi?.Subtitle ?? "0 Piezas Faltantes";

                EffectivenessValue = analytics.GlobalEffectivenessKpi?.Value ?? "100.0%";
                EffectivenessSub = analytics.GlobalEffectivenessKpi?.Subtitle ?? "Cumplimiento de Entrega";

                RouteReturns.Clear();
                if (analytics.RouteReturns != null)
                {
                    foreach (var route in analytics.RouteReturns)
                    {
                        RouteReturns.Add(route);
                    }
                }
            }
        }
        catch (Exception ex)
        {
            _notificationService.ShowError($"Error al actualizar analytics: {ex.Message}");
        }
        finally
        {
            IsLoading = false;
        }
    }

    [RelayCommand]
    private async Task RefreshDashboardAsync()
    {
        await LoadDashboardDataAsync();
    }

    [RelayCommand]
    private void OpenSellerReportPdf()
    {
        OpenPdf("http://167.99.13.177:8080/api/v1/sales-orders/seller-report/pdf");
    }

    [RelayCommand]
    private void OpenReturnsReportPdf()
    {
        OpenPdf("http://167.99.13.177:8080/api/v1/route-liquidations/returns-report/pdf");
    }

    [RelayCommand]
    private void OpenShortagesReportPdf()
    {
        OpenPdf("http://167.99.13.177:8080/api/v1/sales-orders/shortages-report/pdf");
    }

    private void OpenPdf(string url)
    {
        try
        {
            Process.Start(new ProcessStartInfo
            {
                FileName = url,
                UseShellExecute = true
            });
        }
        catch (Exception ex)
        {
            _notificationService.ShowError($"Error al abrir reporte PDF: {ex.Message}");
        }
    }
}
