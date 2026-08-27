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

public class ClientSalesSummary
{
    public string CustomerCode { get; set; } = string.Empty;
    public string CustomerName { get; set; } = string.Empty;
    public string ZoneName { get; set; } = "Sin Zona";
    public int OrdersCount { get; set; }
    public decimal TotalSales { get; set; }
}

public class ZoneSalesSummary
{
    public string ZoneName { get; set; } = "Sin Zona";
    public int ClientsCount { get; set; }
    public int OrdersCount { get; set; }
    public decimal TotalSales { get; set; }
}

public class SellerSalesSummary
{
    public string SellerName { get; set; } = "Sin Vendedor";
    public int OrdersCount { get; set; }
    public decimal TotalSales { get; set; }
    public decimal AverageTicket => OrdersCount > 0 ? TotalSales / OrdersCount : 0m;
}

public class DailySalesSummary
{
    public DateTime Date { get; set; }
    public int OrdersCount { get; set; }
    public decimal TotalSales { get; set; }
    public decimal AverageTicket => OrdersCount > 0 ? TotalSales / OrdersCount : 0m;
}

public partial class SalesViewModel : ViewModelBase
{
    private readonly SalesApiClient _salesApiClient;
    private readonly CustomerApiClient _customerApiClient;
    private readonly UserApiClient _userApiClient;

    [ObservableProperty]
    private DateTime _startDate = DateTime.Today.AddDays(-30);

    [ObservableProperty]
    private DateTime _endDate = DateTime.Today;

    [ObservableProperty]
    private string _searchText = string.Empty;

    [ObservableProperty]
    private string _selectedStatus = "Todos";

    [ObservableProperty]
    private decimal _totalSales;

    [ObservableProperty]
    private int _totalOrdersCount;

    [ObservableProperty]
    private decimal _averageTicket;

    [ObservableProperty]
    private string _topProduct = "Ninguno";

    [ObservableProperty]
    private bool _isLoading;

    public List<string> Statuses { get; } = new() 
    { 
        "Todos", "Borrador", "Confirmado", "Entregado", "Facturado", "Anulado" 
    };

    public ObservableCollection<SalesOrderListItemDto> FilteredOrders { get; } = new();
    public ObservableCollection<ClientSalesSummary> SalesByClient { get; } = new();
    public ObservableCollection<ZoneSalesSummary> SalesByZone { get; } = new();
    public ObservableCollection<SellerSalesSummary> SalesBySeller { get; } = new();
    public ObservableCollection<DailySalesSummary> SalesByDay { get; } = new();
    public ObservableCollection<ConsolidatedProductDto> TopSellingProducts { get; } = new();

    public string Title => "Reporte y Análisis de Ventas";

    public SalesViewModel(SalesApiClient salesApiClient, CustomerApiClient customerApiClient, UserApiClient userApiClient)
    {
        _salesApiClient = salesApiClient;
        _customerApiClient = customerApiClient;
        _userApiClient = userApiClient;

        // Auto load on creation
        _ = LoadSalesDataAsync();
    }

    // Trigger load when filters change
    partial void OnStartDateChanged(DateTime value) => _ = LoadSalesDataAsync();
    partial void OnEndDateChanged(DateTime value) => _ = LoadSalesDataAsync();
    partial void OnSelectedStatusChanged(string value) => _ = LoadSalesDataAsync();
    partial void OnSearchTextChanged(string value) => _ = LoadSalesDataAsync();

