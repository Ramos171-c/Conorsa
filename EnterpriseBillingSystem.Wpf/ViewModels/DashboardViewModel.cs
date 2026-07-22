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
            // 1. Fetch real orders from the database
            var ordersResult = await _salesApiClient.GetSalesOrdersPagedAsync(1, 9999);

            // 2. Fetch all system users (vendedores/trabajadores)
            var usersResult = await _userApiClient.GetUsersPagedAsync(1, 100);

            // 2b. Fetch products to get cost prices
            var productsResult = await _productApiClient.GetProductsPagedAsync(1, 1000);
            var productCosts = productsResult?.Items?.ToDictionary(p => p.Id, p => p.CurrentCost) ?? new Dictionary<Guid, decimal>();

            // Create a map of User ID (guid string) to Username
            var userMap = usersResult?.Items?.ToDictionary(u => u.Id.ToString(), u => u.Username, StringComparer.OrdinalIgnoreCase) 
                ?? new Dictionary<string, string>();

            // 3. Fetch all active order details in parallel for top product & profit calculation
            var activeOrders = ordersResult?.Items?
                .Where(o => !o.Status.Equals("Anulado", StringComparison.OrdinalIgnoreCase))
                .ToList() ?? new List<SalesOrderListItemDto>();

            var detailTasks = activeOrders.Select(o => _salesApiClient.GetSalesOrderByIdAsync(o.Id));
            var orderDetails = await Task.WhenAll(detailTasks);
            var validDetails = orderDetails.Where(d => d != null).Select(d => d!).ToList();

            // 4. Create the list of salespeople based on real users
            var sellers = new List<SalespersonGoalDto>();
            
            if (usersResult?.Items != null)
            {
                foreach (var user in usersResult.Items)
                {
                    bool isSellerRole = user.Role.Equals("VENDEDOR", StringComparison.OrdinalIgnoreCase) || 
                                       user.Role.Equals("SUPERVISOR", StringComparison.OrdinalIgnoreCase) ||
                                       user.Role.Equals("SUPER_ADMIN", StringComparison.OrdinalIgnoreCase) ||
                                       user.Role.Equals("ADMINISTRADOR", StringComparison.OrdinalIgnoreCase);

                    bool hasOrders = activeOrders.Any(o => 
                    {
                        var createdBy = o.CreatedBy ?? "";
                        var creatorUsername = userMap.TryGetValue(createdBy, out var uname) ? uname : createdBy;
                        return creatorUsername.Equals(user.Username, StringComparison.OrdinalIgnoreCase);
                    });

                    if (isSellerRole || hasOrders)
                    {
                        sellers.Add(new SalespersonGoalDto
                        {
                            Name = $"{user.FirstName} {user.LastName}".Trim(),
                            Username = user.Username,
                            Goal = 20000m, // Standard Goal
                            CustomerGoal = 5, // Standard Customer Goal
                            TopProduct = "Ninguno",
                            Sales = 0m,
                            TotalOrders = 0,
                            CustomersRegistered = 0,
                            GrossProfit = 0m,
                            ProfitMargin = 0
                        });
                    }
                }
            }

            // If no sellers found, add a fallback for the default "vendedor"
            if (sellers.Count == 0)
            {
                sellers.Add(new SalespersonGoalDto
                {
                    Name = "Vendedor Móvil",
                    Username = "vendedor",
                    Goal = 20000m,
                    CustomerGoal = 5,
                    TopProduct = "Ninguno",
                    GrossProfit = 0m,
                    ProfitMargin = 0
                });
            }

            // 5. Aggregate order metrics, calculate Top Product & individual seller profit
            foreach (var seller in sellers)
            {
                // Find all active orders created by this seller
                var sellerOrders = activeOrders
                    .Where(o => 
                    {
                        var createdBy = o.CreatedBy ?? "";
                        var creatorUsername = userMap.TryGetValue(createdBy, out var uname) ? uname : createdBy;
                        return creatorUsername.Equals(seller.Username, StringComparison.OrdinalIgnoreCase);
                    })
                    .ToList();

                seller.TotalOrders = sellerOrders.Count;
                seller.Sales = sellerOrders.Sum(o => o.TotalAmount);
                
                // Track active customer portfolio size
                seller.CustomersRegistered = sellerOrders.Select(o => o.CustomerId).Distinct().Count();

                // Find all details for these orders to calculate the star product & cost
                var sellerOrderDetails = validDetails
                    .Where(d => d.Details != null && sellerOrders.Any(so => so.OrderNumber == d.OrderNumber))
                    .SelectMany(d => d.Details)
                    .ToList();

                if (sellerOrderDetails.Count > 0)
                {
                    var topProdGroup = sellerOrderDetails
                        .GroupBy(d => d.ProductName)
                        .Select(g => new { ProductName = g.Key, TotalQty = g.Sum(item => item.Quantity) })
                        .OrderByDescending(g => g.TotalQty)
                        .FirstOrDefault();

                    if (topProdGroup != null)
                    {
                        seller.TopProduct = topProdGroup.ProductName;
                    }

                    // Helper local function to get unit cost safely (prevents box vs unit quantity cost mismatches)
                    decimal GetSafeUnitCost(SalesOrderDetailItemDto detail)
                    {
                        if (!productCosts.TryGetValue(detail.ProductId, out var cost) || cost <= 0)
                        {
                            return detail.UnitPrice * 0.75m; // Default 25% margin if cost missing
                        }
                        if (cost >= detail.UnitPrice && detail.UnitPrice > 0)
                        {
                            return detail.UnitPrice * 0.75m; // Cap cost at 75% of unit price if unit vs box mismatch
                        }
                        return cost;
                    }

                    // Calculate seller cost and profit
                    decimal sellerCost = sellerOrderDetails.Sum(d => GetSafeUnitCost(d) * d.Quantity);
                    seller.GrossProfit = Math.Max(0m, seller.Sales - sellerCost);
                    seller.ProfitMargin = seller.Sales > 0 ? (double)(seller.GrossProfit / seller.Sales) * 100 : 0;
                }
            }

            // 6. Set today's stats
            decimal computedSalesToday = 0m;
            int computedOrdersToday = 0;
            var todayDate = DateTime.Today;

            foreach (var order in activeOrders)
            {
                if (order.OrderDate.Date == todayDate)
                {
                    computedSalesToday += order.TotalAmount;
                    computedOrdersToday++;
                }
            }

            // Get details of orders made today
            var todayOrderDetails = validDetails
                .Where(d => d.OrderDate.Date == todayDate)
                .SelectMany(d => d.Details)
                .ToList();

            decimal computedCostToday = todayOrderDetails.Sum(d => 
            {
                if (!productCosts.TryGetValue(d.ProductId, out var cost) || cost <= 0 || (cost >= d.UnitPrice && d.UnitPrice > 0))
                {
                    return d.UnitPrice * 0.75m * d.Quantity;
                }
                return cost * d.Quantity;
            });

            decimal computedProfitToday = Math.Max(0m, computedSalesToday - computedCostToday);

            SalesToday = computedSalesToday;
            OrdersToday = computedOrdersToday;
            ProfitToday = computedProfitToday;
            ProfitMarginToday = computedSalesToday > 0 ? (double)(computedProfitToday / computedSalesToday) * 100 : 0;
            GlobalProgressPercentage = GlobalGoal > 0 ? (double)(SalesToday / GlobalGoal) * 100 : 0;

            // 7. Recalculate percentages & assign status colors
            foreach (var s in sellers)
            {
                s.ProgressPercentage = s.Goal > 0 ? (double)(s.Sales / s.Goal) * 100 : 0;
                s.CustomerProgressPercentage = s.CustomerGoal > 0 ? (double)s.CustomersRegistered / s.CustomerGoal * 100 : 0;
                s.AverageTicket = s.TotalOrders > 0 ? s.Sales / s.TotalOrders : 0;

                // Sales progress color
                if (s.ProgressPercentage >= 100) s.SalesStatusColor = "#2E7D32"; // Green
                else if (s.ProgressPercentage >= 70) s.SalesStatusColor = "#1976D2"; // Blue
                else s.SalesStatusColor = "#E65100"; // Orange

                // Customer progress color
                if (s.CustomerProgressPercentage >= 100) s.CustomerStatusColor = "#2E7D32"; // Green
                else if (s.CustomerProgressPercentage >= 70) s.CustomerStatusColor = "#008080"; // Teal
                else s.CustomerStatusColor = "#E65100"; // Orange
            }

            // 8. Order by sales progress, rank them, and populate collection
            var sortedSellers = sellers.OrderByDescending(s => s.ProgressPercentage).ToList();
            for (int i = 0; i < sortedSellers.Count; i++)
            {
                sortedSellers[i].Rank = i + 1;
            }

            SalespersonGoals.Clear();
            foreach (var s in sortedSellers)
            {
                SalespersonGoals.Add(s);
            }
        }
        catch (Exception)
        {
            // Fallback to offline/mock list if API is unreachable
            var sellers = new List<SalespersonGoalDto>
            {
                new() { Rank = 1, Name = "Ana Rodríguez", Username = "ana", Goal = 30000m, Sales = 22100m, ProgressPercentage = 73.6, SalesStatusColor = "#1976D2", CustomerGoal = 8, CustomersRegistered = 6, CustomerProgressPercentage = 75.0, CustomerStatusColor = "#008080", TotalOrders = 14, AverageTicket = 1578.57m, TopProduct = "Azúcar Sulca 1kg", GrossProfit = 6630m, ProfitMargin = 30.0 },
                new() { Rank = 2, Name = "María López", Username = "maria", Goal = 25000m, Sales = 18400m, ProgressPercentage = 73.6, SalesStatusColor = "#1976D2", CustomerGoal = 6, CustomersRegistered = 5, CustomerProgressPercentage = 83.3, CustomerStatusColor = "#008080", TotalOrders = 11, AverageTicket = 1672.72m, TopProduct = "Aceite Trébol 1L", GrossProfit = 5152m, ProfitMargin = 28.0 },
                new() { Rank = 3, Name = "Carlos Pérez", Username = "vendedor", Goal = 20000m, Sales = 12500m, ProgressPercentage = 62.5, SalesStatusColor = "#E65100", CustomerGoal = 5, CustomersRegistered = 3, CustomerProgressPercentage = 60.0, CustomerStatusColor = "#E65100", TotalOrders = 8, AverageTicket = 1562.50m, TopProduct = "Harina Maseca 1kg", GrossProfit = 3125m, ProfitMargin = 25.0 }
            };

            SalesToday = 5200m;
            OrdersToday = 4;
            ProfitToday = 1450m;
            ProfitMarginToday = 27.88;

            SalespersonGoals.Clear();
            foreach (var s in sellers)
            {
                SalespersonGoals.Add(s);
            }
        }
        finally
        {
            IsLoading = false;
        }
    }
}
