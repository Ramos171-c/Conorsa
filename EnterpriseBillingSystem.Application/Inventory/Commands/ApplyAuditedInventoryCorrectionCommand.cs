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

namespace EnterpriseBillingSystem.Application.Inventory.Commands;

public record PhysicalCountCorrectionItemInput(
    Guid ProductId,
    decimal PhysicalCountBaseUnits,
    string? Notes = null
);

public record ApplyAuditedInventoryCorrectionCommand(
    Guid BranchWarehouseId,
    List<PhysicalCountCorrectionItemInput> Items,
    string AuditReason
) : IRequest<int>;

public class ApplyAuditedInventoryCorrectionCommandValidator : AbstractValidator<ApplyAuditedInventoryCorrectionCommand>
{
    public ApplyAuditedInventoryCorrectionCommandValidator()
    {
        RuleFor(x => x.BranchWarehouseId).NotEmpty().WithMessage("La bodega es requerida.");
        RuleFor(x => x.AuditReason).NotEmpty().WithMessage("El motivo de auditoría es requerido.");
        RuleFor(x => x.Items).NotEmpty().WithMessage("Debe incluir al menos un producto a ajustar.");
    }
}

public class ApplyAuditedInventoryCorrectionCommandHandler : IRequestHandler<ApplyAuditedInventoryCorrectionCommand, int>
{
    private readonly IInventoryRepository _inventoryRepository;
    private readonly IInventoryMovementRepository _movementRepository;
    private readonly IProductRepository _productRepository;
    private readonly IRepository<BranchWarehouse> _branchWarehouseRepository;
    private readonly ICurrentUserService _currentUserService;
    private readonly IUnitOfWork _unitOfWork;

    public ApplyAuditedInventoryCorrectionCommandHandler(
        IInventoryRepository inventoryRepository,
        IInventoryMovementRepository movementRepository,
        IProductRepository productRepository,
        IRepository<BranchWarehouse> branchWarehouseRepository,
        ICurrentUserService currentUserService,
        IUnitOfWork unitOfWork)
    {
        _inventoryRepository = inventoryRepository;
        _movementRepository = movementRepository;
        _productRepository = productRepository;
        _branchWarehouseRepository = branchWarehouseRepository;
        _currentUserService = currentUserService;
        _unitOfWork = unitOfWork;
    }

    public async Task<int> Handle(ApplyAuditedInventoryCorrectionCommand request, CancellationToken cancellationToken)
    {
        var warehouse = await _branchWarehouseRepository.GetByIdAsync(request.BranchWarehouseId);
        if (warehouse == null)
            throw new ArgumentException("La bodega especificada no existe.");

        int adjustedCount = 0;

        foreach (var item in request.Items)
        {
            var product = await _productRepository.GetByIdWithDetailsAsync(item.ProductId, cancellationToken);
            if (product == null || product.IsDeleted || product.ProductType == ProductType.Service || !product.TrackInventory)
                continue;

            var inventory = await _inventoryRepository.GetByWarehouseAndProductAsync(request.BranchWarehouseId, item.ProductId, cancellationToken);
            decimal previousStock = inventory?.PhysicalStock ?? 0m;
            decimal targetStock = Math.Round(item.PhysicalCountBaseUnits, 4);

            decimal diff = targetStock - previousStock;
            if (Math.Abs(diff) <= 0.0001m)
            {
                // Stock already matches
                continue;
            }

            // Get default base presentation
            var presentation = product.Presentations?.FirstOrDefault(p => p.IsBaseUnit && !p.IsDeleted)
                ?? product.Presentations?.FirstOrDefault(p => !p.IsDeleted);

            decimal conversionFactor = presentation?.ConversionFactor > 0 ? presentation.ConversionFactor : 1.0000m;
            Guid presentationId = presentation?.Id ?? Guid.Empty;
            Guid uomId = presentation?.UnitOfMeasureId ?? product.DefaultUnitOfMeasureId;

            MovementType mType = diff > 0 ? MovementType.PositiveAdjustment : MovementType.NegativeAdjustment;

            var movementNumber = await _movementRepository.GenerateMovementNumberAsync(cancellationToken);
            var movement = new InventoryMovement
            {
                Id = Guid.NewGuid(),
                BranchId = warehouse.BranchId,
                MovementNumber = movementNumber,
                MovementType = mType,
                FromBranchWarehouseId = diff < 0 ? request.BranchWarehouseId : null,
                ToBranchWarehouseId = diff > 0 ? request.BranchWarehouseId : null,
                ReferenceDocument = "AUDIT-CORRECTION",
                Notes = $"Ajuste auditado: Stock anterior = {previousStock}, Stock físico real = {targetStock}. Motivo: {request.AuditReason}. {item.Notes}",
                MovementDate = DateTime.UtcNow,
                CreatedBy = _currentUserService.UserId ?? "System",
                CreatedOnUtc = DateTime.UtcNow
            };

            decimal qtyInPresentation = conversionFactor > 0 ? Math.Abs(diff) / conversionFactor : Math.Abs(diff);

            movement.Details.Add(new InventoryMovementDetail
            {
                Id = Guid.NewGuid(),
                InventoryMovementId = movement.Id,
                BranchId = warehouse.BranchId,
                ProductId = item.ProductId,
                Quantity = Math.Round(qtyInPresentation, 4),
                UnitOfMeasureId = uomId,
                ProductPresentationId = presentationId,
                ConversionFactor = conversionFactor,
                QuantityInBaseUnit = Math.Abs(diff),
                CreatedBy = _currentUserService.UserId ?? "System",
                CreatedOnUtc = DateTime.UtcNow
            });

            if (inventory == null)
            {
                inventory = new Domain.Entities.Inventory
                {
                    Id = Guid.NewGuid(),
                    BranchWarehouseId = request.BranchWarehouseId,
                    ProductId = item.ProductId,
                    PhysicalStock = targetStock,
                    ReservedStock = 0,
                    CommittedStock = 0,
                    CreatedBy = _currentUserService.UserId ?? "System",
                    CreatedOnUtc = DateTime.UtcNow
                };
                await _inventoryRepository.AddAsync(inventory);
            }
            else
            {
                inventory.PhysicalStock = targetStock;
                inventory.LastModifiedBy = _currentUserService.UserId ?? "System";
                inventory.LastModifiedOnUtc = DateTime.UtcNow;
                _inventoryRepository.Update(inventory);
            }

            await _movementRepository.AddAsync(movement);
            adjustedCount++;
        }

        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return adjustedCount;
    }
}
