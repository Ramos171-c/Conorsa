using CommunityToolkit.Mvvm.ComponentModel;

namespace EnterpriseBillingSystem.Wpf.ViewModels;

public partial class DashboardViewModel : ViewModelBase
{
    [ObservableProperty]
    private decimal _salesToday;

    [ObservableProperty]
    private decimal _purchasesToday;

    [ObservableProperty]
    private decimal _currentCash;

    [ObservableProperty]
    private decimal _arBalance;

    [ObservableProperty]
    private decimal _apBalance;

    [ObservableProperty]
    private decimal _bankBalance;

    public DashboardViewModel()
    {
        // Initial mock data as requested
        SalesToday = 12500.50m;
        PurchasesToday = 4320.00m;
        CurrentCash = 8500.00m;
        ArBalance = 24500.75m;
        ApBalance = 15320.40m;
        BankBalance = 145000.00m;
    }
}
