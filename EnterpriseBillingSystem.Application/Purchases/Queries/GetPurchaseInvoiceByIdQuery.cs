using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using EnterpriseBillingSystem.Domain.Repositories;

namespace EnterpriseBillingSystem.Application.Purchases.Queries;

public record PurchaseInvoiceDetailItemDto(
    Guid Id,
    Guid ProductId,
    string ProductName,
    string ProductCode,
    decimal Quantity,
    Guid UnitOfMeasureId,
    string UnitOfMeasureName,
    decimal UnitPrice,
    decimal DiscountPercentage,
    decimal DiscountAmount,
    decimal TaxPercentage,
    decimal TaxAmount,
    decimal NetAmount
);

public record PurchaseInvoiceDetailDto(
    Guid Id,
    string InvoiceNumber,
    string InternalInvoiceNumber,
    Guid? PurchaseReceiptId,
    string? PurchaseReceiptNumber,
    Guid? PurchaseOrderId,
    string? PurchaseOrderNumber,
    Guid SupplierId,
    string SupplierName,
    string SupplierCode,
    DateTime InvoiceDate,
    DateTime? DueDate,
    int PaymentTermsDays,
    decimal SubTotal,
    decimal DiscountAmount,
    decimal TaxAmount,
    decimal TotalAmount,
    string Status,
    string? Notes,
    List<PurchaseInvoiceDetailItemDto> Details
);

public record GetPurchaseInvoiceByIdQuery(Guid Id) : IRequest<PurchaseInvoiceDetailDto?>;

public class GetPurchaseInvoiceByIdQueryHandler : IRequestHandler<GetPurchaseInvoiceByIdQuery, PurchaseInvoiceDetailDto?>
{
    private readonly IPurchaseInvoiceRepository _purchaseInvoiceRepository;

    public GetPurchaseInvoiceByIdQueryHandler(IPurchaseInvoiceRepository purchaseInvoiceRepository)
    {
        _purchaseInvoiceRepository = purchaseInvoiceRepository;
    }

    public async Task<PurchaseInvoiceDetailDto?> Handle(GetPurchaseInvoiceByIdQuery request, CancellationToken cancellationToken)
    {
        var invoice = await _purchaseInvoiceRepository.GetByIdWithDetailsAsync(request.Id, cancellationToken);
        if (invoice == null) return null;

        var details = invoice.Details
            .Select(d => new PurchaseInvoiceDetailItemDto(
                d.Id,
                d.ProductId,
                d.Product?.Name ?? "N/A",
                d.Product?.InternalCode ?? "N/A",
                d.Quantity,
                d.UnitOfMeasureId,
                d.UnitOfMeasure?.Name ?? "N/A",
                d.UnitPrice,
                d.DiscountPercentage,
                d.DiscountAmount,
                d.TaxPercentage,
                d.TaxAmount,
                d.NetAmount
            )).ToList();

        return new PurchaseInvoiceDetailDto(
            invoice.Id,
            invoice.InvoiceNumber,
            invoice.InternalInvoiceNumber,
            invoice.PurchaseReceiptId,
            invoice.PurchaseReceipt?.ReceiptNumber,
            invoice.PurchaseOrderId,
            invoice.PurchaseOrder?.OrderNumber,
            invoice.SupplierId,
            invoice.Supplier?.Name ?? "N/A",
            invoice.Supplier?.SupplierCode ?? "N/A",
            invoice.InvoiceDate,
            invoice.DueDate,
            invoice.PaymentTermsDays,
            invoice.SubTotal,
            invoice.DiscountAmount,
            invoice.TaxAmount,
            invoice.TotalAmount,
            invoice.Status.ToString(),
            invoice.Notes,
            details
        );
    }
}
