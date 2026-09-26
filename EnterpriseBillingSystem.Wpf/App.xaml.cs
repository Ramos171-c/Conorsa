using System;
using System.IO;
using System.Net.Http;
using System.Windows;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using EnterpriseBillingSystem.Wpf.Services.Storage;
using EnterpriseBillingSystem.Wpf.Services.Authentication;
using EnterpriseBillingSystem.Wpf.Services.Navigation;
using EnterpriseBillingSystem.Wpf.Services.Dialogs;
using EnterpriseBillingSystem.Wpf.Services.Api;
using EnterpriseBillingSystem.Wpf.Services;
using EnterpriseBillingSystem.Wpf.ViewModels;
using EnterpriseBillingSystem.Wpf.Views;
using EnterpriseBillingSystem.Wpf.Views.Login;
using EnterpriseBillingSystem.Wpf.Helpers;
using EnterpriseBillingSystem.Wpf.Converters;

namespace EnterpriseBillingSystem.Wpf;

public partial class App : Application
{
    public static IHost? AppHost { get; private set; }

    public App()
    {
        AppHost = Host.CreateDefaultBuilder()
            .ConfigureAppConfiguration((context, config) =>
            {
                config.SetBasePath(AppDomain.CurrentDomain.BaseDirectory);
                config.AddJsonFile("appsettings.json", optional: false, reloadOnChange: true);
            })
            .ConfigureServices((context, services) =>
            {
                ConfigureServices(context.Configuration, services);
            })
            .Build();
    }

    private void ConfigureServices(IConfiguration configuration, IServiceCollection services)
    {
        // Settings
        services.AddSingleton(configuration);

        // Storage & Services
        services.AddSingleton<ILocalStorageService, LocalStorageService>();
        services.AddSingleton<CurrentUserService>();
        services.AddSingleton<IAuthenticationService, AuthenticationService>();
        services.AddSingleton<INavigationService, NavigationService>();
        services.AddSingleton<INotificationService, NotificationService>();
        services.AddSingleton<IReceiptPrinterService, ReceiptPrinterService>();

        // HTTP Delegating Handler for JWT
        services.AddTransient<JwtAuthHeaderHandler>();

        // Register typed HttpClients
        var baseUrl = configuration.GetValue<string>("ApiSettings:BaseUrl") ?? "https://localhost:7228/api/v1/";

        // Set the media base URL for image loading (host + port, no /api/v1 path)
        var apiUri = new Uri(baseUrl);
        RelativeImageUrlConverter.ApiImageBaseUrl = $"{apiUri.Scheme}://{apiUri.Authority}";

        Action<HttpClient> configureClient = client =>
        {
            client.BaseAddress = new Uri(baseUrl);
            client.Timeout = TimeSpan.FromSeconds(15);
        };

        services.AddHttpClient<AuthApiClient>(configureClient).AddHttpMessageHandler<JwtAuthHeaderHandler>();
        services.AddHttpClient<CustomerApiClient>(configureClient).AddHttpMessageHandler<JwtAuthHeaderHandler>();
        services.AddHttpClient<SupplierApiClient>(configureClient).AddHttpMessageHandler<JwtAuthHeaderHandler>();
        services.AddHttpClient<ProductApiClient>(configureClient).AddHttpMessageHandler<JwtAuthHeaderHandler>();
        services.AddHttpClient<InventoryApiClient>(configureClient).AddHttpMessageHandler<JwtAuthHeaderHandler>();
        services.AddHttpClient<SalesApiClient>(configureClient).AddHttpMessageHandler<JwtAuthHeaderHandler>();
        services.AddHttpClient<PurchaseApiClient>(configureClient).AddHttpMessageHandler<JwtAuthHeaderHandler>();
        services.AddHttpClient<CashApiClient>(configureClient).AddHttpMessageHandler<JwtAuthHeaderHandler>();
        services.AddHttpClient<AccountsReceivableApiClient>(configureClient).AddHttpMessageHandler<JwtAuthHeaderHandler>();
        services.AddHttpClient<AccountsPayableApiClient>(configureClient).AddHttpMessageHandler<JwtAuthHeaderHandler>();
        services.AddHttpClient<AccountingApiClient>(configureClient).AddHttpMessageHandler<JwtAuthHeaderHandler>();
        services.AddHttpClient<TreasuryApiClient>(configureClient).AddHttpMessageHandler<JwtAuthHeaderHandler>();
        services.AddHttpClient<FixedAssetsApiClient>(configureClient).AddHttpMessageHandler<JwtAuthHeaderHandler>();
        services.AddHttpClient<PosApiClient>(configureClient).AddHttpMessageHandler<JwtAuthHeaderHandler>();
        services.AddHttpClient<UserApiClient>(configureClient).AddHttpMessageHandler<JwtAuthHeaderHandler>();
        services.AddHttpClient<AdministrationApiClient>(configureClient).AddHttpMessageHandler<JwtAuthHeaderHandler>();

        // Register ViewModels
        services.AddSingleton<MainViewModel>();
        services.AddTransient<LoginViewModel>();
        services.AddTransient<DashboardViewModel>();
        services.AddTransient<CustomersViewModel>();
        services.AddTransient<CustomerEditorViewModel>();
        services.AddTransient<SuppliersViewModel>();
        services.AddTransient<SupplierEditorViewModel>();
        services.AddTransient<ProductsViewModel>();
        services.AddTransient<InventoryViewModel>();
        services.AddTransient<InventoryDashboardViewModel>();
        services.AddTransient<InventoryStockViewModel>();
        services.AddTransient<InventoryAdjustmentsViewModel>();
        services.AddTransient<InventoryTransfersViewModel>();
        services.AddTransient<InventoryReportsViewModel>();
        services.AddTransient<InventoryMovementsViewModel>();
        services.AddTransient<InventoryAuditsViewModel>();
        services.AddTransient<SalesViewModel>();
        services.AddTransient<PurchasesViewModel>();
        services.AddTransient<CashViewModel>();
        services.AddTransient<AccountsReceivableViewModel>();
        services.AddTransient<AccountsPayableViewModel>();
        services.AddTransient<AccountingViewModel>();
        services.AddTransient<TreasuryViewModel>();
        services.AddTransient<FixedAssetsViewModel>();
        services.AddTransient<AdministrationViewModel>();
        services.AddTransient<PosViewModel>();
        services.AddTransient<UsersViewModel>();
        services.AddTransient<MobileOrdersViewModel>();
        services.AddTransient<MobileOrderDetailViewModel>();
        services.AddTransient<ReportsDashboardViewModel>();

        // Register Windows/Views
        services.AddTransient<LoginWindow>();
        services.AddTransient<ShellWindow>();
        services.AddTransient<Views.Pos.PosView>();
        services.AddTransient<Views.Inventory.InventoryDashboardView>();
        services.AddTransient<Views.Inventory.ProductsView>();
        services.AddTransient<Views.Inventory.InventoryStockView>();
        services.AddTransient<Views.Inventory.InventoryAdjustmentsView>();
        services.AddTransient<Views.Inventory.InventoryTransfersView>();
        services.AddTransient<Views.Inventory.InventoryReportsView>();
        services.AddTransient<Views.Inventory.InventoryMovementsView>();
        services.AddTransient<Views.Inventory.InventoryAuditsView>();
        services.AddTransient<Views.Users.UsersView>();
        services.AddTransient<Views.Customers.CustomersView>();
        services.AddTransient<Views.Customers.CustomerEditorDialog>();
        services.AddTransient<Views.Suppliers.SuppliersView>();
        services.AddTransient<Views.Suppliers.SupplierEditorDialog>();
        services.AddTransient<Views.MobileOrders.MobileOrdersView>();
        services.AddTransient<Views.MobileOrders.MobileOrderDetailDialog>();
        services.AddTransient<Views.MobileOrders.RecentOrdersReportDialog>();
        services.AddTransient<Views.Reports.ReportsDashboardView>();
        services.AddTransient<Views.Dialogs.CustomMessageBox>();
        services.AddTransient<Views.Dialogs.CustomInputDialog>();
        services.AddTransient<Views.Purchases.PurchasesView>();
        services.AddTransient<Views.SalesView>();
        services.AddTransient<Views.AdministrationView>();
    }

