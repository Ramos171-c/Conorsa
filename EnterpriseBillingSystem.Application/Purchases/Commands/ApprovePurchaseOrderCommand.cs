using System;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using FluentValidation;
using EnterpriseBillingSystem.Domain.Enums;
using EnterpriseBillingSystem.Domain.Repositories;

namespace EnterpriseBillingSystem.Application.Purchases.Commands;

// ─── Command ─────────────────────────────────────────────────────────────────

public record ApprovePurchaseOrderCommand(Guid PurchaseOrderId) : IRequest<Unit>;

// ─── Validator ────────────────────────────────────────────────────────────────

public class ApprovePurchaseOrderCommandValidator : AbstractValidator<ApprovePurchaseOrderCommand>
{
    public ApprovePurchaseOrderCommandValidator()
    {
        RuleFor(x => x.PurchaseOrderId)
            .NotEmpty().WithMessage("El Id de la orden de compra es requerido.");
    }
}

// ─── Handler ─────────────────────────────────────────────────────────────────

public class ApprovePurchaseOrderCommandHandler : IRequestHandler<ApprovePurchaseOrderCommand, Unit>
{
    private readonly IPurchaseOrderRepository _purchaseOrderRepository;
    private readonly IUnitOfWork _unitOfWork;

    public ApprovePurchaseOrderCommandHandler(
        IPurchaseOrderRepository purchaseOrderRepository,
        IUnitOfWork unitOfWork)
    {
        _purchaseOrderRepository = purchaseOrderRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<Unit> Handle(ApprovePurchaseOrderCommand request, CancellationToken cancellationToken)
    {
        // 1. Obtener la orden con sus detalles
        var order = await _purchaseOrderRepository.GetByIdWithDetailsAsync(request.PurchaseOrderId, cancellationToken);
        if (order == null)
            throw new ArgumentException($"La orden de compra con Id '{request.PurchaseOrderId}' no existe.");

        // 2. Validar estado actual
        if (order.Status != PurchaseOrderStatus.Draft)
            throw new InvalidOperationException($"Solo se pueden aprobar órdenes en estado 'Borrador'. Estado actual: {order.Status}.");

        if (!order.Details.Any())
            throw new InvalidOperationException("No se puede aprobar una orden de compra sin detalles.");

        // 3. Cambiar estado
        order.Status = PurchaseOrderStatus.Approved;
        order.LastModifiedBy = "System";
        order.LastModifiedOnUtc = DateTime.UtcNow;

        _purchaseOrderRepository.Update(order);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return Unit.Value;
    }
}
