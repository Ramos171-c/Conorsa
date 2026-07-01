using System;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using FluentValidation;
using EnterpriseBillingSystem.Domain.Enums;
using EnterpriseBillingSystem.Domain.Repositories;

namespace EnterpriseBillingSystem.Application.Sales.Commands;

// ─── Command ──────────────────────────────────────────────────────────────────

public record ConfirmSalesOrderCommand(Guid SalesOrderId) : IRequest<Unit>;

// ─── Validator ────────────────────────────────────────────────────────────────

public class ConfirmSalesOrderCommandValidator : AbstractValidator<ConfirmSalesOrderCommand>
{
    public ConfirmSalesOrderCommandValidator()
    {
        RuleFor(x => x.SalesOrderId)
            .NotEmpty().WithMessage("El Id del pedido es requerido.");
    }
}

// ─── Handler ──────────────────────────────────────────────────────────────────

public class ConfirmSalesOrderCommandHandler : IRequestHandler<ConfirmSalesOrderCommand, Unit>
{
    private readonly ISalesOrderRepository _salesOrderRepository;
    private readonly IInventoryRepository _inventoryRepository;
    private readonly IProductRepository _productRepository;
    private readonly IRepository<Domain.Entities.BranchWarehouse> _branchWarehouseRepository;
    private readonly IUnitOfWork _unitOfWork;

    public ConfirmSalesOrderCommandHandler(
        ISalesOrderRepository salesOrderRepository,
        IInventoryRepository inventoryRepository,
        IProductRepository productRepository,
        IRepository<Domain.Entities.BranchWarehouse> branchWarehouseRepository,
        IUnitOfWork unitOfWork)
    {
        _salesOrderRepository = salesOrderRepository;
        _inventoryRepository = inventoryRepository;
        _productRepository = productRepository;
        _branchWarehouseRepository = branchWarehouseRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<Unit> Handle(ConfirmSalesOrderCommand request, CancellationToken cancellationToken)
    {
        var order = await _salesOrderRepository.GetByIdWithDetailsAsync(request.SalesOrderId, cancellationToken);
        if (order == null)
            throw new ArgumentException($"El pedido con Id '{request.SalesOrderId}' no existe.");

        if (order.Status != SalesOrderStatus.Recibido)
            throw new InvalidOperationException($"Solo se pueden procesar pedidos en estado Recibido. Estado actual: {order.Status}.");

        if (!order.Details.Any())
            throw new InvalidOperationException("No se puede confirmar un pedido sin detalles.");

        // Nota: En esta fase NO se generan reservas de inventario.
        // La validación de stock ocurre al confirmar la SalesInvoice (PostSalesInvoiceCommand).
        // Las reservas (InventoryReservation) quedan disponibles para una fase futura.

        order.Status = SalesOrderStatus.EnProceso;
        order.LastModifiedBy = "System";
        order.LastModifiedOnUtc = DateTime.UtcNow;

        _salesOrderRepository.Update(order);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return Unit.Value;
    }
}