    protected override async void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        try
        {
            await AppHost!.StartAsync();

            // Check auto-login (max 3 seconds)
            var authService = AppHost.Services.GetRequiredService<IAuthenticationService>();
            var autoLoginSuccess = await authService.AutoLoginAsync();

            if (autoLoginSuccess)
            {
                var shellWindow = AppHost.Services.GetRequiredService<ShellWindow>();
                MainWindow = shellWindow;
                shellWindow.Show();
            }
            else
            {
                var loginWindow = AppHost.Services.GetRequiredService<LoginWindow>();
                MainWindow = loginWindow;
                loginWindow.Show();
            }
        }
        catch (Exception ex)
        {
            try
            {
                var loginWindow = AppHost?.Services.GetService<LoginWindow>() 
                                  ?? new LoginWindow(new ViewModels.LoginViewModel(
                                      AppHost!.Services.GetRequiredService<IAuthenticationService>(),
                                      AppHost!.Services.GetRequiredService<Services.Dialogs.INotificationService>()));
                MainWindow = loginWindow;
                loginWindow.Show();
            }
            catch
            {
                MessageBox.Show($"Error al inicializar la aplicación: {ex.Message}", "Error de Inicio", MessageBoxButton.OK, MessageBoxImage.Error);
                Shutdown();
            }
        }
    }

    protected override async void OnExit(ExitEventArgs e)
    {
        if (AppHost != null)
        {
            await AppHost.StopAsync();
            AppHost.Dispose();
        }
        base.OnExit(e);
    }
}
