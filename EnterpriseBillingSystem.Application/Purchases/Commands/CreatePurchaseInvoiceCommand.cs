using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using FluentValidation;
using EnterpriseBillingSystem.Domain.Entities;
using EnterpriseBillingSystem.Domain.Enums;
using EnterpriseBillingSystem.Domain.Repositories;
using EnterpriseBillingSystem.Application.Common.Interfaces;

namespace EnterpriseBillingSystem.Application.Purchases.Commands;

public record PurchaseInvoiceDetailInput(
    Guid ProductId,
    decimal Quantity,
    Guid ProductPresentationId,
    decimal UnitPrice,
    decimal DiscountPercentage,
    decimal TaxPercentage
);

public record CreatePurchaseInvoiceCommand(
    Guid SupplierId,
    Guid? PurchaseReceiptId,
    Guid? PurchaseOrderId,
    string InvoiceNumber, // Número físico del proveedor
    DateTime InvoiceDate,
    int PaymentTermsDays,
    string? Notes,
    List<PurchaseInvoiceDetailInput> Details
) : IRequest<Guid>;

public class CreatePurchaseInvoiceCommandValidator : AbstractValidator<CreatePurchaseInvoiceCommand>
{
    public CreatePurchaseInvoiceCommandValidator(ISupplierRepository supplierRepository, IProductRepository productRepository)
    {
        RuleFor(x => x.SupplierId)
            .NotEmpty().WithMessage("El proveedor es requerido.")
            .MustAsync(async (id, cancellation) => {
                var supplier = await supplierRepository.GetByIdAsync(id);
                return supplier != null && supplier.IsActive && supplier.Status == SupplierStatus.Active;
            }).WithMessage("El proveedor especificado no existe o no está activo.");

        RuleFor(x => x.InvoiceNumber)
            .NotEmpty().WithMessage("El número de factura del proveedor es requerido.")
            .MaximumLength(50).WithMessage("El número de factura no puede exceder los 50 caracteres.");

        RuleFor(x => x.Details)
            .NotEmpty().WithMessage("La factura debe tener al menos un detalle.");

        RuleForEach(x => x.Details).SetValidator(new PurchaseInvoiceDetailInputValidator(productRepository));
    }
}

public class PurchaseInvoiceDetailInputValidator : AbstractValidator<PurchaseInvoiceDetailInput>
{
    public PurchaseInvoiceDetailInputValidator(IProductRepository productRepository)
    {
        RuleFor(x => x.ProductId)
            .NotEmpty().WithMessage("El producto es requerido.")
            .MustAsync(async (id, cancellation) => {
                var product = await productRepository.GetByIdAsync(id);
                return product != null && product.ProductStatus == ProductStatus.Active;
            }).WithMessage("El producto especificado no existe o no está activo.");

        RuleFor(x => x.Quantity)
            .GreaterThan(0).WithMessage("La cantidad debe ser mayor a 0.");

        RuleFor(x => x.ProductPresentationId)
            .NotEmpty().WithMessage("La presentación del producto es requerida.");

        RuleFor(x => x.UnitPrice)
            .GreaterThanOrEqualTo(0).WithMessage("El precio unitario no puede ser negativo.");

        RuleFor(x => x.DiscountPercentage)
            .InclusiveBetween(0, 100).WithMessage("El porcentaje de descuento debe estar entre 0 y 100.");

        RuleFor(x => x.TaxPercentage)
            .InclusiveBetween(0, 100).WithMessage("El porcentaje de impuesto debe estar entre 0 y 100.");
    }
}

public class CreatePurchaseInvoiceCommandHandler : IRequestHandler<CreatePurchaseInvoiceCommand, Guid>
{
    private readonly IPurchaseInvoiceRepository _purchaseInvoiceRepository;
    private readonly IProductRepository _productRepository;
    private readonly ICurrentUserService _currentUserService;
    private readonly IUnitOfWork _unitOfWork;

