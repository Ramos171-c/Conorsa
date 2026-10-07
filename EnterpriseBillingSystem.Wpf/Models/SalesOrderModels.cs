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
    public string? ProductDescription { get; set; }
    public string DisplayText => !string.IsNullOrWhiteSpace(ProductDescription) ? ProductDescription : ProductName;
    public string ProductCode { get; set; } = string.Empty;
    public Guid UnitOfMeasureId { get; set; }
    public string UnitOfMeasure { get; set; } = string.Empty;
    private decimal _quantity;
    public decimal Quantity
    {
        get => _quantity;
        set
        {
            if (SetProperty(ref _quantity, value))
            {
                OnPropertyChanged(nameof(DeliveredQuantity));
                OnPropertyChanged(nameof(EffectiveNetAmount));
            }
        }
    }
    
    private decimal _unitPrice;
    public decimal UnitPrice
    {
        get => _unitPrice;
        set
        {
            if (SetProperty(ref _unitPrice, value))
            {
                OnPropertyChanged(nameof(EffectiveNetAmount));
            }
        }
    }

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
    string Observation = "",
    string SupplierName = "Distribuidora Jenny",
    string PurchaseUnitName = "Caja",
    decimal UnitsPerCase = 1.00m,
    int SuggestedBoxesToOrder = 0,
    decimal SuggestedTotalUnitsToOrder = 0m,
    decimal BoxCost = 0m,
    decimal SuggestedPurchaseCost = 0m,
    string SellerObservations = ""
)
{
    public string FullUnitOfMeasure => !string.IsNullOrWhiteSpace(UnitOfMeasure) ? UnitOfMeasure : "UND";

    public decimal DisplayTotalSales => GrossSalesAmount > 0 ? GrossSalesAmount : TotalQuantity * UnitPrice;
    public decimal DisplayGrossPurchase => GrossPurchaseCost > 0 ? GrossPurchaseCost : TotalQuantity * UnitCost;
    public decimal DisplayProfit => DisplayTotalSales - DisplayGrossPurchase;
}

public record SalesOrderDetailRequestDto(
    Guid ProductId,
    Guid UnitOfMeasureId,
    decimal Quantity,
    decimal UnitPrice,
    decimal DiscountPercentage,
    decimal TaxPercentage
);

public record UpdateSalesOrderCommandDto(
    Guid Id,
    Guid CustomerId,
    DateTime OrderDate,
    string? Notes,
    List<SalesOrderDetailRequestDto> Details,
    int? Status = null
);

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

public record PresaleShortageItemDto(
    string ProductCode,
    string ProductName,
    string UnitOfMeasureCode,
    decimal RequestedQuantity,
    decimal DeliveredQuantity,
    decimal ShortageQuantity,
    decimal UnitPrice,
    decimal TotalLossAmount
);

public record PresaleShortagesReportDto(
    DateTime? FromDate,
    DateTime? ToDate,
    Guid? RouteId,
    int TotalUniqueProductsWithShortage,
    decimal TotalMissingPiecesCount,
    decimal TotalPresaleLossAmount,
    IEnumerable<PresaleShortageItemDto> Items
);

public class WeeklySalesSummaryDto
{
    public string WeekLabel { get; set; } = string.Empty;
    public DateTime WeekStartDate { get; set; }
    public DateTime WeekEndDate { get; set; }
    public int OrdersCount { get; set; }
    public decimal TotalBilled { get; set; }
    public decimal TotalDelivered { get; set; }
    public decimal TotalShortage { get; set; }
    public decimal EstimatedCost { get; set; }
    public decimal GrossProfit { get; set; }
    public double ProfitMargin { get; set; }
    public double DeliveryEffectiveness { get; set; }
}

public class PriceTypeSalesSummaryDto
{
    public string PriceTypeName { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string BadgeColor { get; set; } = "#1976D2";
    public int OrdersCount { get; set; }
    public decimal TotalSales { get; set; }
    public decimal EstimatedCost { get; set; }
    public decimal GrossProfit { get; set; }
    public double ProfitMargin { get; set; }
    public double SharePercentage { get; set; }
}

public class SalesOrderReportItemDto
{
    public Guid Id { get; set; }
    public string OrderNumber { get; set; } = string.Empty;
    public Guid CustomerId { get; set; }
    public string CustomerName { get; set; } = string.Empty;
    public string CustomerCode { get; set; } = string.Empty;
    public string ZoneName { get; set; } = "Sin Zona";
    public DateTime OrderDate { get; set; }
    public decimal TotalAmount { get; set; }
    public decimal DeliveredAmount { get; set; }
    public decimal ShortageAmount { get; set; }
    public decimal EstimatedCost { get; set; }
    public decimal EstimatedProfit { get; set; }
    public string Status { get; set; } = string.Empty;
    public string CreatedBy { get; set; } = string.Empty;
    public string SellerDisplayName { get; set; } = string.Empty;
    public string PricingType { get; set; } = "Detalle";
    public string PricingBadgeColor { get; set; } = "#1976D2";
}
