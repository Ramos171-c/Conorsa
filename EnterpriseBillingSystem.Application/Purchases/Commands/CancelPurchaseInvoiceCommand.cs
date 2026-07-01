using System;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using FluentValidation;
using EnterpriseBillingSystem.Domain.Entities;
using EnterpriseBillingSystem.Domain.Enums;
using EnterpriseBillingSystem.Domain.Repositories;
using EnterpriseBillingSystem.Application.Common.Interfaces;
using EnterpriseBillingSystem.Application.JournalEntries.Commands;

namespace EnterpriseBillingSystem.Application.Purchases.Commands;

public record CancelPurchaseInvoiceCommand(
    Guid PurchaseInvoiceId,
    string CancellationReason
) : IRequest<Unit>;

public class CancelPurchaseInvoiceCommandValidator : AbstractValidator<CancelPurchaseInvoiceCommand>
{
    public CancelPurchaseInvoiceCommandValidator()
    {
        RuleFor(x => x.PurchaseInvoiceId)
            .NotEmpty().WithMessage("El Id de la factura de compra es requerido.");

        RuleFor(x => x.CancellationReason)
            .NotEmpty().WithMessage("El motivo de anulación es requerido.")
            .MaximumLength(500).WithMessage("El motivo de anulación no puede exceder los 500 caracteres.");
    }
}

public class CancelPurchaseInvoiceCommandHandler : IRequestHandler<CancelPurchaseInvoiceCommand, Unit>
{
    private readonly IPurchaseInvoiceRepository _purchaseInvoiceRepository;
    private readonly IAccountsPayableRepository _apRepository;
    private readonly ICurrentUserService _currentUserService;
    private readonly IJournalEntryRepository _journalEntryRepository;
    private readonly IMediator _mediator;
    private readonly IUnitOfWork _unitOfWork;

    public CancelPurchaseInvoiceCommandHandler(
        IPurchaseInvoiceRepository purchaseInvoiceRepository,
        IAccountsPayableRepository apRepository,
        ICurrentUserService currentUserService,
        IJournalEntryRepository journalEntryRepository,
        IMediator mediator,
        IUnitOfWork unitOfWork)
    {
        _purchaseInvoiceRepository = purchaseInvoiceRepository;
        _apRepository = apRepository;
        _currentUserService = currentUserService;
        _journalEntryRepository = journalEntryRepository;
        _mediator = mediator;
        _unitOfWork = unitOfWork;
    }

    public async Task<Unit> Handle(CancelPurchaseInvoiceCommand request, CancellationToken cancellationToken)
    {
        // 1. Obtener factura
        var invoice = await _purchaseInvoiceRepository.GetByIdWithDetailsAsync(request.PurchaseInvoiceId, cancellationToken);
        if (invoice == null)
            throw new ArgumentException($"La factura de compra con Id '{request.PurchaseInvoiceId}' no existe.");

        if (invoice.Status == PurchaseInvoiceStatus.Cancelled)
            throw new InvalidOperationException("La factura de compra ya se encuentra anulada.");

        if (invoice.Status != PurchaseInvoiceStatus.Posted)
            throw new InvalidOperationException($"Solo se pueden anular facturas contabilizadas (Posted). Estado actual: {invoice.Status}.");

        // 2. Validar que la CxP asociada no tenga abonos
        Domain.Entities.AccountsPayable? ap = await _apRepository.GetByInvoiceIdAsync(invoice.Id, cancellationToken);
        if (ap != null)
        {
            if (ap.PaidAmount > 0)
            {
                throw new InvalidOperationException("No es posible anular una factura de compra con pagos registrados.");
            }

            // Actualizar cuenta por pagar a anulada
            ap.CurrentBalance = 0m;
            ap.Status = AccountsPayableStatus.Cancelled;
            ap.Notes = $"Cuenta cancelada por anulación de factura. Motivo: {request.CancellationReason}";
            ap.LastModifiedBy = _currentUserService.UserId ?? "System";
            ap.LastModifiedOnUtc = DateTime.UtcNow;

            _apRepository.Update(ap);
        }

        // 3. Cambiar estado de la factura de compra
        invoice.Status = PurchaseInvoiceStatus.Cancelled;
        invoice.Notes = $"Factura anulada. Motivo: {request.CancellationReason}. " + invoice.Notes;
        invoice.LastModifiedBy = _currentUserService.UserId ?? "System";
        invoice.LastModifiedOnUtc = DateTime.UtcNow;

        _purchaseInvoiceRepository.Update(invoice);

        // Reversar asiento contable asociado
        var originalJe = await _journalEntryRepository.GetByReferenceIdAsync(invoice.Id, cancellationToken);
        if (originalJe != null && originalJe.Status == JournalEntryStatus.Posted)
        {
            var reverseJeCmd = new ReverseJournalEntryCommand(originalJe.Id, $"Anulación de compra: {request.CancellationReason}");
            await _mediator.Send(reverseJeCmd, cancellationToken);
        }

        // Guardar cambios transaccionalmente
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return Unit.Value;
    }
}
