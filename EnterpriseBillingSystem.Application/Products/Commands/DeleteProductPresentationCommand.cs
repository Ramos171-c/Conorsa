using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using EnterpriseBillingSystem.Domain.Entities;
using EnterpriseBillingSystem.Domain.Repositories;

namespace EnterpriseBillingSystem.Application.Products.Commands;

public record DeleteProductPresentationCommand(Guid Id) : IRequest<bool>;

public class DeleteProductPresentationCommandHandler : IRequestHandler<DeleteProductPresentationCommand, bool>
{
    private readonly IProductPresentationRepository _presentationRepository;
    private readonly IRepository<SalesInvoiceDetail> _salesDetailRepository;
    private readonly IRepository<PurchaseInvoiceDetail> _purchaseDetailRepository;
    private readonly IRepository<InventoryMovementDetail> _movementDetailRepository;
    private readonly IUnitOfWork _unitOfWork;

    public DeleteProductPresentationCommandHandler(
        IProductPresentationRepository presentationRepository,
        IRepository<SalesInvoiceDetail> salesDetailRepository,
        IRepository<PurchaseInvoiceDetail> purchaseDetailRepository,
        IRepository<InventoryMovementDetail> movementDetailRepository,
        IUnitOfWork unitOfWork)
    {
        _presentationRepository = presentationRepository;
        _salesDetailRepository = salesDetailRepository;
        _purchaseDetailRepository = purchaseDetailRepository;
        _movementDetailRepository = movementDetailRepository;
        _unitOfWork = unitOfWork;
    }

    private async Task<bool> PresentationHasMovementsAsync(Guid presentationId)
    {
        var sales = await _salesDetailRepository.FindAsync(d => d.ProductPresentationId == presentationId);
        if (sales.Any()) return true;

        var purchases = await _purchaseDetailRepository.FindAsync(d => d.ProductPresentationId == presentationId);
        if (purchases.Any()) return true;

        var movements = await _movementDetailRepository.FindAsync(d => d.ProductPresentationId == presentationId);
        if (movements.Any()) return true;

        return false;
    }

    public async Task<bool> Handle(DeleteProductPresentationCommand request, CancellationToken cancellationToken)
    {
        var existing = await _presentationRepository.GetByIdAsync(request.Id, cancellationToken);
        if (existing == null) return false;

        // 1. Impedir eliminar la presentación base
        if (existing.IsBaseUnit)
        {
            throw new InvalidOperationException("No se permite eliminar la presentación base del producto.");
        }

        // 2. Impedir eliminar si posee movimientos históricos
        var hasMovements = await PresentationHasMovementsAsync(existing.Id);
        if (hasMovements)
        {
            throw new InvalidOperationException($"No se permite eliminar la presentación '{existing.Name}' porque ya registra movimientos de inventario históricos. Debe quedar como inactiva (IsActive = false).");
        }

        existing.IsActive = false;
        existing.IsDeleted = true; // Soft delete

        _presentationRepository.Update(existing);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return true;
    }
}
