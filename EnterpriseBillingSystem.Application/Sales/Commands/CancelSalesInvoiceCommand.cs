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

public record CancelSalesInvoiceCommand(
    Guid SalesInvoiceId,
    string CancellationReason
) : IRequest<Unit>;

public class CancelSalesInvoiceCommandValidator : AbstractValidator<CancelSalesInvoiceCommand>
{
    public CancelSalesInvoiceCommandValidator()
    {
        RuleFor(x => x.SalesInvoiceId)
            .NotEmpty().WithMessage("El Id de la factura es requerido.");

        RuleFor(x => x.CancellationReason)
            .NotEmpty().WithMessage("El motivo de anulación es requerido.")
            .MaximumLength(500).WithMessage("El motivo de anulación no puede exceder los 500 caracteres.");
    }
}

public class CancelSalesInvoiceCommandHandler : IRequestHandler<CancelSalesInvoiceCommand, Unit>
{
    private readonly ISalesInvoiceRepository _salesInvoiceRepository;
    private readonly IProductRepository _productRepository;
    private readonly IInventoryRepository _inventoryRepository;
    private readonly IInventoryMovementRepository _movementRepository;
    private readonly ICashSessionRepository _cashSessionRepository;
    private readonly IPaymentMethodRepository _paymentMethodRepository;
    private readonly IAccountsReceivableRepository _arRepository;
    private readonly IRepository<BranchWarehouse> _branchWarehouseRepository;
    private readonly ICurrentUserService _currentUserService;
    private readonly IJournalEntryRepository _journalEntryRepository;
    private readonly IMediator _mediator;
    private readonly IUnitOfWork _unitOfWork;

    public CancelSalesInvoiceCommandHandler(
        ISalesInvoiceRepository salesInvoiceRepository,
        IProductRepository productRepository,
        IInventoryRepository inventoryRepository,
        IInventoryMovementRepository movementRepository,
        ICashSessionRepository cashSessionRepository,
        IPaymentMethodRepository paymentMethodRepository,
        IAccountsReceivableRepository arRepository,
        IRepository<BranchWarehouse> branchWarehouseRepository,
        ICurrentUserService currentUserService,
        IJournalEntryRepository journalEntryRepository,
        IMediator mediator,
        IUnitOfWork unitOfWork)
    {
        _salesInvoiceRepository = salesInvoiceRepository;
        _productRepository = productRepository;
        _inventoryRepository = inventoryRepository;
        _movementRepository = movementRepository;
        _cashSessionRepository = cashSessionRepository;
        _paymentMethodRepository = paymentMethodRepository;
        _arRepository = arRepository;
        _branchWarehouseRepository = branchWarehouseRepository;
        _currentUserService = currentUserService;
        _journalEntryRepository = journalEntryRepository;
        _mediator = mediator;
        _unitOfWork = unitOfWork;
    }

