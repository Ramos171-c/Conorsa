using System;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using FluentValidation;
using EnterpriseBillingSystem.Domain.Enums;
using EnterpriseBillingSystem.Domain.Repositories;

namespace EnterpriseBillingSystem.Application.Sales.Commands;

public record RequestSalesOrderCancellationCommand(
    Guid SalesOrderId,
    string? Reason
) : IRequest<Unit>;

public class RequestSalesOrderCancellationCommandValidator : AbstractValidator<RequestSalesOrderCancellationCommand>
{
    public RequestSalesOrderCancellationCommandValidator()
    {
        RuleFor(x => x.SalesOrderId)
            .NotEmpty().WithMessage("El Id del pedido es requerido.");
        RuleFor(x => x.Reason)
            .NotEmpty().WithMessage("El motivo de la solicitud de anulación es requerido.");
    }
}

public class RequestSalesOrderCancellationCommandHandler : IRequestHandler<RequestSalesOrderCancellationCommand, Unit>
{
    private readonly ISalesOrderRepository _salesOrderRepository;
    private readonly IUnitOfWork _unitOfWork;

    public RequestSalesOrderCancellationCommandHandler(
        ISalesOrderRepository salesOrderRepository,
        IUnitOfWork unitOfWork)
    {
        _salesOrderRepository = salesOrderRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<Unit> Handle(RequestSalesOrderCancellationCommand request, CancellationToken cancellationToken)
    {
        var order = await _salesOrderRepository.GetByIdWithDetailsAsync(request.SalesOrderId, cancellationToken);
        if (order == null)
            throw new ArgumentException($"El pedido con Id '{request.SalesOrderId}' no existe.");

        if (order.Status == SalesOrderStatus.Anulado)
            throw new InvalidOperationException("El pedido ya está anulado.");

        if (order.Status == SalesOrderStatus.SolicitudAnulacion)
            throw new InvalidOperationException("Ya existe una solicitud de anulación pendiente para este pedido.");

        if (order.Status != SalesOrderStatus.Recibido && order.Status != SalesOrderStatus.EnProceso)
            throw new InvalidOperationException("Solo se puede solicitar la anulación de pedidos en estado Recibido o En Proceso.");

        order.Status = SalesOrderStatus.SolicitudAnulacion;
        order.Notes = string.IsNullOrEmpty(request.Reason)
            ? order.Notes
            : $"{order.Notes}\n[SOLICITUD ANULACIÓN]: {request.Reason}";
        
        _salesOrderRepository.Update(order);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return Unit.Value;
    }
}
