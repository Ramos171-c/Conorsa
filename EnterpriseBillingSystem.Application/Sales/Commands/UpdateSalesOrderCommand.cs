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

// ─── Command ──────────────────────────────────────────────────────────────────

public record UpdateSalesOrderItemInput(
    Guid ProductId,
    Guid UnitOfMeasureId,
    decimal Quantity,
    decimal UnitPrice,
    decimal DiscountPercentage,
    decimal TaxPercentage
);

public record UpdateSalesOrderCommand(
    Guid Id,
    Guid CustomerId,
    DateTime OrderDate,
    string? Notes,
    List<UpdateSalesOrderItemInput> Details
) : IRequest<Unit>;

// ─── Validator ────────────────────────────────────────────────────────────────

public class UpdateSalesOrderCommandValidator : AbstractValidator<UpdateSalesOrderCommand>
{
    public UpdateSalesOrderCommandValidator()
    {
        RuleFor(x => x.Id)
            .NotEmpty().WithMessage("El Id del pedido es requerido.");

        RuleFor(x => x.CustomerId)
            .NotEmpty().WithMessage("El cliente es requerido.");

        RuleFor(x => x.Details)
            .NotEmpty().WithMessage("El pedido debe contener al menos un producto.")
            .Must(details => details != null && details.Count > 0).WithMessage("El pedido debe contener al menos un producto.");

        RuleForEach(x => x.Details).ChildRules(detail =>
        {
            detail.RuleFor(d => d.ProductId)
                .NotEmpty().WithMessage("El producto es requerido.");

            detail.RuleFor(d => d.UnitOfMeasureId)
                .NotEmpty().WithMessage("La unidad de medida es requerida.");

            detail.RuleFor(d => d.Quantity)
                .GreaterThan(0).WithMessage("La cantidad debe ser mayor a cero.");

            detail.RuleFor(d => d.UnitPrice)
                .GreaterThanOrEqualTo(0).WithMessage("El precio unitario debe ser mayor o igual a cero.");

            detail.RuleFor(d => d.DiscountPercentage)
                .InclusiveBetween(0m, 100m).WithMessage("El descuento debe estar entre 0% y 100%.");
        });
    }
}

// ─── Handler ──────────────────────────────────────────────────────────────────

public class UpdateSalesOrderCommandHandler : IRequestHandler<UpdateSalesOrderCommand, Unit>
{
    private readonly ISalesOrderRepository _salesOrderRepository;
    private readonly ICustomerRepository _customerRepository;
    private readonly IProductRepository _productRepository;
    private readonly IRepository<SystemParameter> _systemParameterRepository;
    private readonly IRepository<SalesOrderDetail> _salesOrderDetailRepository;
    private readonly ICurrentUserService _currentUserService;
    private readonly IUnitOfWork _unitOfWork;

    public UpdateSalesOrderCommandHandler(
        ISalesOrderRepository salesOrderRepository,
        ICustomerRepository customerRepository,
        IProductRepository productRepository,
        IRepository<SystemParameter> systemParameterRepository,
        IRepository<SalesOrderDetail> salesOrderDetailRepository,
        ICurrentUserService currentUserService,
        IUnitOfWork unitOfWork)
    {
        _salesOrderRepository = salesOrderRepository;
        _customerRepository = customerRepository;
        _productRepository = productRepository;
        _systemParameterRepository = systemParameterRepository;
        _salesOrderDetailRepository = salesOrderDetailRepository;
        _currentUserService = currentUserService;
        _unitOfWork = unitOfWork;
    }