    public CreatePurchaseInvoiceCommandHandler(
        IPurchaseInvoiceRepository purchaseInvoiceRepository,
        IProductRepository productRepository,
        ICurrentUserService currentUserService,
        IUnitOfWork unitOfWork)
    {
        _purchaseInvoiceRepository = purchaseInvoiceRepository;
        _productRepository = productRepository;
        _currentUserService = currentUserService;
        _unitOfWork = unitOfWork;
    }

    public async Task<Guid> Handle(CreatePurchaseInvoiceCommand request, CancellationToken cancellationToken)
    {
        var internalNumber = await _purchaseInvoiceRepository.GenerateInternalInvoiceNumberAsync(cancellationToken);

        var invoice = new PurchaseInvoice
        {
            Id = Guid.NewGuid(),
            InvoiceNumber = request.InvoiceNumber,
            InternalInvoiceNumber = internalNumber,
            SupplierId = request.SupplierId,
            PurchaseReceiptId = request.PurchaseReceiptId,
            PurchaseOrderId = request.PurchaseOrderId,
            InvoiceDate = request.InvoiceDate,
            PaymentTermsDays = request.PaymentTermsDays,
            DueDate = request.InvoiceDate.AddDays(request.PaymentTermsDays),
            Status = PurchaseInvoiceStatus.Draft,
            Notes = request.Notes,
            CreatedBy = _currentUserService.UserId ?? "System",
            CreatedOnUtc = DateTime.UtcNow
        };

        decimal headerSubTotal = 0;
        decimal headerDiscountAmount = 0;
        decimal headerTaxAmount = 0;
        decimal headerTotalAmount = 0;

        foreach (var detailInput in request.Details)
        {
            var product = await _productRepository.GetByIdWithDetailsAsync(detailInput.ProductId, cancellationToken);
            if (product == null)
                throw new ArgumentException($"El producto con Id '{detailInput.ProductId}' no existe.");

            var presentation = product.Presentations.FirstOrDefault(p => p.Id == detailInput.ProductPresentationId);
            if (presentation == null)
                throw new ArgumentException($"La presentación especificada no existe para el producto '{product.Name}'.");
            if (!presentation.IsActive)
                throw new InvalidOperationException($"La presentación '{presentation.Name}' del producto '{product.Name}' no está activa.");
            if (!presentation.AllowPurchase)
                throw new InvalidOperationException($"La presentación '{presentation.Name}' del producto '{product.Name}' no está permitida para la compra.");

            var grossAmount = detailInput.Quantity * detailInput.UnitPrice;
            var discountAmount = grossAmount * (detailInput.DiscountPercentage / 100m);
            var netBeforeTax = grossAmount - discountAmount;
            var taxAmount = netBeforeTax * (detailInput.TaxPercentage / 100m);
            var netAmount = netBeforeTax + taxAmount;

            headerSubTotal += grossAmount;
            headerDiscountAmount += discountAmount;
            headerTaxAmount += taxAmount;
            headerTotalAmount += netAmount;

            invoice.Details.Add(new PurchaseInvoiceDetail
            {
                Id = Guid.NewGuid(),
                PurchaseInvoiceId = invoice.Id,
                ProductId = detailInput.ProductId,
                Quantity = detailInput.Quantity,
                UnitOfMeasureId = presentation.UnitOfMeasureId,
                ProductPresentationId = presentation.Id,
                UnitPrice = detailInput.UnitPrice,
                DiscountPercentage = detailInput.DiscountPercentage,
                DiscountAmount = discountAmount,
                TaxPercentage = detailInput.TaxPercentage,
                TaxAmount = taxAmount,
                NetAmount = netAmount
            });
        }

        invoice.SubTotal = headerSubTotal;
        invoice.DiscountAmount = headerDiscountAmount;
        invoice.TaxAmount = headerTaxAmount;
        invoice.TotalAmount = headerTotalAmount;

        await _purchaseInvoiceRepository.AddAsync(invoice);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return invoice.Id;
    }
}
