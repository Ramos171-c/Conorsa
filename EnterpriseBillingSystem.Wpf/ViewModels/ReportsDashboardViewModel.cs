using System;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Threading.Tasks;
using System.Windows.Input;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using EnterpriseBillingSystem.Wpf.Services;
using EnterpriseBillingSystem.Wpf.Services.Dialogs;

namespace EnterpriseBillingSystem.Wpf.ViewModels;

public partial class ReportsDashboardViewModel : ObservableObject
{
    private readonly INotificationService _notificationService;

    [ObservableProperty]
    private bool _isLoading;

    [ObservableProperty]
    private DateTime? _fromDate = DateTime.UtcNow.AddDays(-7);

    [ObservableProperty]
    private DateTime? _toDate = DateTime.UtcNow;

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

    public ReportsDashboardViewModel(INotificationService notificationService)
    {
        _notificationService = notificationService;
    }

    [RelayCommand]
    private void RefreshDashboard()
    {
        IsLoading = true;
        Task.Delay(500).ContinueWith(_ => IsLoading = false);
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
