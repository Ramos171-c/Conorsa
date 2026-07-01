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

public record SalesInvoiceDetailItemDto(
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

public record SalesInvoiceListItemDto(
    Guid Id,
    string InvoiceNumber,
    Guid CustomerId,
    string CustomerName,
    string CustomerIdentification,
    DateTime InvoiceDate,
    DateTime? DueDate,
    bool IsCreditSale,
    decimal SubTotal,
    decimal DiscountAmount,
    decimal TaxAmount,
    decimal TotalAmount,
    string Status
);

public record SalesInvoiceDetailDto(
    Guid Id,
    string InvoiceNumber,
    Guid CustomerId,
    string CustomerName,
    string CustomerIdentification,
    Guid BranchWarehouseId,
    string BranchWarehouseName,
    Guid? SalesOrderId,
    string? SalesOrderNumber,
    Guid? OriginalInvoiceId,
    DateTime InvoiceDate,
    DateTime? DueDate,
    bool IsCreditSale,
    int PaymentTermsDays,
    string Status,
    string? CancellationReason,
    DateTime? CancelledOnUtc,
    decimal SubTotal,
    decimal DiscountAmount,
    decimal TaxAmount,
    decimal TotalAmount,
    string? Notes,
    DateTime CreatedOnUtc,
    List<SalesInvoiceDetailItemDto> Details
);

// ─── Queries ─────────────────────────────────────────────────────────────────

public record GetSalesInvoicesQuery(
    Guid? CustomerId,
    string? Status,
    bool? IsCreditSale,
    DateTime? FromDate,
    DateTime? ToDate,
    int PageNumber = 1,
    int PageSize = 20
) : IRequest<PagedResult<SalesInvoiceListItemDto>>;

public record GetSalesInvoiceByIdQuery(Guid SalesInvoiceId) : IRequest<SalesInvoiceDetailDto?>;

// ─── Handlers ────────────────────────────────────────────────────────────────

public class GetSalesInvoicesQueryHandler : IRequestHandler<GetSalesInvoicesQuery, PagedResult<SalesInvoiceListItemDto>>
{
    private readonly ISalesInvoiceRepository _repository;

    public GetSalesInvoicesQueryHandler(ISalesInvoiceRepository repository)
    {
        _repository = repository;
    }

    public async Task<PagedResult<SalesInvoiceListItemDto>> Handle(GetSalesInvoicesQuery request, CancellationToken cancellationToken)
    {
        var (items, totalCount) = await _repository.GetPagedAsync(
            request.CustomerId, request.Status, request.IsCreditSale, request.FromDate, request.ToDate,
            request.PageNumber, request.PageSize, cancellationToken);

        var dtos = items.Select(si => new SalesInvoiceListItemDto(
            si.Id,
            si.InvoiceNumber,
            si.CustomerId,
            si.CustomerNameSnapshot,
            si.CustomerIdentificationSnapshot,
            si.InvoiceDate,
            si.DueDate,
            si.IsCreditSale,
            si.SubTotal,
            si.DiscountAmount,
            si.TaxAmount,
            si.TotalAmount,
            si.Status.ToString()));

        return new PagedResult<SalesInvoiceListItemDto>(dtos.ToList(), totalCount, request.PageNumber, request.PageSize);
    }
}

public class GetSalesInvoiceByIdQueryHandler : IRequestHandler<GetSalesInvoiceByIdQuery, SalesInvoiceDetailDto?>
{
    private readonly ISalesInvoiceRepository _repository;

    public GetSalesInvoiceByIdQueryHandler(ISalesInvoiceRepository repository)
    {
        _repository = repository;
    }

    public async Task<SalesInvoiceDetailDto?> Handle(GetSalesInvoiceByIdQuery request, CancellationToken cancellationToken)
    {
        var invoice = await _repository.GetByIdWithDetailsAsync(request.SalesInvoiceId, cancellationToken);
        if (invoice == null) return null;

        var details = invoice.Details.Select(d => new SalesInvoiceDetailItemDto(
            d.Id,
            d.ProductId,
            d.ProductNameSnapshot,
            d.ProductCodeSnapshot,
            d.UnitOfMeasureId,
            d.UnitOfMeasureSnapshot,
            d.Quantity,
            d.UnitPrice,
            d.DiscountPercentage,
            d.DiscountAmount,
            d.TaxPercentage,
            d.TaxAmount,
            d.NetAmount
        )).ToList();

        return new SalesInvoiceDetailDto(
            invoice.Id,
            invoice.InvoiceNumber,
            invoice.CustomerId,
            invoice.CustomerNameSnapshot,
            invoice.CustomerIdentificationSnapshot,
            invoice.BranchWarehouseId,
            invoice.BranchWarehouse?.Warehouse?.Name ?? "N/A",
            invoice.SalesOrderId,
            invoice.SalesOrder?.OrderNumber,
            invoice.OriginalInvoiceId,
            invoice.InvoiceDate,
            invoice.DueDate,
            invoice.IsCreditSale,
            invoice.PaymentTermsDays,
            invoice.Status.ToString(),
            invoice.CancellationReason,
            invoice.CancelledOnUtc,
            invoice.SubTotal,
            invoice.DiscountAmount,
            invoice.TaxAmount,
            invoice.TotalAmount,
            invoice.Notes,
            invoice.CreatedOnUtc,
            details);
    }
}
