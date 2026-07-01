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
using EnterpriseBillingSystem.Application.JournalEntries.Commands;

namespace EnterpriseBillingSystem.Application.Sales.Commands;

public record PostSalesInvoiceCommand(
    Guid SalesInvoiceId,
    Guid? PaymentMethodId = null
) : IRequest<Unit>;

public class PostSalesInvoiceCommandValidator : AbstractValidator<PostSalesInvoiceCommand>
{
    public PostSalesInvoiceCommandValidator()
    {
        RuleFor(x => x.SalesInvoiceId)
            .NotEmpty().WithMessage("El Id de la factura es requerido.");
    }
}

public class PostSalesInvoiceCommandHandler : IRequestHandler<PostSalesInvoiceCommand, Unit>
{
    private readonly ISalesInvoiceRepository _salesInvoiceRepository;
    private readonly ICustomerRepository _customerRepository;
    private readonly IProductRepository _productRepository;
    private readonly IInventoryRepository _inventoryRepository;
    private readonly IInventoryMovementRepository _movementRepository;
    private readonly ICashSessionRepository _cashSessionRepository;
    private readonly IPaymentMethodRepository _paymentMethodRepository;
    private readonly IAccountsReceivableRepository _arRepository;
    private readonly IRepository<BranchWarehouse> _branchWarehouseRepository;
    private readonly ICurrentUserService _currentUserService;
    private readonly IMediator _mediator;
    private readonly IUnitOfWork _unitOfWork;

    public PostSalesInvoiceCommandHandler(
        ISalesInvoiceRepository salesInvoiceRepository,
        ICustomerRepository customerRepository,
        IProductRepository productRepository,
        IInventoryRepository inventoryRepository,
        IInventoryMovementRepository movementRepository,
        ICashSessionRepository cashSessionRepository,
        IPaymentMethodRepository paymentMethodRepository,
        IAccountsReceivableRepository arRepository,
        IRepository<BranchWarehouse> branchWarehouseRepository,
        ICurrentUserService currentUserService,
        IMediator mediator,
        IUnitOfWork unitOfWork)
    {
        _salesInvoiceRepository = salesInvoiceRepository;
        _customerRepository = customerRepository;
        _productRepository = productRepository;
        _inventoryRepository = inventoryRepository;
        _movementRepository = movementRepository;
        _cashSessionRepository = cashSessionRepository;
        _paymentMethodRepository = paymentMethodRepository;
        _arRepository = arRepository;
        _branchWarehouseRepository = branchWarehouseRepository;
        _currentUserService = currentUserService;
        _mediator = mediator;
        _unitOfWork = unitOfWork;
    }

