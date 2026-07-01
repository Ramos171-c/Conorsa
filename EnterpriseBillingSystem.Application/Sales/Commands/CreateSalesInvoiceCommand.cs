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

namespace EnterpriseBillingSystem.Application.Sales.Commands;

public record SalesInvoiceDetailRequest(
    Guid ProductId,
    Guid ProductPresentationId,
    decimal Quantity,
    decimal UnitPrice,
    decimal DiscountPercentage,
    decimal TaxPercentage
);

public record CreateSalesInvoiceCommand(
    Guid CustomerId,
    Guid BranchWarehouseId,
    Guid? SalesOrderId,
    DateTime InvoiceDate,
    bool IsCreditSale,
    int PaymentTermsDays,
    string? Notes,
    CustomerType CustomerType,
    List<SalesInvoiceDetailRequest> Details
) : IRequest<Guid>;

public class CreateSalesInvoiceCommandValidator : AbstractValidator<CreateSalesInvoiceCommand>
{
    public CreateSalesInvoiceCommandValidator()
    {
        RuleFor(x => x.CustomerId)
            .NotEmpty().WithMessage("El cliente es requerido.");

        RuleFor(x => x.BranchWarehouseId)
            .NotEmpty().WithMessage("La bodega de salida es requerida.");

        RuleFor(x => x.InvoiceDate)
            .NotEmpty().WithMessage("La fecha de la factura es requerida.");

        RuleFor(x => x.PaymentTermsDays)
            .GreaterThanOrEqualTo(0).WithMessage("Los días de crédito no pueden ser negativos.");

        RuleFor(x => x.Details)
            .NotEmpty().When(x => !x.SalesOrderId.HasValue)
            .WithMessage("Los detalles de la factura son requeridos si no se especifica un pedido.");

        RuleForEach(x => x.Details).ChildRules(d =>
        {
            d.RuleFor(x => x.ProductId)
                .NotEmpty().WithMessage("El producto es requerido.");
            d.RuleFor(x => x.ProductPresentationId)
                .NotEmpty().WithMessage("La presentación del producto es requerida.");
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

public class CreateSalesInvoiceCommandHandler : IRequestHandler<CreateSalesInvoiceCommand, Guid>
{
    private readonly ISalesInvoiceRepository _salesInvoiceRepository;
    private readonly ISalesOrderRepository _salesOrderRepository;
    private readonly ICustomerRepository _customerRepository;
    private readonly IProductRepository _productRepository;
    private readonly IRepository<UnitOfMeasure> _uomRepository;
    private readonly IRepository<BranchWarehouse> _branchWarehouseRepository;
    private readonly IRepository<SystemParameter> _systemParameterRepository;
    private readonly ICurrentUserService _currentUserService;
    private readonly IUnitOfWork _unitOfWork;

    public CreateSalesInvoiceCommandHandler(
        ISalesInvoiceRepository salesInvoiceRepository,
        ISalesOrderRepository salesOrderRepository,
        ICustomerRepository customerRepository,
        IProductRepository productRepository,
        IRepository<UnitOfMeasure> uomRepository,
        IRepository<BranchWarehouse> branchWarehouseRepository,
        IRepository<SystemParameter> systemParameterRepository,
        ICurrentUserService currentUserService,
        IUnitOfWork unitOfWork)
    {
        _salesInvoiceRepository = salesInvoiceRepository;
        _salesOrderRepository = salesOrderRepository;
        _customerRepository = customerRepository;
        _productRepository = productRepository;
        _uomRepository = uomRepository;
        _branchWarehouseRepository = branchWarehouseRepository;
        _systemParameterRepository = systemParameterRepository;
        _currentUserService = currentUserService;
        _unitOfWork = unitOfWork;
    }

    public async Task<Guid> Handle(CreateSalesInvoiceCommand request, CancellationToken cancellationToken)
    {
        // 1. Validar cliente
        var customer = await _customerRepository.GetByIdAsync(request.CustomerId);
        if (customer == null)
            throw new ArgumentException("El cliente especificado no existe.");
        if (customer.Status == CustomerStatus.Blocked || customer.Status == CustomerStatus.Inactive)
            throw new InvalidOperationException($"El cliente '{customer.Name}' no está disponible para transacciones (Estado: {customer.Status}).");

        // Validar crédito
        if (request.IsCreditSale)
        {
            if (!customer.CanUseCredit)
                throw new InvalidOperationException($"El cliente '{customer.Name}' no tiene autorizado el uso de crédito.");
        }

        // 2. Validar bodega
        var warehouse = await _branchWarehouseRepository.GetByIdAsync(request.BranchWarehouseId);
        if (warehouse == null)
            throw new ArgumentException("La bodega de salida especificada no existe.");
        if (!warehouse.IsActive)
            throw new InvalidOperationException("La bodega de salida especificada no está activa.");

        // 3. Validar pedido si existe
        SalesOrder? salesOrder = null;
        if (request.SalesOrderId.HasValue)
        {
            salesOrder = await _salesOrderRepository.GetByIdWithDetailsAsync(request.SalesOrderId.Value, cancellationToken);
            if (salesOrder == null)
                throw new ArgumentException("El pedido especificado no existe.");
            if (salesOrder.CustomerId != request.CustomerId)
                throw new InvalidOperationException("El pedido no pertenece al cliente especificado.");
            if (salesOrder.Status != SalesOrderStatus.Recibido)
                throw new InvalidOperationException($"El pedido debe estar en estado Recibido para poder facturarse. Estado actual: {salesOrder.Status}.");
        }

        // 4. Calcular detalles y snapshots
        decimal subTotal = 0;
        decimal totalDiscount = 0;
        decimal totalTax = 0;
        var details = new List<SalesInvoiceDetail>();

        // Si se especificó un pedido y no se mandaron detalles, los jalamos del pedido
        var sourceDetails = (request.Details == null || !request.Details.Any()) && salesOrder != null
            ? salesOrder.Details.Select(d => new SalesInvoiceDetailRequest(
                d.ProductId,
                Guid.Empty, // Placeholder para resolver en el ciclo
                d.Quantity,
                d.UnitPrice,
                d.DiscountPercentage,
                d.TaxPercentage
            )).ToList()
            : request.Details;

        if (sourceDetails == null || !sourceDetails.Any())
            throw new InvalidOperationException("No se especificaron detalles para la factura.");

        foreach (var req in sourceDetails)
        {
            var product = await _productRepository.GetByIdWithDetailsAsync(req.ProductId, cancellationToken);
            if (product == null)
                throw new ArgumentException($"El producto con Id '{req.ProductId}' no existe.");
            if (!product.IsActive)
                throw new InvalidOperationException($"El producto '{product.Name}' no está activo.");

            ProductPresentation? presentation = null;
            if (req.ProductPresentationId != Guid.Empty)
            {
                presentation = product.Presentations.FirstOrDefault(p => p.Id == req.ProductPresentationId);
            }
            else if (salesOrder != null)
            {
                var salesOrderDetail = salesOrder.Details.FirstOrDefault(d => d.ProductId == req.ProductId);
                if (salesOrderDetail != null)
                {
                    presentation = product.Presentations.FirstOrDefault(p => p.UnitOfMeasureId == salesOrderDetail.UnitOfMeasureId && p.IsActive)
                                   ?? product.Presentations.FirstOrDefault(p => p.IsDefaultSalePresentation && p.IsActive)
                                   ?? product.Presentations.FirstOrDefault(p => p.IsActive);
                }
            }

            if (presentation == null)
                throw new ArgumentException($"La presentación especificada no existe para el producto '{product.Name}'.");
            if (!presentation.IsActive)
                throw new InvalidOperationException($"La presentación '{presentation.Name}' del producto '{product.Name}' no está activa.");
            if (!presentation.AllowSale)
                throw new InvalidOperationException($"La presentación '{presentation.Name}' del producto '{product.Name}' no está permitida para la venta.");

            var uom = await _uomRepository.GetByIdAsync(presentation.UnitOfMeasureId);
            if (uom == null)
                throw new ArgumentException($"La unidad de medida con Id '{presentation.UnitOfMeasureId}' no existe.");

            // Aplicar exención fiscal del cliente
            decimal effectiveTaxPct = customer.IsTaxExempt ? 0m : req.TaxPercentage;

            var discountAmount = req.Quantity * req.UnitPrice * (req.DiscountPercentage / 100m);
            var baseAmount = req.Quantity * req.UnitPrice - discountAmount;
            var taxAmount = baseAmount * (effectiveTaxPct / 100m);
            var netAmount = baseAmount + taxAmount;

            subTotal += req.Quantity * req.UnitPrice;
            totalDiscount += discountAmount;
            totalTax += taxAmount;

            details.Add(new SalesInvoiceDetail
            {
                Id = Guid.NewGuid(),
                ProductId = req.ProductId,
                UnitOfMeasureId = presentation.UnitOfMeasureId,
                ProductPresentationId = presentation.Id,
                Quantity = req.Quantity,
                UnitPrice = req.UnitPrice,
                DiscountPercentage = req.DiscountPercentage,
                DiscountAmount = discountAmount,
                TaxPercentage = effectiveTaxPct,
                TaxAmount = taxAmount,
                NetAmount = netAmount,
                // Snapshots históricos
                ProductCodeSnapshot = product.InternalCode,
                ProductNameSnapshot = product.Name,
                UnitOfMeasureSnapshot = uom.Name
            });
        }

        // Calcular fecha de vencimiento si es crédito
        DateTime? dueDate = request.IsCreditSale
            ? request.InvoiceDate.AddDays(request.PaymentTermsDays)
            : null;

        // 5. Generar número de factura
        var invoiceNumber = await _salesInvoiceRepository.GenerateInvoiceNumberAsync(cancellationToken);

        decimal totalAmount = subTotal - totalDiscount + totalTax;
        decimal minInvoiceAmount = 350m; // Default fallback
        var minAmountParam = (await _systemParameterRepository.FindAsync(p => p.Key == "MinimumInvoiceAmount")).FirstOrDefault();
        if (minAmountParam != null && decimal.TryParse(minAmountParam.Value, out var parsedMin))
        {
            minInvoiceAmount = parsedMin;
        }

        if (totalAmount < minInvoiceAmount)
        {
            throw new InvalidOperationException($"El monto total de la factura de venta debe ser igual o mayor a C${minInvoiceAmount:N2}.");
        }

        // 6. Crear factura
        var invoice = new SalesInvoice
        {
            Id = Guid.NewGuid(),
            InvoiceNumber = invoiceNumber,
            CustomerId = request.CustomerId,
            CustomerNameSnapshot = customer.Name,
            CustomerIdentificationSnapshot = customer.IdentificationNumber,
            CustomerType = request.CustomerType,
            BranchWarehouseId = request.BranchWarehouseId,
            SalesOrderId = request.SalesOrderId,
            InvoiceDate = request.InvoiceDate,
            DueDate = dueDate,
            IsCreditSale = request.IsCreditSale,
            PaymentTermsDays = request.IsCreditSale ? request.PaymentTermsDays : 0,
            Status = SalesInvoiceStatus.Draft,
            SubTotal = subTotal,
            DiscountAmount = totalDiscount,
            TaxAmount = totalTax,
            TotalAmount = totalAmount,
            Notes = request.Notes,
            Details = details,
            CreatedBy = _currentUserService.UserId ?? "System",
            CreatedOnUtc = DateTime.UtcNow
        };

        await _salesInvoiceRepository.AddAsync(invoice);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return invoice.Id;
    }
}