    public async Task<Unit> Handle(UpdateSalesOrderCommand request, CancellationToken cancellationToken)
    {
        // 1. Validar pedido
        var order = await _salesOrderRepository.GetByIdWithDetailsAsync(request.Id, cancellationToken);
        if (order == null)
            throw new ArgumentException("El pedido especificado no existe.");

        if (order.Status == SalesOrderStatus.Anulado || order.Status == SalesOrderStatus.Completado)
            throw new InvalidOperationException($"No se puede editar un pedido en estado {order.Status}.");

        // Regla de 10 minutos (Exención para Administradores)
        bool isAdmin = _currentUserService.IsAdmin;
        if (!isAdmin && order.CreatedOnUtc.AddMinutes(10) < DateTime.UtcNow)
            throw new InvalidOperationException("El pedido ya no se puede editar porque han transcurrido más de 10 minutos desde su creación.");

        // Verificar que no tenga facturas confirmadas
        bool hasPostedInvoices = order.SalesInvoices.Any(si => si.Status == SalesInvoiceStatus.Posted);
        if (hasPostedInvoices)
            throw new InvalidOperationException("No se puede editar un pedido con facturas confirmadas asociadas.");

        // 2. Validar cliente
        var customer = await _customerRepository.GetByIdAsync(request.CustomerId);
        if (customer == null)
            throw new ArgumentException("El cliente especificado no existe.");
        if (customer.Status == CustomerStatus.Blocked || customer.Status == CustomerStatus.Inactive)
            throw new InvalidOperationException($"El cliente '{customer.Name}' no está disponible para transacciones (Estado: {customer.Status}).");

        // 3. Sincronizar detalles (Agregar, Actualizar o Eliminar)
        decimal subTotal = 0;
        decimal totalDiscount = 0;
        decimal totalTax = 0;

        // 3.1 Eliminar detalles que ya no vienen en la solicitud
        // IMPORTANTE: se compara por ProductId + UnitOfMeasureId para soportar el mismo
        // producto en dos presentaciones distintas dentro del mismo pedido.
        var requestedKeys = request.Details
            .Select(d => (d.ProductId, d.UnitOfMeasureId))
            .ToHashSet();
        var detailsToRemove = order.Details
            .Where(d => !requestedKeys.Contains((d.ProductId, d.UnitOfMeasureId)))
            .ToList();
        foreach (var detail in detailsToRemove)
        {
            _salesOrderDetailRepository.Remove(detail);
            order.Details.Remove(detail);
        }

        // 3.2 Agregar o actualizar detalles
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

            var existingDetail = order.Details.FirstOrDefault(d =>
                d.ProductId == req.ProductId && d.UnitOfMeasureId == req.UnitOfMeasureId);
            if (existingDetail != null)
            {
                existingDetail.UnitOfMeasureId = req.UnitOfMeasureId;
                existingDetail.Quantity = req.Quantity;
                existingDetail.UnitPrice = req.UnitPrice;
                existingDetail.DiscountPercentage = req.DiscountPercentage;
                existingDetail.DiscountAmount = discountAmount;
                existingDetail.TaxPercentage = effectiveTaxPct;
                existingDetail.TaxAmount = taxAmount;
                existingDetail.NetAmount = netAmount;
            }
            else
            {
                var newDetail = new SalesOrderDetail
                {
                    Id = Guid.NewGuid(),
                    SalesOrderId = order.Id,
                    ProductId = req.ProductId,
                    UnitOfMeasureId = req.UnitOfMeasureId,
                    Quantity = req.Quantity,
                    OriginalPresaleQuantity = req.Quantity,
                    UnitPrice = req.UnitPrice,
                    DiscountPercentage = req.DiscountPercentage,
                    DiscountAmount = discountAmount,
                    TaxPercentage = effectiveTaxPct,
                    TaxAmount = taxAmount,
                    NetAmount = netAmount
                };
                await _salesOrderDetailRepository.AddAsync(newDetail);
                order.Details.Add(newDetail);
            }
        }

        // Validar compra mínima dinámica
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

        // 4. Actualizar pedido
        order.CustomerId = request.CustomerId;
        order.OrderDate = request.OrderDate;
        order.SubTotal = subTotal;
        order.DiscountAmount = totalDiscount;
        order.TaxAmount = totalTax;
        order.TotalAmount = totalAmount;
        order.Notes = request.Notes;

        order.LastModifiedBy = "System";
        order.LastModifiedOnUtc = DateTime.UtcNow;

        try
        {
            await _unitOfWork.SaveChangesAsync(cancellationToken);
        }
        catch (Microsoft.EntityFrameworkCore.DbUpdateConcurrencyException ex)
        {
            foreach (var entry in ex.Entries)
            {
                var databaseValues = await entry.GetDatabaseValuesAsync(cancellationToken);
                if (databaseValues == null)
                {
                    // La fila ya fue eliminada de la base de datos (por ejemplo, por borrado en cascada),
                    // por lo que la desvinculamos del contexto para que EF Core no falle intentando borrarla de nuevo.
                    entry.State = Microsoft.EntityFrameworkCore.EntityState.Detached;
                }
                else
                {
                    entry.OriginalValues.SetValues(databaseValues);
                }
            }
            await _unitOfWork.SaveChangesAsync(cancellationToken);
        }

        return Unit.Value;
    }
}
