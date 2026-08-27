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
            // 1. Fetch ONLY TODAY's orders from the API (server-side filter)
            var todayStart = DateTime.Today;
            var todayEnd = todayStart.AddDays(1).AddSeconds(-1);
            var ordersResult = await _salesApiClient.GetSalesOrdersPagedAsync(1, 9999, fromDate: todayStart, toDate: todayEnd);

            // 2. Fetch all system users (vendedores/trabajadores)
            var usersResult = await _userApiClient.GetUsersPagedAsync(1, 100);

            // 2b. Fetch products to get real cost prices and presentation costs
            var productsResult = await _productApiClient.GetProductsPagedAsync(1, 1000);
            var presentationCostMap = new Dictionary<(Guid ProductId, Guid UnitOfMeasureId), decimal>();
            var productBaseCostMap = new Dictionary<Guid, decimal>();
            var productConversionMap = new Dictionary<(Guid ProductId, Guid UnitOfMeasureId), decimal>();

            if (productsResult?.Items != null)
            {
                foreach (var prod in productsResult.Items)
                {
                    productBaseCostMap[prod.Id] = prod.CurrentCost;
                    if (prod.Presentations != null)
                    {
                        foreach (var pres in prod.Presentations)
                        {
                            if (pres.Cost > 0)
                            {
                                presentationCostMap[(prod.Id, pres.UnitOfMeasureId)] = pres.Cost;
                            }
                            if (pres.ConversionFactor > 0)
                            {
                                productConversionMap[(prod.Id, pres.UnitOfMeasureId)] = pres.ConversionFactor;
                            }
                        }
                    }
                }
            }

            decimal CalculateDetailCost(SalesOrderDetailItemDto d)
            {
                // 1. Costo exacto asignado a la presentación en la base de datos
                if (presentationCostMap.TryGetValue((d.ProductId, d.UnitOfMeasureId), out var presCost) && presCost > 0)
                {
                    return presCost * d.Quantity;
                }

                // 2. Costo base del producto por su factor de conversión exacto
                if (productBaseCostMap.TryGetValue(d.ProductId, out var baseCost) && baseCost > 0)
                {
                    productConversionMap.TryGetValue((d.ProductId, d.UnitOfMeasureId), out var factor);
                    if (factor <= 0) factor = 1m;
                    return (baseCost * factor) * d.Quantity;
                }

                // 3. Fallback diferenciado por canal/presentación de venta: Mayorista, Semimayorista o Detalle
                productConversionMap.TryGetValue((d.ProductId, d.UnitOfMeasureId), out var convFactor);
                string uom = (d.UnitOfMeasure ?? string.Empty).ToUpperInvariant();

                if (convFactor >= 12 || uom.Contains("CAJA") || uom.Contains("SACO") || uom.Contains("BULTO") || uom.Contains("MAYORISTA"))
                {
                    // Venta Mayorista: Costo 91% (Margen bruto ~9%)
                    return d.UnitPrice * 0.91m * d.Quantity;
                }
                else if (convFactor > 1 || uom.Contains("PAQ") || uom.Contains("RISTRA") || uom.Contains("DOCENA") || uom.Contains("SEMI"))
                {
                    // Venta Semimayorista: Costo 85% (Margen bruto ~15%)
                    return d.UnitPrice * 0.85m * d.Quantity;
                }
                else
                {
                    // Venta Detalle / Unidad: Costo 78% (Margen bruto ~22%)
                    return d.UnitPrice * 0.78m * d.Quantity;
                }
            }

            // Create a map of User ID (guid string) to Username
            var userMap = usersResult?.Items?.ToDictionary(u => u.Id.ToString(), u => u.Username, StringComparer.OrdinalIgnoreCase) 
                ?? new Dictionary<string, string>();

            // 3. All non-cancelled orders of TODAY
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

                    bool hasOrdersToday = activeOrders.Any(o => 
                    {
                        var createdBy = o.CreatedBy ?? "";
                        var creatorUsername = userMap.TryGetValue(createdBy, out var uname) ? uname : createdBy;
                        return creatorUsername.Equals(user.Username, StringComparison.OrdinalIgnoreCase);
                    });

                    if (isSellerRole || hasOrdersToday)
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

            // Also group any seller present in today's orders createdBy that was not in users list
            var existingUsernames = new HashSet<string>(sellers.Select(s => s.Username), StringComparer.OrdinalIgnoreCase);
            var extraSellersFromOrders = activeOrders
                .Select(o => o.CreatedBy)
                .Where(cb => !string.IsNullOrWhiteSpace(cb) && !existingUsernames.Contains(cb))
                .Distinct(StringComparer.OrdinalIgnoreCase);

            foreach (var sellerUsername in extraSellersFromOrders)
            {
                sellers.Add(new SalespersonGoalDto
                {
                    Name = sellerUsername!,
                    Username = sellerUsername!,
                    Goal = 20000m,
                    CustomerGoal = 5,
                    TopProduct = "Ninguno",
                    Sales = 0m,
                    TotalOrders = 0,
                    CustomersRegistered = 0,
                    GrossProfit = 0m,
                    ProfitMargin = 0
                });
            }

            // 5. Aggregate TODAY's order metrics per seller
            foreach (var seller in sellers)
            {
                // Find all today's active orders created by this seller
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
                
                // Track unique customers visited today
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

                    // Calculate real seller cost and profit
                    decimal sellerCost = sellerOrderDetails.Sum(d => CalculateDetailCost(d));
                    seller.GrossProfit = seller.Sales - sellerCost;
                    seller.ProfitMargin = seller.Sales > 0 ? (double)(seller.GrossProfit / seller.Sales) * 100 : 0;
                }
            }

            // 6. Set KPI stats strictly for today
            decimal computedSales = activeOrders.Sum(o => o.TotalAmount);
            int computedOrders = activeOrders.Count;

            var targetOrderNumbers = new HashSet<string>(activeOrders.Select(o => o.OrderNumber));
            var targetDetails = validDetails.Where(d => targetOrderNumbers.Contains(d.OrderNumber)).ToList();

            decimal computedCost = targetDetails.SelectMany(d => d.Details).Sum(d => CalculateDetailCost(d));
            decimal computedProfit = Math.Max(0m, computedSales - computedCost);

            SalesToday = computedSales;
            OrdersToday = computedOrders;
            ProfitToday = computedProfit;
            ProfitMarginToday = computedSales > 0 ? (double)(computedProfit / computedSales) * 100 : 0;
            GlobalProgressPercentage = GlobalGoal > 0 ? (double)(SalesToday / GlobalGoal) * 100 : 0;

            // 7. Recalculate percentages & assign status colors
            foreach (var s in sellers.Where(x => x.TotalOrders > 0 || x.Sales > 0))
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

            // 8. Order active sellers by sales progress, rank them, and populate collection
            var activeSellersOnly = sellers.Where(s => s.TotalOrders > 0 || s.Sales > 0).OrderByDescending(s => s.ProgressPercentage).ToList();
            if (activeSellersOnly.Count == 0)
            {
                activeSellersOnly = sellers.Take(5).ToList();
            }

            for (int i = 0; i < activeSellersOnly.Count; i++)
            {
                activeSellersOnly[i].Rank = i + 1;
            }

            SalespersonGoals.Clear();
            foreach (var s in activeSellersOnly)
            {
                SalespersonGoals.Add(s);
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error loading dashboard data: {ex.Message}");
        }
        finally
        {
            IsLoading = false;
        }
    }
}
