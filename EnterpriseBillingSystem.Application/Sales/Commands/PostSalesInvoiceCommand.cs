using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using FluentValidation;
using Microsoft.Extensions.Logging;
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
    private readonly ICashSessionRepository _cashSessionRepository;
    private readonly IPaymentMethodRepository _paymentMethodRepository;
    private readonly IAccountsReceivableRepository _arRepository;
    private readonly ICurrentUserService _currentUserService;
    private readonly IMediator _mediator;
    private readonly ILogger<PostSalesInvoiceCommandHandler> _logger;
    private readonly IUnitOfWork _unitOfWork;

    public PostSalesInvoiceCommandHandler(
        ISalesInvoiceRepository salesInvoiceRepository,
        ICustomerRepository customerRepository,
        ICashSessionRepository cashSessionRepository,
        IPaymentMethodRepository paymentMethodRepository,
        IAccountsReceivableRepository arRepository,
        ICurrentUserService currentUserService,
        IMediator mediator,
        ILogger<PostSalesInvoiceCommandHandler> logger,
        IUnitOfWork unitOfWork)
    {
        _salesInvoiceRepository = salesInvoiceRepository;
        _customerRepository = customerRepository;
        _cashSessionRepository = cashSessionRepository;
        _paymentMethodRepository = paymentMethodRepository;
        _arRepository = arRepository;
        _currentUserService = currentUserService;
        _mediator = mediator;
        _logger = logger;
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

        // 4. Registrar cobro en caja (si es contado)
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

        // 6.7 Generar Asiento Contable Automático (opcional — no bloquea la venta si faltan cuentas)
        try
        {
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

            // El costo de ventas ya no se calcula desde facturas (inventario manual).
            // Se omite el asiento contable de costo.

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
        }
        catch (Exception jeEx)
        {
            // El asiento contable es informativo; si falta alguna cuenta en el catálogo
            // no debemos bloquear la confirmación de la factura.
            _logger.LogWarning(jeEx, "No se pudo generar el asiento contable automático para la factura {InvoiceNumber}. Verifique el catálogo de cuentas contables.", invoice.InvoiceNumber);
        }

        // Guardar todo en una transacción única
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return Unit.Value;
    }
}
