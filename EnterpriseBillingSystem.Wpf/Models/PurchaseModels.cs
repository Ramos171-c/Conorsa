using System;
using System.Collections.Generic;

namespace EnterpriseBillingSystem.Wpf.Models;

public record ReceiptDetailRequestDto(
    Guid ProductId,
    Guid ProductPresentationId,
    decimal Quantity,
    decimal UnitPrice
);

public record RegisterPurchaseReceiptCommandDto(
    Guid SupplierId,
    Guid BranchWarehouseId,
    Guid? PurchaseOrderId,
    DateTime ReceiptDate,
    string? ReferenceDocument,
    string? Notes,
    List<ReceiptDetailRequestDto> Details
);

public record PurchaseReceiptListItemDto(
    Guid Id,
    string ReceiptNumber,
    Guid SupplierId,
    string SupplierName,
    Guid BranchWarehouseId,
    string WarehouseName,
    DateTime ReceiptDate,
    string? ReferenceDocument,
    string Status
);

public record PurchaseOrderListItemDto(
    Guid Id,
    string OrderNumber,
    Guid SupplierId,
    string SupplierName,
    DateTime OrderDate,
    DateTime? ExpectedDeliveryDate,
    decimal SubTotal,
    decimal DiscountAmount,
    decimal TaxAmount,
    decimal TotalAmount,
    string Status
);

public record PurchaseOrderDetailItemDto(
    Guid Id,
    Guid ProductId,
    string ProductName,
    string ProductCode,
    Guid UnitOfMeasureId,
    string UnitOfMeasure,
    decimal Quantity,
    decimal ReceivedQuantity,
    decimal PendingQuantity,
    decimal UnitPrice,
    decimal DiscountPercentage,
    decimal DiscountAmount,
    decimal TaxPercentage,
    decimal TaxAmount,
    decimal NetAmount
);

public record PurchaseOrderDetailDto(
    Guid Id,
    string OrderNumber,
    Guid SupplierId,
    string SupplierName,
    string SupplierCode,
    DateTime OrderDate,
    DateTime? ExpectedDeliveryDate,
    decimal SubTotal,
    decimal DiscountAmount,
    decimal TaxAmount,
    decimal TotalAmount,
    string Status,
    string? Notes,
    DateTime CreatedOnUtc,
    List<PurchaseOrderDetailItemDto> Details
);
