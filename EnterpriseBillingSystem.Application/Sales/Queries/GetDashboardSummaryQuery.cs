using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using EnterpriseBillingSystem.Domain.Entities;
using EnterpriseBillingSystem.Domain.Enums;
using EnterpriseBillingSystem.Domain.Repositories;

namespace EnterpriseBillingSystem.Application.Sales.Queries;

public record SalespersonSummaryDto(
    int Rank,
    string Name,
    string Username,
    decimal Sales,
    int TotalOrders,
    int CustomersRegistered,
    string TopProduct,
    decimal GrossProfit,
    double ProfitMargin,
    decimal Goal = 20000m,
    int CustomerGoal = 5,
    double ProgressPercentage = 0,
    double CustomerProgressPercentage = 0,
    decimal AverageTicket = 0,
    string SalesStatusColor = "#1976D2",
    string CustomerStatusColor = "#008080"
);

public record DashboardSummaryDto(
    DateTime FromDate,
    DateTime ToDate,
    decimal SalesToday,
    int OrdersToday,
    decimal ProfitToday,
    double ProfitMarginToday,
    decimal GlobalGoal,
    double GlobalProgressPercentage,
    List<SalespersonSummaryDto> SalespersonGoals
);

public record GetDashboardSummaryQuery(
    DateTime? FromDate = null,
    DateTime? ToDate = null
) : IRequest<DashboardSummaryDto>;

public class GetDashboardSummaryQueryHandler : IRequestHandler<GetDashboardSummaryQuery, DashboardSummaryDto>
{
    private readonly ISalesOrderRepository _salesOrderRepository;
    private readonly IProductRepository _productRepository;
    private readonly IRepository<ApplicationUser> _userRepository;

    public GetDashboardSummaryQueryHandler(
        ISalesOrderRepository salesOrderRepository,
        IProductRepository productRepository,
        IRepository<ApplicationUser> userRepository)
    {
        _salesOrderRepository = salesOrderRepository;
        _productRepository = productRepository;
        _userRepository = userRepository;
    }

