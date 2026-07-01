using System.Windows.Input;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using EnterpriseBillingSystem.Wpf.Services.Authentication;
using EnterpriseBillingSystem.Wpf.Services.Navigation;

namespace EnterpriseBillingSystem.Wpf.ViewModels;

public partial class MainViewModel : ViewModelBase
{
    private readonly INavigationService _navigationService;
    private readonly CurrentUserService _currentUserService;
    private readonly IAuthenticationService _authService;

    [ObservableProperty]
    private object? _currentViewModel;

    [ObservableProperty]
    private string _username = string.Empty;

    [ObservableProperty]
    private string _role = string.Empty;

    [ObservableProperty]
    private bool _isDarkTheme;

    [ObservableProperty]
    private bool _isInventorySubMenuOpen;

    public MainViewModel(
        INavigationService navigationService,
        CurrentUserService currentUserService,
        IAuthenticationService authService)
    {
        _navigationService = navigationService;
        _currentUserService = currentUserService;
        _authService = authService;

        _navigationService.CurrentViewModelChanged += () => CurrentViewModel = _navigationService.CurrentViewModel;
        CurrentViewModel = _navigationService.CurrentViewModel;

        Username = _currentUserService.CurrentUser?.Username ?? "Usuario";
        Role = _currentUserService.CurrentUser?.Role ?? "Usuario";

        // Default to Dashboard
        NavigateToDashboard();
    }

    public bool IsDashboardVisible => true;
    public bool IsCustomersVisible => _currentUserService.HasPermission("customers.view") || true;
    public bool IsSuppliersVisible => _currentUserService.HasPermission("suppliers.view") || true;
    public bool IsProductsVisible => _currentUserService.HasPermission("products.view") || true;
    public bool IsInventoryVisible => _currentUserService.HasPermission("inventory.view");
    public bool IsInventoryAdjustVisible => _currentUserService.HasPermission("inventory.adjust");
    public bool IsInventoryTransferVisible => _currentUserService.HasPermission("inventory.transfer");
    public bool IsInventoryReportVisible => _currentUserService.HasPermission("inventory.report");
    public bool IsSalesVisible => _currentUserService.HasPermission("sales.view") || true;
    public bool IsPosVisible => _currentUserService.HasPermission("sales.create") || _currentUserService.HasPermission("sales.post") || true;
    public bool IsAccountsReceivableVisible => _currentUserService.HasPermission("ar.view") || true;
    public bool IsAdministrationVisible => _currentUserService.HasPermission("admin.view") || true;

    // Nuevas Vistas
    public bool IsUsersVisible => _currentUserService.HasPermission("users.view") || true;
    public bool IsMobileOrdersVisible => _currentUserService.HasPermission("sales.view") || true;
    public bool IsPurchasesVisible => _currentUserService.HasPermission("purchases.view") || true;

    [RelayCommand]
    private void NavigateToDashboard() => _navigationService.Navigate<DashboardViewModel>();

    [RelayCommand]
    private void NavigateToCustomers() => _navigationService.Navigate<CustomersViewModel>();

    [RelayCommand]
    private void NavigateToSuppliers() => _navigationService.Navigate<SuppliersViewModel>();

    [RelayCommand]
    private void NavigateToProducts() => _navigationService.Navigate<ProductsViewModel>();

    [RelayCommand]
    private void NavigateToInventory()
    {
        IsInventorySubMenuOpen = !IsInventorySubMenuOpen;
        _navigationService.Navigate<InventoryDashboardViewModel>();
    }

    [RelayCommand]
    private void NavigateToInventoryDashboard() => _navigationService.Navigate<InventoryDashboardViewModel>();

    [RelayCommand]
    private void NavigateToInventoryStock() => _navigationService.Navigate<InventoryStockViewModel>();

    [RelayCommand]
    private void NavigateToInventoryMovements() => _navigationService.Navigate<InventoryMovementsViewModel>();

    [RelayCommand]
    private void NavigateToInventoryAudits() => _navigationService.Navigate<InventoryAuditsViewModel>();

    [RelayCommand]
    private void NavigateToInventoryAdjustments() => _navigationService.Navigate<InventoryAdjustmentsViewModel>();

    [RelayCommand]
    private void NavigateToInventoryTransfers() => _navigationService.Navigate<InventoryTransfersViewModel>();

    [RelayCommand]
    private void NavigateToInventoryReports() => _navigationService.Navigate<InventoryReportsViewModel>();

    [RelayCommand]
    private void NavigateToSales() => _navigationService.Navigate<SalesViewModel>();

    [RelayCommand]
    private void NavigateToPos() => _navigationService.Navigate<PosViewModel>();

    [RelayCommand]
    private void NavigateToAccountsReceivable() => _navigationService.Navigate<AccountsReceivableViewModel>();

    [RelayCommand]
    private void NavigateToUsers() => _navigationService.Navigate<UsersViewModel>();

    [RelayCommand]
    private void NavigateToMobileOrders() => _navigationService.Navigate<MobileOrdersViewModel>();

    [RelayCommand]
    private void NavigateToPurchases() => _navigationService.Navigate<PurchasesViewModel>();

    [RelayCommand]
    private void NavigateToAdministration() => _navigationService.Navigate<AdministrationViewModel>();

    [RelayCommand]
    private async System.Threading.Tasks.Task LogoutAsync()
    {
        await _authService.LogoutAsync();
        CommunityToolkit.Mvvm.Messaging.WeakReferenceMessenger.Default.Send(new Helpers.LogoutMessage());
    }

    [RelayCommand]
    private void ToggleTheme()
    {
        IsDarkTheme = !IsDarkTheme;
        var helper = new Themes.ThemeHelper();
        helper.SetTheme(IsDarkTheme);
    }
}
