using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using EnterpriseBillingSystem.Wpf.Models;
using EnterpriseBillingSystem.Wpf.Services.Api;

namespace EnterpriseBillingSystem.Wpf.ViewModels;

public partial class DashboardViewModel : ViewModelBase
{
    private readonly SalesApiClient _salesApiClient;
    private readonly CustomerApiClient _customerApiClient;
    private readonly UserApiClient _userApiClient;
    private readonly ProductApiClient _productApiClient;

    [ObservableProperty]
    private decimal _salesToday;

    [ObservableProperty]
    private int _ordersToday;

    [ObservableProperty]
    private decimal _profitToday;

    [ObservableProperty]
    private double _profitMarginToday;

    [ObservableProperty]
    private decimal _globalGoal = 100000m;

    [ObservableProperty]
    private double _globalProgressPercentage;

    [ObservableProperty]
    private bool _isLoading;

    public ObservableCollection<SalespersonGoalDto> SalespersonGoals { get; } = new();

    public DashboardViewModel(SalesApiClient salesApiClient, CustomerApiClient customerApiClient, UserApiClient userApiClient, ProductApiClient productApiClient)
    {
        _salesApiClient = salesApiClient;
        _customerApiClient = customerApiClient;
        _userApiClient = userApiClient;
        _productApiClient = productApiClient;

        _ = LoadDashboardDataAsync();
    }

    [RelayCommand]
    public async Task LoadDashboardDataAsync()
    {
        IsLoading = true;
        try
        {
            // Fetch ultra-fast consolidated summary from backend for today
            var summary = await _salesApiClient.GetDashboardSummaryAsync(DateTime.Today, DateTime.Today);

            if (summary != null)
            {
                SalesToday = summary.SalesToday;
                OrdersToday = summary.OrdersToday;
                ProfitToday = summary.ProfitToday;
                ProfitMarginToday = summary.ProfitMarginToday;
                GlobalGoal = summary.GlobalGoal > 0 ? summary.GlobalGoal : 100000m;
                GlobalProgressPercentage = summary.GlobalProgressPercentage;

                SalespersonGoals.Clear();
                if (summary.SalespersonGoals != null)
                {
                    foreach (var seller in summary.SalespersonGoals)
                    {
                        SalespersonGoals.Add(seller);
                    }
                }
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error loading dashboard summary: {ex.Message}");
        }
        finally
        {
            IsLoading = false;
        }
    }
}
