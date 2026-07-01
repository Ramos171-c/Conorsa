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

namespace EnterpriseBillingSystem.Application.Purchases.Commands;

// ─── DTOs ────────────────────────────────────────────────────────────────────

public record PurchaseOrderDetailRequest(
    Guid ProductId,
    Guid UnitOfMeasureId,
    decimal Quantity,
    decimal UnitPrice,
    decimal DiscountPercentage,
    decimal TaxPercentage
);

public record CreatePurchaseOrderCommand(
    Guid SupplierId,
    DateTime OrderDate,
    DateTime? ExpectedDeliveryDate,
    string? Notes,
    List<PurchaseOrderDetailRequest> Details
) : IRequest<Guid>;

// ─── Validator ────────────────────────────────────────────────────────────────

public class CreatePurchaseOrderCommandValidator : AbstractValidator<CreatePurchaseOrderCommand>
{
    public CreatePurchaseOrderCommandValidator()
    {
        RuleFor(x => x.SupplierId)
            .NotEmpty().WithMessage("El proveedor es requerido.");

        RuleFor(x => x.OrderDate)
            .NotEmpty().WithMessage("La fecha de orden es requerida.");

        RuleFor(x => x.Details)
            .NotEmpty().WithMessage("La orden de compra debe tener al menos un detalle.")
            .Must(d => d.Count > 0).WithMessage("La orden de compra debe tener al menos un detalle.");

        RuleForEach(x => x.Details).ChildRules(detail =>
        {
            detail.RuleFor(d => d.ProductId)
                .NotEmpty().WithMessage("El producto es requerido.");

            detail.RuleFor(d => d.UnitOfMeasureId)
                .NotEmpty().WithMessage("La unidad de medida es requerida.");

            detail.RuleFor(d => d.Quantity)
                .GreaterThan(0).WithMessage("La cantidad debe ser mayor a 0.");

            detail.RuleFor(d => d.UnitPrice)
                .GreaterThanOrEqualTo(0).WithMessage("El precio unitario no puede ser negativo.");

            detail.RuleFor(d => d.DiscountPercentage)
                .InclusiveBetween(0, 100).WithMessage("El descuento debe estar entre 0 y 100.");

            detail.RuleFor(d => d.TaxPercentage)
                .GreaterThanOrEqualTo(0).WithMessage("El impuesto no puede ser negativo.");
        });
    }
}

// ─── Handler ─────────────────────────────────────────────────────────────────

public class CreatePurchaseOrderCommandHandler : IRequestHandler<CreatePurchaseOrderCommand, Guid>
{
    private readonly IPurchaseOrderRepository _purchaseOrderRepository;
    private readonly ISupplierRepository _supplierRepository;
    private readonly IProductRepository _productRepository;
    private readonly IUnitOfWork _unitOfWork;

    public CreatePurchaseOrderCommandHandler(
        IPurchaseOrderRepository purchaseOrderRepository,
        ISupplierRepository supplierRepository,
        IProductRepository productRepository,
        IUnitOfWork unitOfWork)
    {
        _purchaseOrderRepository = purchaseOrderRepository;
        _supplierRepository = supplierRepository;
        _productRepository = productRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<Guid> Handle(CreatePurchaseOrderCommand request, CancellationToken cancellationToken)
    {
        // 1. Validar proveedor
        var supplier = await _supplierRepository.GetByIdAsync(request.SupplierId);
        if (supplier == null)
            throw new ArgumentException("El proveedor especificado no existe.");
        if (supplier.Status == SupplierStatus.Blocked || supplier.Status == SupplierStatus.Inactive)
            throw new InvalidOperationException($"El proveedor '{supplier.Name}' no está disponible para transacciones (Estado: {supplier.Status}).");

        // 2. Validar productos y calcular totales
        decimal subTotal = 0;
        decimal totalDiscount = 0;
        decimal totalTax = 0;
        var orderDetails = new List<PurchaseOrderDetail>();

        foreach (var detailRequest in request.Details)
        {
            var product = await _productRepository.GetByIdWithDetailsAsync(detailRequest.ProductId, cancellationToken);
            if (product == null)
                throw new ArgumentException($"El producto con Id '{detailRequest.ProductId}' no existe.");
            if (!product.IsActive)
                throw new InvalidOperationException($"El producto '{product.Name}' no está activo.");

            var discountAmount = detailRequest.Quantity * detailRequest.UnitPrice * (detailRequest.DiscountPercentage / 100m);
            var baseAmount = detailRequest.Quantity * detailRequest.UnitPrice - discountAmount;
            var taxAmount = baseAmount * (detailRequest.TaxPercentage / 100m);
            var netAmount = baseAmount + taxAmount;

            subTotal += detailRequest.Quantity * detailRequest.UnitPrice;
            totalDiscount += discountAmount;
            totalTax += taxAmount;

            orderDetails.Add(new PurchaseOrderDetail
            {
                Id = Guid.NewGuid(),
                ProductId = detailRequest.ProductId,
                UnitOfMeasureId = detailRequest.UnitOfMeasureId,
                Quantity = detailRequest.Quantity,
                ReceivedQuantity = 0,
                UnitPrice = detailRequest.UnitPrice,
                DiscountPercentage = detailRequest.DiscountPercentage,
                DiscountAmount = discountAmount,
                TaxPercentage = detailRequest.TaxPercentage,
                TaxAmount = taxAmount,
                NetAmount = netAmount
            });
        }

        // 3. Generar número de orden
        var orderNumber = await _purchaseOrderRepository.GenerateOrderNumberAsync(cancellationToken);

        // 4. Crear la orden
        var purchaseOrder = new PurchaseOrder
        {
            Id = Guid.NewGuid(),
            OrderNumber = orderNumber,
            SupplierId = request.SupplierId,
            OrderDate = request.OrderDate,
            ExpectedDeliveryDate = request.ExpectedDeliveryDate,
            SubTotal = subTotal,
            DiscountAmount = totalDiscount,
            TaxAmount = totalTax,
            TotalAmount = subTotal - totalDiscount + totalTax,
            Status = PurchaseOrderStatus.Draft,
            Notes = request.Notes,
            Details = orderDetails,
            CreatedBy = "System",
            CreatedOnUtc = DateTime.UtcNow
        };

        await _purchaseOrderRepository.AddAsync(purchaseOrder);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return purchaseOrder.Id;
    }
}
