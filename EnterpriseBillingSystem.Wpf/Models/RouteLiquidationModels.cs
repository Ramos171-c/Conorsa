using System;
using System.Collections.Generic;

namespace EnterpriseBillingSystem.Wpf.Models;

public record RouteLiquidationListItemDto(
    Guid Id,
    string LiquidationNumber,
    Guid RouteId,
    string RouteName,
    DateTime LiquidationDate,
    string Status,
    decimal TotalQuantitySent,
    decimal TotalQuantityReturned,
    decimal TotalQuantitySold,
    decimal TotalAmountSold,
    decimal TotalAmountReturned,
    decimal TotalCostSold,
    decimal EstimatedProfit,
    string? Observations,
    string CreatedBy
);

public record RouteLiquidationDetailDto(
    Guid Id,
    Guid ProductId,
    string ProductCode,
    string ProductName,
    Guid UnitOfMeasureId,
    string UnitOfMeasureCode,
    Guid? ProductPresentationId,
    string? PresentationName,
    decimal QuantitySent,
    decimal QuantityReturned,
    decimal QuantitySold,
    decimal BaseQuantitySent,
    decimal BaseQuantityReturned,
    decimal BaseQuantitySold,
    decimal SalePrice,
    decimal Cost,
    decimal SubtotalSold,
    decimal SubtotalReturned,
    string? Notes
);

public record RouteLiquidationFullDto(
    Guid Id,
    string LiquidationNumber,
    Guid RouteId,
    string RouteName,
    DateTime LiquidationDate,
    string Status,
    decimal TotalQuantitySent,
    decimal TotalQuantityReturned,
    decimal TotalQuantitySold,
    decimal TotalAmountSold,
    decimal TotalAmountReturned,
    decimal TotalCostSold,
    decimal EstimatedProfit,
    string? Observations,
    string CreatedBy,
    IEnumerable<RouteLiquidationDetailDto> Details
);