    public async Task<Unit> Handle(CancelSalesInvoiceCommand request, CancellationToken cancellationToken)
    {
        // 1. Obtener factura
        var invoice = await _salesInvoiceRepository.GetByIdWithDetailsAsync(request.SalesInvoiceId, cancellationToken);
        if (invoice == null)
            throw new ArgumentException($"La factura con Id '{request.SalesInvoiceId}' no existe.");

        if (invoice.Status == SalesInvoiceStatus.Cancelled)
            throw new InvalidOperationException("La factura ya se encuentra anulada.");

        if (invoice.Status != SalesInvoiceStatus.Posted)
            throw new InvalidOperationException($"Solo se pueden anular facturas confirmadas (Posted). Estado actual: {invoice.Status}.");

        // Validar que la CxC no tenga abonos si es crédito
        Domain.Entities.AccountsReceivable? ar = null;
        if (invoice.IsCreditSale)
        {
            ar = await _arRepository.GetByInvoiceIdAsync(invoice.Id, cancellationToken);
            if (ar != null && ar.PaidAmount > 0)
            {
                throw new InvalidOperationException($"No se puede anular la factura '{invoice.InvoiceNumber}' porque su cuenta por cobrar asociada ya registra abonos/pagos.");
            }
        }
        if (!invoice.IsCreditSale)
        {
            var currentUserId = Guid.Parse(_currentUserService.UserId ?? throw new InvalidOperationException("Usuario no autenticado."));
            var openSession = await _cashSessionRepository.GetOpenSessionByUserAsync(currentUserId, cancellationToken);
            if (openSession == null)
                throw new InvalidOperationException("Debe tener una sesión de caja abierta para registrar el reembolso por anulación de factura.");

            var cashPaymentMethod = await _paymentMethodRepository.GetByCodeAsync("EFEC", cancellationToken)
                ?? (await _paymentMethodRepository.FindAsync(p => p.IsCash && p.IsActive)).FirstOrDefault();

            if (cashPaymentMethod == null)
                throw new InvalidOperationException("No se encontró un método de pago en efectivo ('EFEC') activo configurado en el sistema.");

            // Crear movimiento de salida en caja (monto positivo)
            var reversalMovement = new CashMovement
            {
                Id = Guid.NewGuid(),
                CashSessionId = openSession.Id,
                MovementType = CashMovementType.CashOut, // Egreso/Salida
                PaymentMethodId = cashPaymentMethod.Id,
                ReferenceDocument = invoice.InvoiceNumber,
                ReferenceId = invoice.Id,
                Amount = invoice.TotalAmount, // Guardado como positivo
                Reason = "Anulación de Factura Contado",
                Notes = $"Reembolso por anulación de Factura Contado {invoice.InvoiceNumber}. Motivo: {request.CancellationReason}",
                CreatedAt = DateTime.UtcNow
            };

            openSession.CashMovements.Add(reversalMovement);
            _cashSessionRepository.Update(openSession);
        }

        // 3. Reversar inventario
        var movementNumber = await _movementRepository.GenerateMovementNumberAsync(cancellationToken);
        var movement = new InventoryMovement
        {
            Id = Guid.NewGuid(),
            MovementNumber = movementNumber,
            MovementType = MovementType.SaleReversal,
            FromBranchWarehouseId = null,
            ToBranchWarehouseId = invoice.BranchWarehouseId,
            ReferenceDocument = invoice.InvoiceNumber,
            Notes = $"Anulación de Factura {invoice.InvoiceNumber}. Motivo: {request.CancellationReason}",
            MovementDate = DateTime.UtcNow,
            CreatedBy = _currentUserService.UserId ?? "System",
            CreatedOnUtc = DateTime.UtcNow
        };

        bool requiresMovement = false;
        var affectedProducts = new Dictionary<Guid, Product>();

        foreach (var detail in invoice.Details)
        {
            var product = await _productRepository.GetByIdWithDetailsAsync(detail.ProductId, cancellationToken);
            if (product == null) continue;

            if (product.ProductType == ProductType.Service || !product.TrackInventory)
                continue;

            if (!affectedProducts.ContainsKey(product.Id))
            {
                affectedProducts.Add(product.Id, product);
            }

            var presentation = product.Presentations.FirstOrDefault(p => p.Id == detail.ProductPresentationId);
            if (presentation == null)
                throw new ArgumentException($"La presentación especificada no existe para el producto '{product.Name}'.");

            decimal conversionFactor = presentation.ConversionFactor;
            decimal quantityInBaseUnit = detail.Quantity * conversionFactor;

            var inventory = await _inventoryRepository.GetByWarehouseAndProductAsync(invoice.BranchWarehouseId, detail.ProductId, cancellationToken);
            if (inventory == null)
            {
                inventory = new Domain.Entities.Inventory
                {
                    Id = Guid.NewGuid(),
                    BranchWarehouseId = invoice.BranchWarehouseId,
                    ProductId = detail.ProductId,
                    PhysicalStock = quantityInBaseUnit,
                    ReservedStock = 0,
                    CommittedStock = 0,
                    CreatedBy = _currentUserService.UserId ?? "System",
                    CreatedOnUtc = DateTime.UtcNow
                };
                await _inventoryRepository.AddAsync(inventory);
            }
            else
            {
                inventory.PhysicalStock += quantityInBaseUnit;
                _inventoryRepository.Update(inventory);
            }

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

            requiresMovement = true;
        }

        if (requiresMovement)
        {
            await _movementRepository.AddAsync(movement);
        }

        // 3.5 AutoMarkSoldOut
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

        // 4. Cambiar estado de la factura
        invoice.Status = SalesInvoiceStatus.Cancelled;
        invoice.CancellationReason = request.CancellationReason;
        invoice.CancelledOnUtc = DateTime.UtcNow;
        invoice.LastModifiedBy = _currentUserService.UserId ?? "System";
        invoice.LastModifiedOnUtc = DateTime.UtcNow;

        _salesInvoiceRepository.Update(invoice);

        // Cancelar cuenta por cobrar asociada si existe
        if (ar != null)
        {
            ar.CurrentBalance = 0m;
            ar.Status = AccountsReceivableStatus.Cancelled;
            ar.Notes = $"Cuenta cancelada por anulación de factura. Motivo: {request.CancellationReason}";
            ar.LastModifiedBy = _currentUserService.UserId ?? "System";
            ar.LastModifiedOnUtc = DateTime.UtcNow;
            _arRepository.Update(ar);
        }

        // Reversar asiento contable asociado
        var originalJe = await _journalEntryRepository.GetByReferenceIdAsync(invoice.Id, cancellationToken);
        if (originalJe != null && originalJe.Status == JournalEntryStatus.Posted)
        {
            var reverseJeCmd = new ReverseJournalEntryCommand(originalJe.Id, $"Anulación de factura: {request.CancellationReason}");
            await _mediator.Send(reverseJeCmd, cancellationToken);
        }

        // Guardar cambios transaccionalmente
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return Unit.Value;
    }
}
