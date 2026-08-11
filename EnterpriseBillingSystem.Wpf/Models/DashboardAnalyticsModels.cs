using System;
using System.Collections.Generic;

namespace EnterpriseBillingSystem.Wpf.Models;

public class KpiMetricDto
{
    public string Title { get; set; } = string.Empty;
    public string Value { get; set; } = string.Empty;
    public string Subtitle { get; set; } = string.Empty;
    public string Icon { get; set; } = string.Empty;
    public string TrendColor { get; set; } = string.Empty;
}

public class DailySalesTrendDto
{
    public string DateLabel { get; set; } = string.Empty;
    public decimal PresaleAmount { get; set; }
    public decimal DeliveredAmount { get; set; }
}

public class SellerPerformanceChartDto
{
    public string SellerName { get; set; } = string.Empty;
    public decimal PresaleAmount { get; set; }
    public decimal DeliveredAmount { get; set; }
    public decimal LossAmount { get; set; }
}

public class RouteReturnsChartDto
{
    public string RouteName { get; set; } = string.Empty;
    public decimal ReturnedPieces { get; set; }
    public decimal ReturnedAmount { get; set; }
}

public class TopShortageProductDto
{
    public string ProductCode { get; set; } = string.Empty;
    public string ProductName { get; set; } = string.Empty;
    public decimal ShortageQuantity { get; set; }
    public decimal TotalLossAmount { get; set; }
}

public class DashboardAnalyticsDto
{
    public DateTime FromDate { get; set; }
    public DateTime ToDate { get; set; }
    public Guid? RouteId { get; set; }
    public KpiMetricDto TotalPresaleKpi { get; set; } = new();
    public KpiMetricDto TotalDeliveredKpi { get; set; } = new();
    public KpiMetricDto TotalShortageLossKpi { get; set; } = new();
    public KpiMetricDto GlobalEffectivenessKpi { get; set; } = new();
    public List<DailySalesTrendDto> DailyTrend { get; set; } = new();
    public List<SellerPerformanceChartDto> SellerPerformance { get; set; } = new();
    public List<RouteReturnsChartDto> RouteReturns { get; set; } = new();
    public List<TopShortageProductDto> TopShortageProducts { get; set; } = new();
}
