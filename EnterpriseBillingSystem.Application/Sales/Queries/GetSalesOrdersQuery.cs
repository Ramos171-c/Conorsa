using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using EnterpriseBillingSystem.Application.Common.Models;
using EnterpriseBillingSystem.Domain.Repositories;

namespace EnterpriseBillingSystem.Application.Sales.Queries;

// ─── DTOs ────────────────────────────────────────────────────────────────────

public record SalesOrderDetailItemDto(
    Guid Id,
    Guid ProductId,
    string ProductName,
    string ProductCode,
    Guid UnitOfMeasureId,
    string UnitOfMeasure,
    decimal Quantity,
    decimal UnitPrice,
    decimal DiscountPercentage,
    decimal DiscountAmount,
    decimal TaxPercentage,
    decimal TaxAmount,
    decimal NetAmount
);

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

// ─── Queries ─────────────────────────────────────────────────────────────────

public record GetSalesOrdersQuery(
    Guid? CustomerId,
    string? Status,
    DateTime? FromDate,
    DateTime? ToDate,
    int PageNumber = 1,
    int PageSize = 20,
    string? CreatedBy = null
) : IRequest<PagedResult<SalesOrderListItemDto>>;

public record GetSalesOrderByIdQuery(Guid SalesOrderId) : IRequest<SalesOrderDetailDto?>;

// ─── Handlers ────────────────────────────────────────────────────────────────

public class GetSalesOrdersQueryHandler : IRequestHandler<GetSalesOrdersQuery, PagedResult<SalesOrderListItemDto>>
{
    private readonly ISalesOrderRepository _repository;

    public GetSalesOrdersQueryHandler(ISalesOrderRepository repository)
    {
        _repository = repository;
    }

    public async Task<PagedResult<SalesOrderListItemDto>> Handle(GetSalesOrdersQuery request, CancellationToken cancellationToken)
    {
        var (items, totalCount) = await _repository.GetPagedAsync(
            request.CustomerId, request.Status, request.FromDate, request.ToDate,
            request.PageNumber, request.PageSize, request.CreatedBy, cancellationToken);

        var dtos = items.Select(so => new SalesOrderListItemDto(
            so.Id,
            so.OrderNumber,
            so.CustomerId,
            so.Customer?.Name ?? string.Empty,
            so.OrderDate,
            so.SubTotal,
            so.DiscountAmount,
            so.TaxAmount,
            so.TotalAmount,
            so.Status.ToString(),
            so.CreatedBy));

        return new PagedResult<SalesOrderListItemDto>(dtos.ToList(), totalCount, request.PageNumber, request.PageSize);
    }
}

public class GetSalesOrderByIdQueryHandler : IRequestHandler<GetSalesOrderByIdQuery, SalesOrderDetailDto?>
{
    private readonly ISalesOrderRepository _repository;

    public GetSalesOrderByIdQueryHandler(ISalesOrderRepository repository)
    {
        _repository = repository;
    }

    public async Task<SalesOrderDetailDto?> Handle(GetSalesOrderByIdQuery request, CancellationToken cancellationToken)
    {
        var order = await _repository.GetByIdWithDetailsAsync(request.SalesOrderId, cancellationToken);
        if (order == null) return null;

        var details = order.Details.Select(d => new SalesOrderDetailItemDto(
            d.Id,
            d.ProductId,
            d.Product?.Name ?? string.Empty,
            d.Product?.InternalCode ?? string.Empty,
            d.UnitOfMeasureId,
            d.UnitOfMeasure?.Code ?? string.Empty,
            d.Quantity,
            d.UnitPrice,
            d.DiscountPercentage,
            d.DiscountAmount,
            d.TaxPercentage,
            d.TaxAmount,
            d.NetAmount
        )).ToList();

        return new SalesOrderDetailDto(
            order.Id,
            order.OrderNumber,
            order.CustomerId,
            order.Customer?.Name ?? string.Empty,
            order.Customer?.CustomerCode ?? string.Empty,
            order.OrderDate,
            order.SubTotal,
            order.DiscountAmount,
            order.TaxAmount,
            order.TotalAmount,
            order.Status.ToString(),
            order.Notes,
            order.CreatedOnUtc,
            details);
    }
}
