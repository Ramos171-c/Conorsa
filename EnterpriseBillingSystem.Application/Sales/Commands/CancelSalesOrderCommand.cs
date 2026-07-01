using System;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using FluentValidation;
using EnterpriseBillingSystem.Domain.Enums;
using EnterpriseBillingSystem.Domain.Repositories;

namespace EnterpriseBillingSystem.Application.Sales.Commands;

// ─── Command ──────────────────────────────────────────────────────────────────

public record CancelSalesOrderCommand(
    Guid SalesOrderId,
    string? CancellationReason
) : IRequest<Unit>;

// ─── Validator ────────────────────────────────────────────────────────────────

public class CancelSalesOrderCommandValidator : AbstractValidator<CancelSalesOrderCommand>
{
    public CancelSalesOrderCommandValidator()
    {
        RuleFor(x => x.SalesOrderId)
            .NotEmpty().WithMessage("El Id del pedido es requerido.");
    }
}

// ─── Handler ──────────────────────────────────────────────────────────────────

public class CancelSalesOrderCommandHandler : IRequestHandler<CancelSalesOrderCommand, Unit>
{
    private readonly ISalesOrderRepository _salesOrderRepository;
    private readonly IUnitOfWork _unitOfWork;

    public CancelSalesOrderCommandHandler(
        ISalesOrderRepository salesOrderRepository,
        IUnitOfWork unitOfWork)
    {
        _salesOrderRepository = salesOrderRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<Unit> Handle(CancelSalesOrderCommand request, CancellationToken cancellationToken)
    {
        var order = await _salesOrderRepository.GetByIdWithDetailsAsync(request.SalesOrderId, cancellationToken);
        if (order == null)
            throw new ArgumentException($"El pedido con Id '{request.SalesOrderId}' no existe.");

        if (order.Status == SalesOrderStatus.Anulado)
            throw new InvalidOperationException("El pedido ya está anulado.");

        // Verificar que no tenga facturas confirmadas
        bool hasPostedInvoices = order.SalesInvoices.Any(si => si.Status == SalesInvoiceStatus.Posted);
        if (hasPostedInvoices)
            throw new InvalidOperationException("No se puede anular un pedido con facturas confirmadas asociadas.");

        order.Status = SalesOrderStatus.Anulado;
        order.Notes = string.IsNullOrEmpty(request.CancellationReason)
            ? order.Notes
            : $"{order.Notes}\n[ANULACIÓN]: {request.CancellationReason}";
        order.LastModifiedBy = "System";
        order.LastModifiedOnUtc = DateTime.UtcNow;

        _salesOrderRepository.Update(order);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return Unit.Value;
    }
}
