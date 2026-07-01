using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using EnterpriseBillingSystem.Domain.Repositories;

namespace EnterpriseBillingSystem.Application.Purchases.Queries;

// ─── DTOs ────────────────────────────────────────────────────────────────────

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

// ─── Queries ─────────────────────────────────────────────────────────────────

public record GetPurchaseOrdersQuery(
    Guid? SupplierId,
    string? Status,
    DateTime? FromDate,
    DateTime? ToDate,
    int PageNumber = 1,
    int PageSize = 20
) : IRequest<PagedResult<PurchaseOrderListItemDto>>;

public record GetPurchaseOrderByIdQuery(Guid PurchaseOrderId) : IRequest<PurchaseOrderDetailDto?>;

// ─── Handlers ────────────────────────────────────────────────────────────────

public class GetPurchaseOrdersQueryHandler : IRequestHandler<GetPurchaseOrdersQuery, PagedResult<PurchaseOrderListItemDto>>
{
    private readonly IPurchaseOrderRepository _repository;

    public GetPurchaseOrdersQueryHandler(IPurchaseOrderRepository repository)
    {
        _repository = repository;
    }

    public async Task<PagedResult<PurchaseOrderListItemDto>> Handle(GetPurchaseOrdersQuery request, CancellationToken cancellationToken)
    {
        var (items, totalCount) = await _repository.GetPagedAsync(
            request.SupplierId, request.Status, request.FromDate, request.ToDate,
            request.PageNumber, request.PageSize, cancellationToken);

        var dtos = items.Select(po => new PurchaseOrderListItemDto(
            po.Id, po.OrderNumber, po.SupplierId,
            po.Supplier?.Name ?? string.Empty,
            po.OrderDate, po.ExpectedDeliveryDate,
            po.SubTotal, po.DiscountAmount, po.TaxAmount, po.TotalAmount,
            po.Status.ToString()));

        return new PagedResult<PurchaseOrderListItemDto>(dtos, totalCount, request.PageNumber, request.PageSize);
    }
}

public class GetPurchaseOrderByIdQueryHandler : IRequestHandler<GetPurchaseOrderByIdQuery, PurchaseOrderDetailDto?>
{
    private readonly IPurchaseOrderRepository _repository;

    public GetPurchaseOrderByIdQueryHandler(IPurchaseOrderRepository repository)
    {
        _repository = repository;
    }

    public async Task<PurchaseOrderDetailDto?> Handle(GetPurchaseOrderByIdQuery request, CancellationToken cancellationToken)
    {
        var order = await _repository.GetByIdWithDetailsAsync(request.PurchaseOrderId, cancellationToken);
        if (order == null) return null;

        var details = order.Details.Select(d => new PurchaseOrderDetailItemDto(
            d.Id, d.ProductId,
            d.Product?.Name ?? string.Empty,
            d.Product?.InternalCode ?? string.Empty,
            d.UnitOfMeasureId,
            d.UnitOfMeasure?.Code ?? string.Empty,
            d.Quantity, d.ReceivedQuantity,
            d.Quantity - d.ReceivedQuantity,
            d.UnitPrice, d.DiscountPercentage, d.DiscountAmount,
            d.TaxPercentage, d.TaxAmount, d.NetAmount
        )).ToList();

        return new PurchaseOrderDetailDto(
            order.Id, order.OrderNumber,
            order.SupplierId, order.Supplier?.Name ?? string.Empty,
            order.Supplier?.SupplierCode ?? string.Empty,
            order.OrderDate, order.ExpectedDeliveryDate,
            order.SubTotal, order.DiscountAmount, order.TaxAmount, order.TotalAmount,
            order.Status.ToString(), order.Notes, order.CreatedOnUtc, details);
    }
}
