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

public record BulkInventoryAdjustmentItem(
    Guid ProductId,
    decimal NewPhysicalStock
);

public record BulkAdjustInventoryCommand(
    Guid BranchWarehouseId,
    List<BulkInventoryAdjustmentItem> Items,
    string? Notes = null
) : IRequest<int>;

public class BulkAdjustInventoryCommandValidator : AbstractValidator<BulkAdjustInventoryCommand>
{
    public BulkAdjustInventoryCommandValidator()
    {
        RuleFor(x => x.BranchWarehouseId)
            .NotEmpty().WithMessage("La bodega es requerida.");

        RuleFor(x => x.Items)
            .NotEmpty().WithMessage("Debe incluir al menos un producto para ajustar.");
    }
}

public class BulkAdjustInventoryCommandHandler : IRequestHandler<BulkAdjustInventoryCommand, int>
{
    private readonly IInventoryRepository _inventoryRepository;
    private readonly IInventoryMovementRepository _movementRepository;
    private readonly IProductRepository _productRepository;
    private readonly IRepository<BranchWarehouse> _branchWarehouseRepository;
    private readonly ICurrentUserService _currentUserService;
    private readonly IUnitOfWork _unitOfWork;

    public BulkAdjustInventoryCommandHandler(
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

    public async Task<int> Handle(BulkAdjustInventoryCommand request, CancellationToken cancellationToken)
    {
        var branchWarehouse = await _branchWarehouseRepository.GetByIdAsync(request.BranchWarehouseId);
        if (branchWarehouse == null)
            throw new ArgumentException("La bodega especificada no existe.");

        int updatedCount = 0;
        var movementNumber = await _movementRepository.GenerateMovementNumberAsync(cancellationToken);
        
        foreach (var item in request.Items)
        {
            if (item.NewPhysicalStock < 0) continue;

            var product = await _productRepository.GetByIdWithDetailsAsync(item.ProductId, cancellationToken);
            if (product == null || !product.TrackInventory || product.ProductType == ProductType.Service) continue;

            var inventory = await _inventoryRepository.GetByWarehouseAndProductAsync(request.BranchWarehouseId, item.ProductId, cancellationToken);
            
            decimal currentStock = inventory?.PhysicalStock ?? 0m;
            decimal diff = item.NewPhysicalStock - currentStock;

            if (diff == 0 && inventory != null) continue;

            var presentation = product.Presentations.FirstOrDefault(p => p.IsDefaultSalePresentation)
                ?? product.Presentations.FirstOrDefault();

            var uomId = presentation?.UnitOfMeasureId ?? product.DefaultUnitOfMeasureId;

            if (inventory == null)
            {
                inventory = new Domain.Entities.Inventory
                {
                    Id = Guid.NewGuid(),
                    BranchWarehouseId = request.BranchWarehouseId,
                    ProductId = item.ProductId,
                    PhysicalStock = item.NewPhysicalStock,
                    ReservedStock = 0,
                    CommittedStock = 0,
                    CreatedBy = _currentUserService.UserId ?? "System",
                    CreatedOnUtc = DateTime.UtcNow
                };
                await _inventoryRepository.AddAsync(inventory);
            }
            else
            {
                inventory.PhysicalStock = item.NewPhysicalStock;
                inventory.LastModifiedBy = _currentUserService.UserId ?? "System";
                inventory.LastModifiedOnUtc = DateTime.UtcNow;
                _inventoryRepository.Update(inventory);
            }

            var itemMovement = new InventoryMovement
            {
                Id = Guid.NewGuid(),
                MovementNumber = $"{movementNumber}-{updatedCount + 1}",
                MovementType = diff >= 0 ? MovementType.PositiveAdjustment : MovementType.NegativeAdjustment,
                FromBranchWarehouseId = request.BranchWarehouseId,
                ToBranchWarehouseId = null,
                ReferenceDocument = "Saneo Masivo de Inventario",
                Notes = request.Notes ?? "Ajuste e inventario físico masivo directo",
                MovementDate = DateTime.UtcNow,
                CreatedBy = _currentUserService.UserId ?? "System",
                CreatedOnUtc = DateTime.UtcNow
            };

            itemMovement.Details.Add(new InventoryMovementDetail
            {
                Id = Guid.NewGuid(),
                InventoryMovementId = itemMovement.Id,
                ProductId = item.ProductId,
                UnitOfMeasureId = uomId,
                ProductPresentationId = presentation?.Id ?? Guid.Empty,
                Quantity = Math.Abs(diff),
                ConversionFactor = 1.0000m,
                QuantityInBaseUnit = Math.Abs(diff)
            });

            await _movementRepository.AddAsync(itemMovement);
            updatedCount++;
        }

        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return updatedCount;
    }
}
