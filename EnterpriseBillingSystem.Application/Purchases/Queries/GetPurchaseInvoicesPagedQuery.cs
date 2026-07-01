using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using EnterpriseBillingSystem.Application.Common.Models;
using EnterpriseBillingSystem.Domain.Repositories;

namespace EnterpriseBillingSystem.Application.Purchases.Queries;

public record PurchaseInvoiceListItemDto(
    Guid Id,
    string InvoiceNumber,
    string InternalInvoiceNumber,
    Guid SupplierId,
    string SupplierName,
    string SupplierCode,
    DateTime InvoiceDate,
    DateTime? DueDate,
    decimal TotalAmount,
    string Status,
    string? Notes
);

public record GetPurchaseInvoicesPagedQuery(
    Guid? SupplierId,
    string? Status,
    DateTime? FromDate,
    DateTime? ToDate,
    int PageNumber = 1,
    int PageSize = 20
) : IRequest<PagedResult<PurchaseInvoiceListItemDto>>;

public class GetPurchaseInvoicesPagedQueryHandler : IRequestHandler<GetPurchaseInvoicesPagedQuery, PagedResult<PurchaseInvoiceListItemDto>>
{
    private readonly IPurchaseInvoiceRepository _purchaseInvoiceRepository;

    public GetPurchaseInvoicesPagedQueryHandler(IPurchaseInvoiceRepository purchaseInvoiceRepository)
    {
        _purchaseInvoiceRepository = purchaseInvoiceRepository;
    }

    public async Task<PagedResult<PurchaseInvoiceListItemDto>> Handle(GetPurchaseInvoicesPagedQuery request, CancellationToken cancellationToken)
    {
        var (items, totalCount) = await _purchaseInvoiceRepository.GetPagedAsync(
            request.SupplierId,
            request.Status,
            request.FromDate,
            request.ToDate,
            request.PageNumber,
            request.PageSize,
            cancellationToken);

        var dtos = items.Select(pi => new PurchaseInvoiceListItemDto(
            pi.Id,
            pi.InvoiceNumber,
            pi.InternalInvoiceNumber,
            pi.SupplierId,
            pi.Supplier?.Name ?? "N/A",
            pi.Supplier?.SupplierCode ?? "N/A",
            pi.InvoiceDate,
            pi.DueDate,
            pi.TotalAmount,
            pi.Status.ToString(),
            pi.Notes
        )).ToList();

        return new PagedResult<PurchaseInvoiceListItemDto>(dtos, totalCount, request.PageNumber, request.PageSize);
    }
}
