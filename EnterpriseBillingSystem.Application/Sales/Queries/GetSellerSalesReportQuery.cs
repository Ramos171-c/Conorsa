using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using EnterpriseBillingSystem.Domain.Repositories;
using EnterpriseBillingSystem.Domain.Enums;

namespace EnterpriseBillingSystem.Application.Sales.Queries;

public record SellerSalesSummaryDto(
    string SellerName,
    int TotalOrdersCount,
    int DeliveredOrdersCount,
    int CancelledOrdersCount,
    decimal TotalPresaleAmount,
    decimal TotalDeliveredAmount,
    decimal TotalReturnedAmount,
    decimal DeliveryEffectivenessPercentage
);

public record SellerSalesReportDto(
    DateTime? FromDate,
    DateTime? ToDate,
    Guid? RouteId,
    int TotalSellersCount,
    int TotalOrdersCount,
    decimal GrandTotalPresaleAmount,
    decimal GrandTotalDeliveredAmount,
    decimal GrandTotalReturnedAmount,
    decimal OverallEffectivenessPercentage,
    IEnumerable<SellerSalesSummaryDto> Sellers
);

public record GetSellerSalesReportQuery(
    DateTime? FromDate,
    DateTime? ToDate,
    Guid? RouteId,
    string? SellerName
) : IRequest<SellerSalesReportDto>;

public class GetSellerSalesReportQueryHandler : IRequestHandler<GetSellerSalesReportQuery, SellerSalesReportDto>
{
    private readonly ISalesOrderRepository _salesOrderRepository;

    public GetSellerSalesReportQueryHandler(ISalesOrderRepository salesOrderRepository)
    {
        _salesOrderRepository = salesOrderRepository;
    }

    public async Task<SellerSalesReportDto> Handle(GetSellerSalesReportQuery request, CancellationToken cancellationToken)
    {
        var orders = (await _salesOrderRepository.GetFilteredWithDetailsAsync(
            customerId: null,
            status: null,
            fromDate: request.FromDate,
            toDate: request.ToDate,
            routeId: request.RouteId,
            cancellationToken: cancellationToken)).ToList();

        if (!string.IsNullOrWhiteSpace(request.SellerName))
        {
            orders = orders.Where(o => (o.CreatedBy ?? "").Equals(request.SellerName, StringComparison.OrdinalIgnoreCase)).ToList();
        }

        var sellerGroups = orders.GroupBy(o => string.IsNullOrWhiteSpace(o.CreatedBy) ? "Sin Vendedor" : o.CreatedBy).ToList();
        var sellerSummaries = new List<SellerSalesSummaryDto>();

        foreach (var group in sellerGroups)
        {
            var sellerName = group.Key;
            var groupOrders = group.ToList();
            var totalOrders = groupOrders.Count;
            var deliveredOrders = groupOrders.Count(o => o.Status == SalesOrderStatus.Completado || o.Status == SalesOrderStatus.EnCamino);
            var cancelledOrders = groupOrders.Count(o => o.Status == SalesOrderStatus.Anulado);

            decimal totalPresale = 0;
            decimal totalDelivered = 0;
            decimal totalReturned = 0;

            foreach (var order in groupOrders)
            {
                decimal currentOrderDelivered = order.Status == SalesOrderStatus.Anulado ? 0 : order.TotalAmount;
                decimal originalPresale = order.Details.Sum(d => d.Quantity * d.UnitPrice - d.DiscountAmount + d.TaxAmount);
                if (originalPresale < currentOrderDelivered) originalPresale = currentOrderDelivered;

                decimal returnedOnOrder = originalPresale - currentOrderDelivered;
                if (returnedOnOrder < 0) returnedOnOrder = 0;

                totalPresale += originalPresale;
                totalDelivered += currentOrderDelivered;
                totalReturned += returnedOnOrder;
            }

            decimal effectiveness = totalPresale > 0 ? Math.Round((totalDelivered / totalPresale) * 100, 2) : 100.00m;

            sellerSummaries.Add(new SellerSalesSummaryDto(
                SellerName: sellerName,
                TotalOrdersCount: totalOrders,
                DeliveredOrdersCount: deliveredOrders,
                CancelledOrdersCount: cancelledOrders,
                TotalPresaleAmount: Math.Round(totalPresale, 2),
                TotalDeliveredAmount: Math.Round(totalDelivered, 2),
                TotalReturnedAmount: Math.Round(totalReturned, 2),
                DeliveryEffectivenessPercentage: effectiveness
            ));
        }

        sellerSummaries = sellerSummaries.OrderByDescending(s => s.TotalDeliveredAmount).ToList();

        decimal grandPresale = sellerSummaries.Sum(s => s.TotalPresaleAmount);
        decimal grandDelivered = sellerSummaries.Sum(s => s.TotalDeliveredAmount);
        decimal grandReturned = sellerSummaries.Sum(s => s.TotalReturnedAmount);
        decimal overallEffectiveness = grandPresale > 0 ? Math.Round((grandDelivered / grandPresale) * 100, 2) : 100.00m;

        return new SellerSalesReportDto(
            FromDate: request.FromDate,
            ToDate: request.ToDate,
            RouteId: request.RouteId,
            TotalSellersCount: sellerSummaries.Count,
            TotalOrdersCount: orders.Count,
            GrandTotalPresaleAmount: grandPresale,
            GrandTotalDeliveredAmount: grandDelivered,
            GrandTotalReturnedAmount: grandReturned,
            OverallEffectivenessPercentage: overallEffectiveness,
            Sellers: sellerSummaries
        );
    }
}
