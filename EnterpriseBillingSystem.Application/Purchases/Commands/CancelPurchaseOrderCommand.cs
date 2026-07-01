using System;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using FluentValidation;
using EnterpriseBillingSystem.Domain.Enums;
using EnterpriseBillingSystem.Domain.Repositories;

namespace EnterpriseBillingSystem.Application.Purchases.Commands;

// ─── Command ─────────────────────────────────────────────────────────────────

public record CancelPurchaseOrderCommand(
    Guid PurchaseOrderId,
    string? CancellationReason
) : IRequest<Unit>;

// ─── Validator ────────────────────────────────────────────────────────────────

public class CancelPurchaseOrderCommandValidator : AbstractValidator<CancelPurchaseOrderCommand>
{
    public CancelPurchaseOrderCommandValidator()
    {
        RuleFor(x => x.PurchaseOrderId)
            .NotEmpty().WithMessage("El Id de la orden de compra es requerido.");
    }
}

// ─── Handler ─────────────────────────────────────────────────────────────────

public class CancelPurchaseOrderCommandHandler : IRequestHandler<CancelPurchaseOrderCommand, Unit>
{
    private readonly IPurchaseOrderRepository _purchaseOrderRepository;
    private readonly IUnitOfWork _unitOfWork;

    public CancelPurchaseOrderCommandHandler(
        IPurchaseOrderRepository purchaseOrderRepository,
        IUnitOfWork unitOfWork)
    {
        _purchaseOrderRepository = purchaseOrderRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<Unit> Handle(CancelPurchaseOrderCommand request, CancellationToken cancellationToken)
    {
        var order = await _purchaseOrderRepository.GetByIdWithDetailsAsync(request.PurchaseOrderId, cancellationToken);
        if (order == null)
            throw new ArgumentException($"La orden de compra con Id '{request.PurchaseOrderId}' no existe.");

        // Solo se pueden anular órdenes en estado Draft o Approved (sin recepciones parciales)
        if (order.Status == PurchaseOrderStatus.Completed || order.Status == PurchaseOrderStatus.Cancelled)
            throw new InvalidOperationException($"No se puede anular una orden en estado '{order.Status}'.");

        if (order.Status == PurchaseOrderStatus.PartiallyReceived)
            throw new InvalidOperationException("No se puede anular una orden con recepciones parciales registradas. Primero revierta las recepciones correspondientes.");

        order.Status = PurchaseOrderStatus.Cancelled;
        order.Notes = string.IsNullOrEmpty(request.CancellationReason)
            ? order.Notes
            : $"{order.Notes}\n[ANULACIÓN]: {request.CancellationReason}";
        order.LastModifiedBy = "System";
        order.LastModifiedOnUtc = DateTime.UtcNow;

        _purchaseOrderRepository.Update(order);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return Unit.Value;
    }
}
