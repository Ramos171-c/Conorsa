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

public class SalesOrderDetailItemDto : CommunityToolkit.Mvvm.ComponentModel.ObservableObject
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

    private decimal? _deliveredQuantity;
    public decimal DeliveredQuantity
    {
        get => _deliveredQuantity ?? Math.Max(0, Quantity - MissingQuantity - ReturnedQuantity);
        set
        {
            if (SetProperty(ref _deliveredQuantity, value))
            {
                _missingQuantity = Math.Max(0, Quantity - value - ReturnedQuantity);
                OnPropertyChanged(nameof(MissingQuantity));
                OnPropertyChanged(nameof(EffectiveNetAmount));
            }
        }
    }

    private decimal _missingQuantity;
    public decimal MissingQuantity
    {
        get => _missingQuantity;
        set
        {
            if (SetProperty(ref _missingQuantity, value))
            {
                _deliveredQuantity = Math.Max(0, Quantity - value - ReturnedQuantity);
                OnPropertyChanged(nameof(DeliveredQuantity));
                OnPropertyChanged(nameof(EffectiveNetAmount));
            }
        }
    }

    private string _missingReason = string.Empty;
    public string MissingReason
    {
        get => _missingReason;
        set => SetProperty(ref _missingReason, value);
    }

    public decimal EffectiveNetAmount
    {
        get
        {
            decimal qtyToBill = Quantity - MissingQuantity - ReturnedQuantity;
            if (qtyToBill < 0) qtyToBill = 0;
            decimal baseAmount = qtyToBill * UnitPrice;
            decimal disc = baseAmount * (DiscountPercentage / 100m);
            decimal tax = (baseAmount - disc) * (TaxPercentage / 100m);
            return baseAmount - disc + tax;
        }
    }
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
    List<SalesOrderDetailItemDto> Details,
    string? CreatedBy
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
    string Observation = ""
)
{
    public string FullUnitOfMeasure => !string.IsNullOrWhiteSpace(UnitOfMeasure) ? UnitOfMeasure : "UND";

    public decimal DisplayTotalSales => GrossSalesAmount > 0 ? GrossSalesAmount : TotalQuantity * UnitPrice;
    public decimal DisplayGrossPurchase => GrossPurchaseCost > 0 ? GrossPurchaseCost : TotalQuantity * UnitCost;
    public decimal DisplayProfit => DisplayTotalSales - DisplayGrossPurchase;
}