    [RelayCommand]
    public async Task LoadSalesDataAsync()
    {
        if (IsLoading) return;
        IsLoading = true;

        try
        {
            // 1. Fetch sales orders, customers, and users from API
            string? apiStatus = SelectedStatus == "Todos" ? null : SelectedStatus;
            var ordersResult = await _salesApiClient.GetSalesOrdersPagedAsync(
                page: 1, 
                pageSize: 9999, 
                status: apiStatus, 
                fromDate: StartDate.Date, 
                toDate: EndDate.Date.AddDays(1).AddSeconds(-1)
            );
            var customersResult = await _customerApiClient.GetCustomersPagedAsync(1, 9999);
            var usersResult = await _userApiClient.GetUsersPagedAsync(1, 100);

            var orders = ordersResult?.Items ?? new List<SalesOrderListItemDto>();
            var customers = customersResult?.Items ?? new List<CustomerDto>();

            // Map User ID & Username -> User Full Name
            var userMap = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            if (usersResult?.Items != null)
            {
                foreach (var u in usersResult.Items)
                {
                    string displayName = string.IsNullOrWhiteSpace(u.FullName) ? u.Username : u.FullName;
                    userMap[u.Id.ToString()] = displayName;
                    userMap[u.Username] = displayName;
                }
            }

            // Map Customer ID to Route (Zone) Name
            var customerRoutes = customers.ToDictionary(
                c => c.Id,
                c => c.RouteName ?? "Sin Zona"
            );

            // Fetch consolidated products for the date range
            var consolidatedProducts = await _salesApiClient.GetConsolidatedProductsAsync(
                fromDate: StartDate,
                toDate: EndDate.AddDays(1).AddSeconds(-1)
            );

            TopSellingProducts.Clear();
            foreach (var prod in consolidatedProducts.OrderByDescending(p => p.TotalNetAmount))
            {
                TopSellingProducts.Add(prod);
            }

            if (TopSellingProducts.Any())
            {
                TopProduct = TopSellingProducts.First().ProductName;
            }
            else
            {
                TopProduct = "Ninguno";
            }

            // Apply Date Filters to local list
            var filtered = orders.Where(o => o.OrderDate.Date >= StartDate.Date && o.OrderDate.Date <= EndDate.Date);

            // Filter by Status
            if (SelectedStatus != "Todos")
            {
                filtered = filtered.Where(o => o.Status.Equals(SelectedStatus, StringComparison.OrdinalIgnoreCase));
            }

            // Filter by Search Text (Client Name or Creator or Order Number)
            if (!string.IsNullOrWhiteSpace(SearchText))
            {
                filtered = filtered.Where(o => 
                    o.CustomerName.Contains(SearchText, StringComparison.OrdinalIgnoreCase) ||
                    o.OrderNumber.Contains(SearchText, StringComparison.OrdinalIgnoreCase) ||
                    (o.CreatedBy != null && o.CreatedBy.Contains(SearchText, StringComparison.OrdinalIgnoreCase))
                );
            }

            var finalFilteredList = filtered.ToList();

            // Populate FilteredOrders
            FilteredOrders.Clear();
            foreach (var order in finalFilteredList)
            {
                FilteredOrders.Add(order);
            }

            // 2. Compute KPI Metrics (only active/non-annulled orders for financials, or all based on status)
            var financialOrders = finalFilteredList.Where(o => !o.Status.Equals("Anulado", StringComparison.OrdinalIgnoreCase)).ToList();
            TotalSales = financialOrders.Sum(o => o.TotalAmount);
            TotalOrdersCount = financialOrders.Count;
            AverageTicket = TotalOrdersCount > 0 ? TotalSales / TotalOrdersCount : 0m;

            // 3. Compute Grouping by Client
            var clientGroups = financialOrders
                .GroupBy(o => o.CustomerId)
                .Select(g => 
                {
                    var firstOrder = g.First();
                    customerRoutes.TryGetValue(g.Key, out var zone);
                    return new ClientSalesSummary
                    {
                        CustomerCode = customers.FirstOrDefault(c => c.Id == g.Key)?.CustomerCode ?? "N/D",
                        CustomerName = firstOrder.CustomerName,
                        ZoneName = zone ?? "Sin Zona",
                        OrdersCount = g.Count(),
                        TotalSales = g.Sum(o => o.TotalAmount)
                    };
                })
                .OrderByDescending(c => c.TotalSales)
                .ToList();

            SalesByClient.Clear();
            foreach (var c in clientGroups)
            {
                SalesByClient.Add(c);
            }

            // 4. Compute Grouping by Zone (Route)
            var zoneClientCounts = customers
                .GroupBy(c => c.RouteName ?? "Sin Zona")
                .ToDictionary(g => g.Key, g => g.Count());

            var zoneGroups = financialOrders
                .GroupBy(o => {
                    customerRoutes.TryGetValue(o.CustomerId, out var zone);
                    return zone ?? "Sin Zona";
                })
                .Select(g => {
                    zoneClientCounts.TryGetValue(g.Key, out var clientCount);
                    return new ZoneSalesSummary
                    {
                        ZoneName = g.Key,
                        ClientsCount = clientCount,
                        OrdersCount = g.Count(),
                        TotalSales = g.Sum(o => o.TotalAmount)
                    };
                })
                .OrderByDescending(z => z.TotalSales)
                .ToList();

            SalesByZone.Clear();
            foreach (var z in zoneGroups)
            {
                SalesByZone.Add(z);
            }

            // 5. Compute Grouping by Seller / User (Displaying Full Name)
            var sellerGroups = financialOrders
                .GroupBy(o => {
                    var createdBy = o.CreatedBy ?? string.Empty;
                    if (string.IsNullOrWhiteSpace(createdBy)) return "Sistema / Sin Vendedor";
                    if (userMap.TryGetValue(createdBy, out var fullName) && !string.IsNullOrWhiteSpace(fullName))
                    {
                        return fullName;
                    }
                    return createdBy;
                })
                .Select(g => new SellerSalesSummary
                {
                    SellerName = g.Key,
                    OrdersCount = g.Count(),
                    TotalSales = g.Sum(o => o.TotalAmount)
                })
                .OrderByDescending(s => s.TotalSales)
                .ToList();

            SalesBySeller.Clear();
            foreach (var s in sellerGroups)
            {
                SalesBySeller.Add(s);
            }

            // 6. Compute Grouping by Day
            var dayGroups = financialOrders
                .GroupBy(o => o.OrderDate.Date)
                .Select(g => new DailySalesSummary
                {
                    Date = g.Key,
                    OrdersCount = g.Count(),
                    TotalSales = g.Sum(o => o.TotalAmount)
                })
                .OrderByDescending(d => d.Date)
                .ToList();

            SalesByDay.Clear();
            foreach (var d in dayGroups)
            {
                SalesByDay.Add(d);
            }
        }
        catch (Exception)
        {
            // Fail silently or set fallbacks if API is temporarily offline
            TotalSales = 0;
            TotalOrdersCount = 0;
            AverageTicket = 0;
            TopProduct = "Error de Conexión";
            FilteredOrders.Clear();
            SalesByClient.Clear();
            SalesByZone.Clear();
            SalesBySeller.Clear();
            SalesByDay.Clear();
            TopSellingProducts.Clear();
        }
        finally
        {
            IsLoading = false;
        }
    }
}
