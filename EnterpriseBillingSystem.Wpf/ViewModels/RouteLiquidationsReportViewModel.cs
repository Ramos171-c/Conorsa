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

public partial class RouteLiquidationsReportViewModel : ObservableObject
{
    private readonly SalesApiClient _salesApiClient;
    private readonly CustomerApiClient _customerApiClient;
    private readonly INotificationService _notificationService;

    [ObservableProperty]
    private DateTime? _fromDate = DateTime.Today.AddDays(-7);

    [ObservableProperty]
    private DateTime? _toDate = DateTime.Today;

    [ObservableProperty]
    private RouteDto? _selectedRoute;

    [ObservableProperty]
    private string? _selectedStatus = "ALL";

    [ObservableProperty]
    private bool _isLoading;

    [ObservableProperty]
    private RouteLiquidationFullDto? _selectedLiquidation;

    public ObservableCollection<RouteDto> Routes { get; } = new();
    public ObservableCollection<RouteLiquidationListItemDto> Liquidations { get; } = new();

    public decimal TotalAmountSold => Liquidations.Sum(l => l.TotalAmountSold);
    public decimal TotalAmountReturned => Liquidations.Sum(l => l.TotalAmountReturned);
    public decimal TotalProfit => Liquidations.Sum(l => l.EstimatedProfit);

    public RouteLiquidationsReportViewModel(
        SalesApiClient salesApiClient,
        CustomerApiClient customerApiClient,
        INotificationService notificationService)
    {
        _salesApiClient = salesApiClient;
        _customerApiClient = customerApiClient;
        _notificationService = notificationService;
    }

    public async Task InitializeAsync()
    {
        await LoadRoutesAsync();
        await LoadLiquidationsAsync();
    }

    private async Task LoadRoutesAsync()
    {
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
            System.Diagnostics.Debug.WriteLine($"Error al cargar rutas: {ex.Message}");
        }
    }

    [RelayCommand]
    public async Task LoadLiquidationsAsync()
    {
        try
        {
            IsLoading = true;
            Guid? routeId = SelectedRoute != null && SelectedRoute.Id != Guid.Empty ? SelectedRoute.Id : null;
            string? status = SelectedStatus != "ALL" ? SelectedStatus : null;

            var result = await _salesApiClient.GetRouteLiquidationsPagedAsync(1, 1000, FromDate, ToDate, routeId, status);
            Liquidations.Clear();
            if (result?.Items != null)
            {
                foreach (var item in result.Items)
                {
                    Liquidations.Add(item);
                }
            }

            OnPropertyChanged(nameof(TotalAmountSold));
            OnPropertyChanged(nameof(TotalAmountReturned));
            OnPropertyChanged(nameof(TotalProfit));
        }
        catch (Exception ex)
        {
            _notificationService.ShowError($"Error al consultar liquidaciones: {ex.Message}");
        }
        finally
        {
            IsLoading = false;
        }
    }

    [RelayCommand]
    private async Task ViewDetailsAsync(RouteLiquidationListItemDto? item)
    {
        if (item == null) return;
        try
        {
            IsLoading = true;
            SelectedLiquidation = await _salesApiClient.GetRouteLiquidationByIdAsync(item.Id);
        }
        catch (Exception ex)
        {
            _notificationService.ShowError($"Error al obtener detalle de la liquidación: {ex.Message}");
        }
        finally
        {
            IsLoading = false;
        }
    }
}
