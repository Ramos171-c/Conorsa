using System;
using System.Collections.Generic;

namespace EnterpriseBillingSystem.Wpf.Models;

public record SalesOrderListItemDto(
    Guid Id,
    string OrderNumber,
    Guid CustomerId,
    string CustomerName,
    DateTime OrderDate,
    decimal SubTotal,
    decimal DiscountAmount,
    decimal TaxAmount,
    decimal TotalAmount,
    string Status,
    string? CreatedBy
);

public class SalesOrderDetailItemDto
{
    public Guid Id { get; set; }
    public Guid ProductId { get; set; }
    public string ProductName { get; set; } = string.Empty;
    public string ProductCode { get; set; } = string.Empty;
    public Guid UnitOfMeasureId { get; set; }
    public string UnitOfMeasure { get; set; } = string.Empty;
    public decimal Quantity { get; set; }
    public decimal UnitPrice { get; set; }
    public decimal DiscountPercentage { get; set; }
    public decimal DiscountAmount { get; set; }
    public decimal TaxPercentage { get; set; }
    public decimal TaxAmount { get; set; }
    public decimal NetAmount { get; set; }
    public decimal ReturnedQuantity { get; set; } = 0;
}

public record ReturnSalesOrderDetailItemDto(
    Guid SalesOrderDetailId,
    decimal Quantity
);

public record ReturnSalesOrderCommandDto(
    Guid SalesOrderId,
    List<ReturnSalesOrderDetailItemDto>? Items
);

public record SalesOrderDetailDto(
    Guid Id,
    string OrderNumber,
    Guid CustomerId,
    string CustomerName,
    string CustomerCode,
    DateTime OrderDate,
    decimal SubTotal,
    decimal DiscountAmount,
    decimal TaxAmount,
    decimal TotalAmount,
    string Status,
    string? Notes,
    DateTime CreatedOnUtc,
    List<SalesOrderDetailItemDto> Details
);

public record CancelSalesOrderCommandDto(
    Guid SalesOrderId,
    string? CancellationReason
);

public record ConsolidatedProductDto(
    Guid ProductId,
    string ProductCode,
    string ProductName,
    string UnitOfMeasure,
    decimal TotalQuantity,
    decimal AvailableStock,
    decimal DeductedFromInventory,
    decimal NetQuantityToOrder,
    decimal UnitCost,
    decimal UnitPrice,
    decimal GrossPurchaseCost,
    decimal GrossSalesAmount,
    decimal InventoryDeductedPurchaseCost,
    decimal InventoryDeductedSalesAmount,
    decimal TotalPurchaseCost,
    decimal NetSalesAmount,
    decimal ProfitMarginAmount,
    decimal ProfitMarginPercentage,
    decimal TotalNetAmount,
    decimal TotalCost,
    string Observation,
    string SupplierName,
    string PurchaseUnitName,
    decimal UnitsPerCase,
    int SuggestedBoxesToOrder,
    decimal SuggestedTotalUnitsToOrder,
    decimal BoxCost,
    decimal SuggestedPurchaseCost,
    string SellerObservations
);