    public async Task<Unit> Handle(PostSalesInvoiceCommand request, CancellationToken cancellationToken)
    {
        // 1. Obtener factura con detalles
        var invoice = await _salesInvoiceRepository.GetByIdWithDetailsAsync(request.SalesInvoiceId, cancellationToken);
        if (invoice == null)
            throw new ArgumentException($"La factura con Id '{request.SalesInvoiceId}' no existe.");

        if (invoice.Status != SalesInvoiceStatus.Draft)
            throw new InvalidOperationException($"Solo se pueden confirmar facturas en estado Borrador. Estado actual: {invoice.Status}.");

        // 2. Validar cliente y crédito
        var customer = await _customerRepository.GetByIdAsync(invoice.CustomerId);
        if (customer == null)
            throw new ArgumentException("El cliente asociado a la factura no existe.");
        if (customer.Status == CustomerStatus.Blocked || customer.Status == CustomerStatus.Inactive)
            throw new InvalidOperationException($"El cliente '{customer.Name}' no está disponible para transacciones (Estado: {customer.Status}).");

        if (invoice.IsCreditSale)
        {
            if (!customer.CanUseCredit)
                throw new InvalidOperationException($"El cliente '{customer.Name}' no tiene autorizado el uso de crédito.");

            // Validar mora y límite de crédito
            var activeArs = await _arRepository.GetActiveByCustomerIdAsync(invoice.CustomerId, cancellationToken);
            
            var hasOverdue = activeArs.Any(a => a.Status == AccountsReceivableStatus.Overdue || (a.DueDate.Date < DateTime.UtcNow.Date && a.CurrentBalance > 0));
            if (hasOverdue)
                throw new InvalidOperationException($"El cliente '{customer.Name}' tiene facturas vencidas (mora) y su crédito se encuentra bloqueado.");

            var totalActiveBalance = activeArs.Sum(a => a.CurrentBalance);
            if (totalActiveBalance + invoice.TotalAmount > customer.CreditLimit)
                throw new InvalidOperationException($"La factura excede el límite de crédito del cliente. Límite: {customer.CreditLimit}, Saldo CxC Actual: {totalActiveBalance}, Requerido: {invoice.TotalAmount}.");
        }

        // 3. Integración con Caja (Para Ventas Contado)
        CashSession? openSession = null;
        PaymentMethod? paymentMethod = null;

        if (!invoice.IsCreditSale)
        {
            var currentUserId = Guid.Parse(_currentUserService.UserId ?? throw new InvalidOperationException("Usuario no autenticado."));
            
            // Regla de Negocio: Venta al contado requiere sesión de caja abierta
            openSession = await _cashSessionRepository.GetOpenSessionByUserAsync(currentUserId, cancellationToken);
            if (openSession == null)
                throw new InvalidOperationException("Debe tener una sesión de caja abierta para poder confirmar facturas al contado.");

            // Resolver método de pago
            if (request.PaymentMethodId.HasValue)
            {
                paymentMethod = await _paymentMethodRepository.GetByIdAsync(request.PaymentMethodId.Value);
                if (paymentMethod == null || !paymentMethod.IsActive)
                    throw new ArgumentException("El método de pago especificado no existe o no está activo.");
            }
            else
            {
                paymentMethod = await _paymentMethodRepository.GetByCodeAsync("EFEC", cancellationToken)
                    ?? (await _paymentMethodRepository.FindAsync(p => p.IsCash && p.IsActive)).FirstOrDefault();

                if (paymentMethod == null)
                    throw new InvalidOperationException("No se encontró un método de pago en efectivo ('EFEC') activo configurado en el sistema.");
            }
        }

        // 4. Procesar inventario
        var movementNumber = await _movementRepository.GenerateMovementNumberAsync(cancellationToken);
        var movement = new InventoryMovement
        {
            Id = Guid.NewGuid(),
            MovementNumber = movementNumber,
            MovementType = MovementType.Sale,
            FromBranchWarehouseId = invoice.BranchWarehouseId,
            ToBranchWarehouseId = null,
            ReferenceDocument = invoice.InvoiceNumber,
            Notes = $"Salida por venta Factura {invoice.InvoiceNumber}",
            MovementDate = invoice.InvoiceDate,
            CreatedBy = _currentUserService.UserId ?? "System",
            CreatedOnUtc = DateTime.UtcNow
        };

        bool requiresMovement = false;
        decimal totalCost = 0;
        var affectedProducts = new Dictionary<Guid, Product>();
        var warehouse = await _branchWarehouseRepository.GetByIdAsync(invoice.BranchWarehouseId);

        foreach (var detail in invoice.Details)
        {
            var product = await _productRepository.GetByIdWithDetailsAsync(detail.ProductId, cancellationToken);
            if (product == null)
                throw new ArgumentException($"El producto con Id '{detail.ProductId}' no existe.");
            if (!product.IsActive)
                throw new InvalidOperationException($"El producto '{product.Name}' no está activo.");

            // Si el producto es servicio o no se trackea inventario, no afecta stock
            if (product.ProductType == ProductType.Service || !product.TrackInventory)
                continue;

            if (!affectedProducts.ContainsKey(product.Id))
            {
                affectedProducts.Add(product.Id, product);
            }

            // Obtener presentación
            var presentation = product.Presentations.FirstOrDefault(p => p.Id == detail.ProductPresentationId);
            if (presentation == null)
                throw new ArgumentException($"La presentación especificada no existe para el producto '{product.Name}'.");

            decimal conversionFactor = presentation.ConversionFactor;
            decimal quantityInBaseUnit = detail.Quantity * conversionFactor;

            // Obtener stock actual
            var inventory = await _inventoryRepository.GetByWarehouseAndProductAsync(invoice.BranchWarehouseId, detail.ProductId, cancellationToken);
            if (inventory == null)
            {
                if (warehouse == null || !warehouse.AllowNegativeInventory)
                {
                    throw new InvalidOperationException($"Stock insuficiente para el producto '{product.Name}' en la bodega de salida. Disponible: 0, Requerido: {quantityInBaseUnit} (en unidad base).");
                }
                inventory = new Domain.Entities.Inventory
                {
                    Id = Guid.NewGuid(),
                    BranchWarehouseId = invoice.BranchWarehouseId,
                    ProductId = detail.ProductId,
                    PhysicalStock = 0,
                    ReservedStock = 0,
                    CommittedStock = 0,
                    CreatedBy = _currentUserService.UserId ?? "System",
                    CreatedOnUtc = DateTime.UtcNow
                };
                await _inventoryRepository.AddAsync(inventory);
            }
            else if (warehouse == null || (!warehouse.AllowNegativeInventory && inventory.AvailableStock < quantityInBaseUnit))
            {
                throw new InvalidOperationException($"Stock insuficiente para el producto '{product.Name}' en la bodega de salida. Disponible: {inventory.AvailableStock}, Requerido: {quantityInBaseUnit} (en unidad base).");
            }

            // Descontar del inventario
            inventory.PhysicalStock -= quantityInBaseUnit;
            _inventoryRepository.Update(inventory);

            // Detalle del movimiento Kardex
            movement.Details.Add(new InventoryMovementDetail
            {
                Id = Guid.NewGuid(),
                ProductId = detail.ProductId,
                Quantity = detail.Quantity,
                UnitOfMeasureId = detail.UnitOfMeasureId,
                ProductPresentationId = detail.ProductPresentationId,
                ConversionFactor = conversionFactor,
                QuantityInBaseUnit = quantityInBaseUnit,
                CreatedBy = _currentUserService.UserId ?? "System",
                CreatedOnUtc = DateTime.UtcNow
            });

            totalCost += quantityInBaseUnit * product.CurrentCost;
            requiresMovement = true;
        }

        if (requiresMovement)
        {
            await _movementRepository.AddAsync(movement);
        }

        // 4.5 AutoMarkSoldOut
        foreach (var prod in affectedProducts.Values)
        {
            if (prod.AutoMarkSoldOut)
            {
                var activeWarehouses = await _branchWarehouseRepository.FindAsync(bw => bw.IsActive);
                var activeWarehouseIds = activeWarehouses.Select(w => w.Id).ToList();
                var inventories = await _inventoryRepository.FindAsync(i => i.ProductId == prod.Id);
                
                var totalPhysicalStock = inventories
                    .Where(i => activeWarehouseIds.Contains(i.BranchWarehouseId))
                    .Sum(i => i.PhysicalStock);

                var newIsSoldOut = totalPhysicalStock <= 0;
                if (prod.IsSoldOut != newIsSoldOut)
                {
                    prod.IsSoldOut = newIsSoldOut;
                    prod.SoldOutAt = newIsSoldOut ? DateTime.UtcNow : null;
                    prod.SoldOutBy = newIsSoldOut ? (_currentUserService.UserId ?? "System") : null;
                    _productRepository.Update(prod);
                }
            }
        }

        // 5. Registrar cobro en caja (si es contado)
        if (!invoice.IsCreditSale && openSession != null && paymentMethod != null)
        {
            var cashMovement = new CashMovement
            {
                Id = Guid.NewGuid(),
                CashSessionId = openSession.Id,
                MovementType = CashMovementType.SalePayment,
                PaymentMethodId = paymentMethod.Id,
                ReferenceDocument = invoice.InvoiceNumber,
                ReferenceId = invoice.Id,
                Amount = invoice.TotalAmount, // Almacenar siempre como positivo
                Notes = $"Cobro de Factura Contado {invoice.InvoiceNumber}",
                CreatedAt = DateTime.UtcNow
            };
            openSession.CashMovements.Add(cashMovement);
            _cashSessionRepository.Update(openSession);
        }

        // 6. Confirmar factura
        invoice.Status = SalesInvoiceStatus.Posted;
        invoice.LastModifiedBy = _currentUserService.UserId ?? "System";
        invoice.LastModifiedOnUtc = DateTime.UtcNow;

        _salesInvoiceRepository.Update(invoice);

        // 6.5 Crear Cuenta por Cobrar si es a crédito
        if (invoice.IsCreditSale)
        {
            var existingAr = await _arRepository.GetByInvoiceIdAsync(invoice.Id, cancellationToken);
            if (existingAr != null)
                throw new InvalidOperationException($"Ya existe una cuenta por cobrar registrada para la factura '{invoice.InvoiceNumber}'.");

            var ar = new Domain.Entities.AccountsReceivable
            {
                Id = Guid.NewGuid(),
                CustomerId = invoice.CustomerId,
                SalesInvoiceId = invoice.Id,
                DocumentNumber = invoice.InvoiceNumber,
                InvoiceDate = invoice.InvoiceDate,
                DueDate = invoice.DueDate ?? invoice.InvoiceDate.AddDays(invoice.PaymentTermsDays),
                OriginalAmount = invoice.TotalAmount,
                PaidAmount = 0m,
                CurrentBalance = invoice.TotalAmount,
                Status = AccountsReceivableStatus.Pending,
                Notes = invoice.Notes
            };

            await _arRepository.AddAsync(ar);
        }

        // 6.7 Generar Asiento Contable Automático
        var jeDetails = new List<JournalEntryDetailInput>();
        if (!invoice.IsCreditSale)
        {
            // Venta Contado: Dr 1110 Caja General / Cr 4100 Ventas
            jeDetails.Add(new JournalEntryDetailInput("1110", invoice.TotalAmount, 0, $"Cobro Factura Contado {invoice.InvoiceNumber}"));
            jeDetails.Add(new JournalEntryDetailInput("4100", 0, invoice.TotalAmount, $"Venta Factura Contado {invoice.InvoiceNumber}"));
        }
        else
        {
            // Venta Crédito: Dr 1200 Cuentas por Cobrar / Cr 4100 Ventas
            jeDetails.Add(new JournalEntryDetailInput("1200", invoice.TotalAmount, 0, $"CxC Factura Crédito {invoice.InvoiceNumber}"));
            jeDetails.Add(new JournalEntryDetailInput("4100", 0, invoice.TotalAmount, $"Venta Factura Crédito {invoice.InvoiceNumber}"));
        }

        if (totalCost > 0)
        {
            // Costo: Dr 5100 Costo de Ventas / Cr 1300 Inventarios
            jeDetails.Add(new JournalEntryDetailInput("5100", totalCost, 0, $"Costo de Venta Factura {invoice.InvoiceNumber}"));
            jeDetails.Add(new JournalEntryDetailInput("1300", 0, totalCost, $"Descargo Inventario Factura {invoice.InvoiceNumber}"));
        }

        var createJeCmd = new CreateJournalEntryCommand(
            EntryDate: invoice.InvoiceDate,
            Description: $"Asiento por Venta Factura {invoice.InvoiceNumber}",
            ReferenceDocument: invoice.InvoiceNumber,
            ReferenceId: invoice.Id,
            SourceModule: "Sales",
            Details: jeDetails,
            PostImmediately: true
        );

        await _mediator.Send(createJeCmd, cancellationToken);

        // Guardar todo en una transacción única
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return Unit.Value;
    }
}
