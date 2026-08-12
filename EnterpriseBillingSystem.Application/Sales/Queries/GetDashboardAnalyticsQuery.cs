using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using EnterpriseBillingSystem.Domain.Entities;
using EnterpriseBillingSystem.Domain.Repositories;
using EnterpriseBillingSystem.Domain.Enums;

namespace EnterpriseBillingSystem.Application.Sales.Queries;

public record KpiMetricDto(
    string Title,
    string Value,
    string Subtitle,
    string Icon,
    string TrendColor
);

public record DailySalesTrendDto(
    string DateLabel,
    decimal PresaleAmount,
    decimal DeliveredAmount
);

public record SellerPerformanceChartDto(
    string SellerName,
    decimal PresaleAmount,
    decimal DeliveredAmount,
    decimal LossAmount
);

public record RouteReturnsChartDto(
    string RouteName,
    decimal ReturnedPieces,
    decimal ReturnedAmount
);

public record TopShortageProductDto(
    string ProductCode,
    string ProductName,
    decimal ShortageQuantity,
    decimal TotalLossAmount
);

public record DashboardAnalyticsDto(
    DateTime FromDate,
    DateTime ToDate,
    Guid? RouteId,
    KpiMetricDto TotalPresaleKpi,
    KpiMetricDto TotalDeliveredKpi,
    KpiMetricDto TotalShortageLossKpi,
    KpiMetricDto GlobalEffectivenessKpi,
    IEnumerable<DailySalesTrendDto> DailyTrend,
    IEnumerable<SellerPerformanceChartDto> SellerPerformance,
    IEnumerable<RouteReturnsChartDto> RouteReturns,
    IEnumerable<TopShortageProductDto> TopShortageProducts
);

public record GetDashboardAnalyticsQuery(
    DateTime? FromDate,
    DateTime? ToDate,
    Guid? RouteId
) : IRequest<DashboardAnalyticsDto>;

public class GetDashboardAnalyticsQueryHandler : IRequestHandler<GetDashboardAnalyticsQuery, DashboardAnalyticsDto>
{
    private readonly ISalesOrderRepository _salesOrderRepository;
    private readonly IRouteLiquidationRepository _liquidationRepository;
    private readonly IRepository<ApplicationUser> _userRepository;

    public GetDashboardAnalyticsQueryHandler(
        ISalesOrderRepository salesOrderRepository,
        IRouteLiquidationRepository liquidationRepository,
        IRepository<ApplicationUser> userRepository)
    {
        _salesOrderRepository = salesOrderRepository;
        _liquidationRepository = liquidationRepository;
        _userRepository = userRepository;
    }

