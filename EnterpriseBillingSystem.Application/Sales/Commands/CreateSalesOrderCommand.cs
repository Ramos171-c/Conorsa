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

namespace EnterpriseBillingSystem.Application.Sales.Commands;

// ─── DTO de línea ─────────────────────────────────────────────────────────────

public record SalesOrderDetailRequest(
    Guid ProductId,
    Guid UnitOfMeasureId,
    decimal Quantity,
    decimal UnitPrice,
    decimal DiscountPercentage,
    decimal TaxPercentage
);

// ─── Command ──────────────────────────────────────────────────────────────────

public record CreateSalesOrderCommand(
    Guid CustomerId,
    DateTime OrderDate,
    string? Notes,
    List<SalesOrderDetailRequest> Details
) : IRequest<Guid>;

// ─── Validator ────────────────────────────────────────────────────────────────

public class CreateSalesOrderCommandValidator : AbstractValidator<CreateSalesOrderCommand>
{
    public CreateSalesOrderCommandValidator()
    {
        RuleFor(x => x.CustomerId)
            .NotEmpty().WithMessage("El cliente es requerido.");

        RuleFor(x => x.OrderDate)
            .NotEmpty().WithMessage("La fecha del pedido es requerida.");

        RuleFor(x => x.Details)
            .NotEmpty().WithMessage("El pedido debe tener al menos un detalle.");

        RuleForEach(x => x.Details).ChildRules(d =>
        {
            d.RuleFor(x => x.ProductId)
                .NotEmpty().WithMessage("El producto es requerido.");
            d.RuleFor(x => x.UnitOfMeasureId)
                .NotEmpty().WithMessage("La unidad de medida es requerida.");
            d.RuleFor(x => x.Quantity)
                .GreaterThan(0).WithMessage("La cantidad debe ser mayor a 0.");
            d.RuleFor(x => x.UnitPrice)
                .GreaterThanOrEqualTo(0).WithMessage("El precio unitario no puede ser negativo.");
            d.RuleFor(x => x.DiscountPercentage)
                .InclusiveBetween(0, 100).WithMessage("El descuento debe estar entre 0 y 100%.");
            d.RuleFor(x => x.TaxPercentage)
                .GreaterThanOrEqualTo(0).WithMessage("El impuesto no puede ser negativo.");
        });
    }
}

// ─── Handler ──────────────────────────────────────────────────────────────────

public class CreateSalesOrderCommandHandler : IRequestHandler<CreateSalesOrderCommand, Guid>
{
    private readonly ISalesOrderRepository _salesOrderRepository;
    private readonly ICustomerRepository _customerRepository;
    private readonly IProductRepository _productRepository;
    private readonly IRepository<SystemParameter> _systemParameterRepository;
    private readonly IUnitOfWork _unitOfWork;

    public CreateSalesOrderCommandHandler(
        ISalesOrderRepository salesOrderRepository,
        ICustomerRepository customerRepository,
        IProductRepository productRepository,
        IRepository<SystemParameter> systemParameterRepository,
        IUnitOfWork unitOfWork)
    {
        _salesOrderRepository = salesOrderRepository;
        _customerRepository = customerRepository;
        _productRepository = productRepository;
        _systemParameterRepository = systemParameterRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<Guid> Handle(CreateSalesOrderCommand request, CancellationToken cancellationToken)
    {
        // 1. Validar cliente
        var customer = await _customerRepository.GetByIdAsync(request.CustomerId);
        if (customer == null)
            throw new ArgumentException("El cliente especificado no existe.");
        if (customer.Status == CustomerStatus.Blocked || customer.Status == CustomerStatus.Inactive)
            throw new InvalidOperationException($"El cliente '{customer.Name}' no está disponible para transacciones (Estado: {customer.Status}).");

        // 2. Calcular totales y construir detalles
        decimal subTotal = 0;
        decimal totalDiscount = 0;
        decimal totalTax = 0;
        var details = new List<SalesOrderDetail>();

        foreach (var req in request.Details)
        {
            var product = await _productRepository.GetByIdWithDetailsAsync(req.ProductId, cancellationToken);
            if (product == null)
                throw new ArgumentException($"El producto con Id '{req.ProductId}' no existe.");
            if (!product.IsActive)
                throw new InvalidOperationException($"El producto '{product.Name}' no está activo.");

            // Aplicar exención fiscal del cliente
            decimal effectiveTaxPct = customer.IsTaxExempt ? 0m : req.TaxPercentage;

            var discountAmount = req.Quantity * req.UnitPrice * (req.DiscountPercentage / 100m);
            var baseAmount = req.Quantity * req.UnitPrice - discountAmount;
            var taxAmount = baseAmount * (effectiveTaxPct / 100m);
            var netAmount = baseAmount + taxAmount;

            subTotal += req.Quantity * req.UnitPrice;
            totalDiscount += discountAmount;
            totalTax += taxAmount;

            details.Add(new SalesOrderDetail
            {
                Id = Guid.NewGuid(),
                ProductId = req.ProductId,
                UnitOfMeasureId = req.UnitOfMeasureId,
                Quantity = req.Quantity,
                UnitPrice = req.UnitPrice,
                DiscountPercentage = req.DiscountPercentage,
                DiscountAmount = discountAmount,
                TaxPercentage = effectiveTaxPct,
                TaxAmount = taxAmount,
                NetAmount = netAmount
            });
        }

        // 3. Generar número de pedido
        var orderNumber = await _salesOrderRepository.GenerateOrderNumberAsync(cancellationToken);

        decimal totalAmount = subTotal - totalDiscount + totalTax;
        decimal minOrderAmount = 350m; // Default fallback
        var minAmountParam = (await _systemParameterRepository.FindAsync(p => p.Key == "MinimumInvoiceAmount")).FirstOrDefault();
        if (minAmountParam != null && decimal.TryParse(minAmountParam.Value, out var parsedMin))
        {
            minOrderAmount = parsedMin;
        }

        if (totalAmount < minOrderAmount)
        {
            throw new InvalidOperationException($"El monto total del pedido de venta debe ser igual o mayor a C${minOrderAmount:N2}.");
        }

        // 4. Crear pedido
        var order = new SalesOrder
        {
            Id = Guid.NewGuid(),
            OrderNumber = orderNumber,
            CustomerId = request.CustomerId,
            OrderDate = request.OrderDate,
            SubTotal = subTotal,
            DiscountAmount = totalDiscount,
            TaxAmount = totalTax,
            TotalAmount = totalAmount,
            Status = SalesOrderStatus.Recibido,
            Notes = request.Notes,
            Details = details,
            CreatedBy = "System",
            CreatedOnUtc = DateTime.UtcNow
        };

        await _salesOrderRepository.AddAsync(order);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return order.Id;
    }
}