    public async Task<DashboardSummaryDto> Handle(GetDashboardSummaryQuery request, CancellationToken cancellationToken)
    {
        var fromDate = request.FromDate?.Date ?? DateTime.Today;
        var toDate = request.ToDate.HasValue 
            ? (request.ToDate.Value.TimeOfDay == TimeSpan.Zero ? request.ToDate.Value.Date.AddDays(1).AddTicks(-1) : request.ToDate.Value)
            : DateTime.Today.AddDays(1).AddTicks(-1);

        // 1. Fetch filtered active orders with details
        var orders = (await _salesOrderRepository.GetFilteredWithDetailsAsync(
            customerId: null,
            status: null,
            fromDate: fromDate,
            toDate: toDate,
            routeId: null,
            cancellationToken: cancellationToken))
            .Where(o => o.Status != SalesOrderStatus.Anulado)
            .ToList();

        // 2. Fetch products and users for mapping
        var products = (await _productRepository.GetAllAsync()).ToDictionary(p => p.Id, p => p);
        var users = (await _userRepository.GetAllAsync()).ToList();

        var userMap = users.ToDictionary(
            u => u.Id.ToString(), 
            u => string.IsNullOrWhiteSpace(u.FirstName) ? (u.UserName ?? "Vendedor") : $"{u.FirstName} {u.LastName}".Trim(), 
            StringComparer.OrdinalIgnoreCase);

        var usernameMap = users.ToDictionary(
            u => u.Id.ToString(),
            u => u.UserName ?? string.Empty,
            StringComparer.OrdinalIgnoreCase);

        // Helper: Calculate line item cost taking presentation conversion factor into account
        decimal CalculateDetailCost(SalesOrderDetail detail)
        {
            if (!products.TryGetValue(detail.ProductId, out var product) || product == null || product.CurrentCost <= 0)
            {
                return detail.NetAmount * 0.75m;
            }

            var presentation = product.Presentations?.FirstOrDefault(p => p.UnitOfMeasureId == detail.UnitOfMeasureId && !p.IsDeleted);
            decimal conversionFactor = presentation?.ConversionFactor ?? 1.0m;
            if (conversionFactor <= 0) conversionFactor = 1.0m;

            decimal totalBaseUnits = detail.Quantity * conversionFactor;
            return totalBaseUnits * product.CurrentCost;
        }

        // 3. Compute overall today/period metrics
        decimal salesToday = orders.Sum(o => o.TotalAmount);
        int ordersToday = orders.Count;

        decimal totalCostToday = orders.SelectMany(o => o.Details).Sum(CalculateDetailCost);
        decimal profitToday = Math.Max(0m, salesToday - totalCostToday);
        double profitMarginToday = salesToday > 0 ? (double)(profitToday / salesToday) * 100 : 0;
        decimal globalGoal = 100000m;
        double globalProgress = globalGoal > 0 ? (double)(salesToday / globalGoal) * 100 : 0;

        // 4. Group seller metrics
        var sellerGroups = orders
            .GroupBy(o => string.IsNullOrWhiteSpace(o.CreatedBy) ? "Vendedor General" : o.CreatedBy)
            .ToList();

        var salespersonList = new List<SalespersonSummaryDto>();

        foreach (var group in sellerGroups)
        {
            var rawCreatedBy = group.Key;
            var displayName = userMap.TryGetValue(rawCreatedBy, out var name) ? name : rawCreatedBy;
            var username = usernameMap.TryGetValue(rawCreatedBy, out var uname) ? uname : rawCreatedBy;

            var sellerOrders = group.ToList();
            decimal sellerSales = sellerOrders.Sum(o => o.TotalAmount);
            int sellerOrdersCount = sellerOrders.Count;
            int sellerCustomersCount = sellerOrders.Select(o => o.CustomerId).Distinct().Count();

            var sellerDetails = sellerOrders.SelectMany(o => o.Details).ToList();
            string topProduct = "Ninguno";

            if (sellerDetails.Count > 0)
            {
                var topProdGroup = sellerDetails
                    .GroupBy(d => d.Product?.Name ?? "Producto")
                    .Select(g => new { Name = g.Key, TotalQty = g.Sum(x => x.Quantity) })
                    .OrderByDescending(x => x.TotalQty)
                    .FirstOrDefault();

                if (topProdGroup != null)
                {
                    topProduct = topProdGroup.Name;
                }
            }

            decimal sellerCost = sellerDetails.Sum(CalculateDetailCost);
            decimal sellerProfit = Math.Max(0m, sellerSales - sellerCost);
            double sellerMargin = sellerSales > 0 ? (double)(sellerProfit / sellerSales) * 100 : 0;

            decimal goal = 20000m;
            int customerGoal = 5;
            double progressPct = goal > 0 ? (double)(sellerSales / goal) * 100 : 0;
            double customerProgressPct = customerGoal > 0 ? (double)sellerCustomersCount / customerGoal * 100 : 0;
            decimal avgTicket = sellerOrdersCount > 0 ? sellerSales / sellerOrdersCount : 0;

            string salesColor = progressPct >= 100 ? "#2E7D32" : (progressPct >= 70 ? "#1976D2" : "#E65100");
            string customerColor = customerProgressPct >= 100 ? "#2E7D32" : (customerProgressPct >= 70 ? "#008080" : "#E65100");

            salespersonList.Add(new SalespersonSummaryDto(
                Rank: 0,
                Name: displayName,
                Username: username,
                Sales: sellerSales,
                TotalOrders: sellerOrdersCount,
                CustomersRegistered: sellerCustomersCount,
                TopProduct: topProduct,
                GrossProfit: sellerProfit,
                ProfitMargin: sellerMargin,
                Goal: goal,
                CustomerGoal: customerGoal,
                ProgressPercentage: progressPct,
                CustomerProgressPercentage: customerProgressPct,
                AverageTicket: avgTicket,
                SalesStatusColor: salesColor,
                CustomerStatusColor: customerColor
            ));
        }

        // Add standard system sellers who didn't have orders today if list is small
        if (salespersonList.Count == 0 && users.Count > 0)
        {
            foreach (var user in users.Take(5))
            {
                salespersonList.Add(new SalespersonSummaryDto(
                    Rank: 0,
                    Name: string.IsNullOrWhiteSpace(user.FirstName) ? (user.UserName ?? "Vendedor") : $"{user.FirstName} {user.LastName}".Trim(),
                    Username: user.UserName ?? string.Empty,
                    Sales: 0m,
                    TotalOrders: 0,
                    CustomersRegistered: 0,
                    TopProduct: "Ninguno",
                    GrossProfit: 0m,
                    ProfitMargin: 0
                ));
            }
        }

        // Rank sellers
        var rankedSellers = salespersonList
            .OrderByDescending(s => s.Sales)
            .Select((s, index) => s with { Rank = index + 1 })
            .ToList();

        return new DashboardSummaryDto(
            FromDate: fromDate,
            ToDate: toDate,
            SalesToday: salesToday,
            OrdersToday: ordersToday,
            ProfitToday: profitToday,
            ProfitMarginToday: profitMarginToday,
            GlobalGoal: globalGoal,
            GlobalProgressPercentage: globalProgress,
            SalespersonGoals: rankedSellers
        );
    }
}