    public async Task<DashboardAnalyticsDto> Handle(GetDashboardAnalyticsQuery request, CancellationToken cancellationToken)
    {
        var fromDate = request.FromDate ?? DateTime.UtcNow.AddDays(-7).Date;
        var toDate = request.ToDate ?? DateTime.UtcNow;

        var orders = await _salesOrderRepository.GetFilteredWithDetailsAsync(
            customerId: null,
            status: null,
            fromDate: fromDate,
            toDate: toDate,
            routeId: request.RouteId,
            cancellationToken: cancellationToken);

        var users = await _userRepository.GetAllAsync();
        var userMap = users.ToDictionary(
            u => u.Id.ToString(),
            u => string.IsNullOrWhiteSpace(u.FirstName) ? (u.UserName ?? "Vendedor") : $"{u.FirstName} {u.LastName}".Trim(),
            StringComparer.OrdinalIgnoreCase);

        var usernameMap = users.ToDictionary(
            u => u.UserName ?? string.Empty,
            u => string.IsNullOrWhiteSpace(u.FirstName) ? (u.UserName ?? "Vendedor") : $"{u.FirstName} {u.LastName}".Trim(),
            StringComparer.OrdinalIgnoreCase);

        var validOrders = orders.Where(o => o.Status != SalesOrderStatus.Anulado).ToList();

        decimal totalPresale = 0;
        decimal totalDelivered = 0;
        decimal totalShortagePieces = 0;
        decimal totalShortageAmount = 0;

        var sellerData = new Dictionary<string, (decimal Presale, decimal Delivered)>();
        var dailyData = new Dictionary<DateTime, (decimal Presale, decimal Delivered)>();

        foreach (var order in validOrders)
        {
            var rawCreator = order.CreatedBy ?? "Vendedor General";
            var sellerName = userMap.TryGetValue(rawCreator, out var name1) ? name1
                : (usernameMap.TryGetValue(rawCreator, out var name2) ? name2 : rawCreator);

            var dateKey = order.OrderDate.Date;

            // Reconstruct original presale amount from details
            decimal presale = 0;
            foreach (var detail in order.Details)
            {
                decimal qty = detail.OriginalPresaleQuantity ?? detail.Quantity;
                decimal gross = qty * detail.UnitPrice;
                decimal disc = gross * (detail.DiscountPercentage / 100m);
                decimal tax = (gross - disc) * (detail.TaxPercentage / 100m);
                presale += gross - disc + tax;

                if (detail.OriginalPresaleQuantity.HasValue && detail.OriginalPresaleQuantity.Value > detail.Quantity)
                {
                    decimal diffQty = detail.OriginalPresaleQuantity.Value - detail.Quantity;
                    totalShortagePieces += diffQty;

                    decimal lineGrossLoss = diffQty * detail.UnitPrice;
                    decimal lineDiscLoss = lineGrossLoss * (detail.DiscountPercentage / 100m);
                    decimal lineTaxLoss = (lineGrossLoss - lineDiscLoss) * (detail.TaxPercentage / 100m);
                    totalShortageAmount += lineGrossLoss - lineDiscLoss + lineTaxLoss;
                }
            }

            // Delivered is TotalAmount ONLY if the order is completed
            decimal delivered = order.Status == SalesOrderStatus.Completado ? order.TotalAmount : 0m;

            totalPresale += presale;
            totalDelivered += delivered;

            if (!sellerData.ContainsKey(sellerName)) sellerData[sellerName] = (0, 0);
            var sCur = sellerData[sellerName];
            sellerData[sellerName] = (sCur.Presale + presale, sCur.Delivered + delivered);

            if (!dailyData.ContainsKey(dateKey)) dailyData[dateKey] = (0, 0);
            var dCur = dailyData[dateKey];
            dailyData[dateKey] = (dCur.Presale + presale, dCur.Delivered + delivered);
        }

        // Obtener liquidaciones de ruta para totalizar devoluciones y faltantes
        var (liquidations, _) = await _liquidationRepository.GetPagedAsync(
            fromDate, toDate, request.RouteId, status: null, pageNumber: 1, pageSize: 1000, cancellationToken);

        decimal totalReturnedPieces = 0;
        decimal totalReturnedAmount = 0;
        var routeReturnsList = new List<RouteReturnsChartDto>();

        foreach (var liq in liquidations)
        {
            totalReturnedPieces += liq.TotalQuantityReturned;
            totalReturnedAmount += liq.TotalAmountReturned;

            var rName = liq.Route?.Name ?? "Ruta Generica";
            routeReturnsList.Add(new RouteReturnsChartDto(
                RouteName: rName,
                ReturnedPieces: liq.TotalQuantityReturned,
                ReturnedAmount: liq.TotalAmountReturned
            ));
        }

        decimal totalLoss = totalReturnedAmount + totalShortageAmount;
        decimal totalLossPieces = totalReturnedPieces + totalShortagePieces;
        decimal completedPresale = totalDelivered + totalLoss;
        decimal effectivenessPct = completedPresale > 0 ? (totalDelivered / completedPresale) * 100m : 100m;

        var kpiPresale = new KpiMetricDto("Preventa Solicitada", $"C$ {totalPresale:N2}", $"{validOrders.Count} Pedidos Tomados", "CurrencyUsd", "#0284C7");
        var kpiDelivered = new KpiMetricDto("Entrega Efectiva", $"C$ {totalDelivered:N2}", "Llegó al cliente", "TruckCheck", "#059669");
        var kpiLoss = new KpiMetricDto("Pérdida por Devoluciones", $"C$ {totalLoss:N2}", $"{totalLossPieces:N0} Piezas Faltantes", "AlertCircleOutline", "#DC2626");
        var kpiEffectiveness = new KpiMetricDto("Efectividad Global", $"{effectivenessPct:N1}%", "Cumplimiento de Entrega", "ChartLine", "#7C3AED");

        var dailyTrendSeries = dailyData.OrderBy(kvp => kvp.Key).Select(kvp => new DailySalesTrendDto(
            DateLabel: kvp.Key.ToString("dd/MM"),
            PresaleAmount: kvp.Value.Presale,
            DeliveredAmount: kvp.Value.Delivered
        )).ToList();

        var sellerPerformanceSeries = sellerData.Select(kvp => new SellerPerformanceChartDto(
            SellerName: kvp.Key,
            PresaleAmount: kvp.Value.Presale,
            DeliveredAmount: kvp.Value.Delivered,
            LossAmount: Math.Max(0, kvp.Value.Presale - kvp.Value.Delivered)
        )).OrderByDescending(x => x.PresaleAmount).ToList();

        // Top faltantes
        var topShortageProducts = new List<TopShortageProductDto>();

        return new DashboardAnalyticsDto(
            FromDate: fromDate,
            ToDate: toDate,
            RouteId: request.RouteId,
            TotalPresaleKpi: kpiPresale,
            TotalDeliveredKpi: kpiDelivered,
            TotalShortageLossKpi: kpiLoss,
            GlobalEffectivenessKpi: kpiEffectiveness,
            DailyTrend: dailyTrendSeries,
            SellerPerformance: sellerPerformanceSeries,
            RouteReturns: routeReturnsList,
            TopShortageProducts: topShortageProducts
        );
    }
}
