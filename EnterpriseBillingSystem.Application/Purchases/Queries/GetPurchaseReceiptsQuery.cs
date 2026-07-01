using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using EnterpriseBillingSystem.Domain.Repositories;

namespace EnterpriseBillingSystem.Application.Purchases.Queries;

// ─── DTOs ────────────────────────────────────────────────────────────────────

public record PurchaseReceiptDetailItemDto(
    Guid Id,
    Guid ProductId,
    string ProductName,
    string ProductCode,
    Guid UnitOfMeasureId,
    string UnitOfMeasure,
    decimal Quantity,
    decimal UnitPrice
);

public record PurchaseReceiptListItemDto(
    Guid Id,
    string ReceiptNumber,
    Guid SupplierId,
    string SupplierName,
    string? OrderNumber,
    DateTime ReceiptDate,
    string Status
);

public record PurchaseReceiptDetailDto(
    Guid Id,
    string ReceiptNumber,
    Guid SupplierId,
    string SupplierName,
    Guid? PurchaseOrderId,
    string? OrderNumber,
    Guid BranchWarehouseId,
    string BranchName,
    string WarehouseName,
    DateTime ReceiptDate,
    string? ReferenceDocument,
    string Status,
    string? Notes,
    DateTime CreatedOnUtc,
    List<PurchaseReceiptDetailItemDto> Details
);

// ─── Queries ─────────────────────────────────────────────────────────────────

public record GetPurchaseReceiptsQuery(
    Guid? SupplierId,
    Guid? PurchaseOrderId,
    string? Status,
    DateTime? FromDate,
    DateTime? ToDate,
    int PageNumber = 1,
    int PageSize = 20
) : IRequest<PagedResult<PurchaseReceiptListItemDto>>;

public record GetPurchaseReceiptByIdQuery(Guid ReceiptId) : IRequest<PurchaseReceiptDetailDto?>;

// ─── Handlers ────────────────────────────────────────────────────────────────

public class GetPurchaseReceiptsQueryHandler : IRequestHandler<GetPurchaseReceiptsQuery, PagedResult<PurchaseReceiptListItemDto>>
{
    private readonly IPurchaseReceiptRepository _repository;

    public GetPurchaseReceiptsQueryHandler(IPurchaseReceiptRepository repository)
    {
        _repository = repository;
    }

    public async Task<PagedResult<PurchaseReceiptListItemDto>> Handle(GetPurchaseReceiptsQuery request, CancellationToken cancellationToken)
    {
        var (items, totalCount) = await _repository.GetPagedAsync(
            request.SupplierId, request.PurchaseOrderId, request.Status,
            request.FromDate, request.ToDate,
            request.PageNumber, request.PageSize, cancellationToken);

        var dtos = items.Select(r => new PurchaseReceiptListItemDto(
            r.Id, r.ReceiptNumber, r.SupplierId,
            r.Supplier?.Name ?? string.Empty,
            r.PurchaseOrder?.OrderNumber,
            r.ReceiptDate, r.Status.ToString()));

        return new PagedResult<PurchaseReceiptListItemDto>(dtos, totalCount, request.PageNumber, request.PageSize);
    }
}

public class GetPurchaseReceiptByIdQueryHandler : IRequestHandler<GetPurchaseReceiptByIdQuery, PurchaseReceiptDetailDto?>
{
    private readonly IPurchaseReceiptRepository _repository;

    public GetPurchaseReceiptByIdQueryHandler(IPurchaseReceiptRepository repository)
    {
        _repository = repository;
    }

    public async Task<PurchaseReceiptDetailDto?> Handle(GetPurchaseReceiptByIdQuery request, CancellationToken cancellationToken)
    {
        var receipt = await _repository.GetByIdWithDetailsAsync(request.ReceiptId, cancellationToken);
        if (receipt == null) return null;

        var details = receipt.Details.Select(d => new PurchaseReceiptDetailItemDto(
            d.Id, d.ProductId,
            d.Product?.Name ?? string.Empty,
            d.Product?.InternalCode ?? string.Empty,
            d.UnitOfMeasureId,
            d.UnitOfMeasure?.Code ?? string.Empty,
            d.Quantity, d.UnitPrice
        )).ToList();

        return new PurchaseReceiptDetailDto(
            receipt.Id, receipt.ReceiptNumber,
            receipt.SupplierId, receipt.Supplier?.Name ?? string.Empty,
            receipt.PurchaseOrderId, receipt.PurchaseOrder?.OrderNumber,
            receipt.BranchWarehouseId,
            receipt.BranchWarehouse?.Branch?.Name ?? string.Empty,
            receipt.BranchWarehouse?.Warehouse?.Name ?? string.Empty,
            receipt.ReceiptDate, receipt.ReferenceDocument,
            receipt.Status.ToString(), receipt.Notes,
            receipt.CreatedOnUtc, details);
    }
}
