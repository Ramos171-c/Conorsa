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

public record TransferInventoryCommand(
    Guid FromBranchWarehouseId,
    Guid ToBranchWarehouseId,
    Guid ProductId,
    decimal Quantity,
    Guid ProductPresentationId,
    string? ReferenceDocument,
    string? Notes
) : IRequest<Guid>;

public class TransferInventoryCommandValidator : AbstractValidator<TransferInventoryCommand>
{
    public TransferInventoryCommandValidator()
    {
        RuleFor(x => x.FromBranchWarehouseId)
            .NotEmpty().WithMessage("La bodega origen es requerida.");

        RuleFor(x => x.ToBranchWarehouseId)
            .NotEmpty().WithMessage("La bodega destino es requerida.");

        RuleFor(x => x.FromBranchWarehouseId)
            .NotEqual(x => x.ToBranchWarehouseId).WithMessage("La bodega origen no puede ser igual a la bodega destino.");

        RuleFor(x => x.ProductId)
            .NotEmpty().WithMessage("El producto es requerido.");

        RuleFor(x => x.Quantity)
            .GreaterThan(0).WithMessage("La cantidad a transferir debe ser mayor a 0.");

        RuleFor(x => x.ProductPresentationId)
            .NotEmpty().WithMessage("La presentación del producto es requerida.");
    }
}

public class TransferInventoryCommandHandler : IRequestHandler<TransferInventoryCommand, Guid>
{
    private readonly IInventoryRepository _inventoryRepository;
    private readonly IInventoryMovementRepository _movementRepository;
    private readonly IProductRepository _productRepository;
    private readonly IRepository<BranchWarehouse> _branchWarehouseRepository;
    private readonly ICurrentUserService _currentUserService;
    private readonly IUnitOfWork _unitOfWork;

    public TransferInventoryCommandHandler(
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

    public async Task<Guid> Handle(TransferInventoryCommand request, CancellationToken cancellationToken)
    {
        // 1. Validar bodega origen
        var fromWarehouse = await _branchWarehouseRepository.GetByIdAsync(request.FromBranchWarehouseId);
        if (fromWarehouse == null)
            throw new ArgumentException("La bodega origen especificada no existe.");
        if (!fromWarehouse.IsActive)
            throw new InvalidOperationException("La bodega origen especificada no está activa.");

        // 2. Validar bodega destino
        var toWarehouse = await _branchWarehouseRepository.GetByIdAsync(request.ToBranchWarehouseId);
        if (toWarehouse == null)
            throw new ArgumentException("La bodega destino especificada no existe.");
        if (!toWarehouse.IsActive)
            throw new InvalidOperationException("La bodega destino especificada no está activa.");

        // 3. Validar producto
        var product = await _productRepository.GetByIdWithDetailsAsync(request.ProductId, cancellationToken);
        if (product == null)
            throw new ArgumentException("El producto especificado no existe.");
        if (!product.IsActive)
            throw new InvalidOperationException("El producto especificado no está activo.");
        if (product.ProductType == ProductType.Service)
            throw new InvalidOperationException("No se puede registrar movimientos de inventario para productos de tipo Servicio.");
        if (!product.TrackInventory)
            throw new InvalidOperationException("El producto especificado no maneja control de inventario.");

        // 4. Obtener presentación
        var presentation = product.Presentations.FirstOrDefault(p => p.Id == request.ProductPresentationId);
        if (presentation == null)
            throw new ArgumentException("La presentación especificada no existe para este producto.");
        if (!presentation.IsActive)
            throw new InvalidOperationException("La presentación especificada no está activa.");

        decimal conversionFactor = presentation.ConversionFactor;
        decimal quantityInBaseUnit = request.Quantity * conversionFactor;

        // 5. Validar y actualizar inventario origen
        var fromInventory = await _inventoryRepository.GetByWarehouseAndProductAsync(request.FromBranchWarehouseId, request.ProductId, cancellationToken);
        if (fromInventory == null)
        {
            if (!fromWarehouse.AllowNegativeInventory)
            {
                throw new InvalidOperationException($"Existencias insuficientes en la bodega de origen. Disponible: 0, Requerido: {quantityInBaseUnit}.");
            }
            fromInventory = new Domain.Entities.Inventory
            {
                Id = Guid.NewGuid(),
                BranchWarehouseId = request.FromBranchWarehouseId,
                ProductId = request.ProductId,
                PhysicalStock = 0,
                ReservedStock = 0,
                CommittedStock = 0,
                CreatedBy = _currentUserService.UserId ?? "System",
                CreatedOnUtc = DateTime.UtcNow
            };
            await _inventoryRepository.AddAsync(fromInventory);
        }
        else if (!fromWarehouse.AllowNegativeInventory && fromInventory.AvailableStock < quantityInBaseUnit)
        {
            throw new InvalidOperationException($"Existencias insuficientes en la bodega de origen. Disponible: {fromInventory.AvailableStock}, Requerido: {quantityInBaseUnit}.");
        }

        // 6. Obtener o crear inventario destino
        var toInventory = await _inventoryRepository.GetByWarehouseAndProductAsync(request.ToBranchWarehouseId, request.ProductId, cancellationToken);
        if (toInventory == null)
        {
            toInventory = new Domain.Entities.Inventory
            {
                Id = Guid.NewGuid(),
                BranchWarehouseId = request.ToBranchWarehouseId,
                ProductId = request.ProductId,
                PhysicalStock = 0,
                ReservedStock = 0,
                CommittedStock = 0,
                CreatedBy = _currentUserService.UserId ?? "System",
                CreatedOnUtc = DateTime.UtcNow
            };
            await _inventoryRepository.AddAsync(toInventory);
        }

        // Actualizar existencias
        fromInventory.PhysicalStock -= quantityInBaseUnit;
        toInventory.PhysicalStock += quantityInBaseUnit;

        _inventoryRepository.Update(fromInventory);
        _inventoryRepository.Update(toInventory);

        // 7. Crear movimiento de inventario (TransferOut representa la transacción)
        var movementNumber = await _movementRepository.GenerateMovementNumberAsync(cancellationToken);
        var movement = new InventoryMovement
        {
            Id = Guid.NewGuid(),
            MovementNumber = movementNumber,
            MovementType = MovementType.TransferOut,
            FromBranchWarehouseId = request.FromBranchWarehouseId,
            ToBranchWarehouseId = request.ToBranchWarehouseId,
            ReferenceDocument = request.ReferenceDocument,
            Notes = request.Notes,
            MovementDate = DateTime.UtcNow,
            CreatedBy = _currentUserService.UserId ?? "System",
            CreatedOnUtc = DateTime.UtcNow
        };

        movement.Details.Add(new InventoryMovementDetail
        {
            Id = Guid.NewGuid(),
            ProductId = request.ProductId,
            Quantity = request.Quantity,
            UnitOfMeasureId = presentation.UnitOfMeasureId,
            ProductPresentationId = presentation.Id,
            ConversionFactor = conversionFactor,
            QuantityInBaseUnit = quantityInBaseUnit,
            CreatedBy = _currentUserService.UserId ?? "System",
            CreatedOnUtc = DateTime.UtcNow
        });

        await _movementRepository.AddAsync(movement);

        // 8. AutoMarkSoldOut
        if (product.AutoMarkSoldOut)
        {
            var activeWarehouses = await _branchWarehouseRepository.FindAsync(bw => bw.IsActive);
            var activeWarehouseIds = activeWarehouses.Select(w => w.Id).ToList();
            var inventories = await _inventoryRepository.FindAsync(i => i.ProductId == product.Id);
            
            // Consolidar existencias considerando que ya fueron modificadas en memoria
            var otherWarehousesStock = inventories
                .Where(i => i.Id != fromInventory.Id && i.Id != toInventory.Id && activeWarehouseIds.Contains(i.BranchWarehouseId))
                .Sum(i => i.PhysicalStock);
            
            var totalPhysicalStock = otherWarehousesStock;
            if (activeWarehouseIds.Contains(fromInventory.BranchWarehouseId))
            {
                totalPhysicalStock += fromInventory.PhysicalStock;
            }
            if (activeWarehouseIds.Contains(toInventory.BranchWarehouseId))
            {
                totalPhysicalStock += toInventory.PhysicalStock;
            }

            var newIsSoldOut = totalPhysicalStock <= 0;
            if (product.IsSoldOut != newIsSoldOut)
            {
                product.IsSoldOut = newIsSoldOut;
                product.SoldOutAt = newIsSoldOut ? DateTime.UtcNow : null;
                product.SoldOutBy = newIsSoldOut ? (_currentUserService.UserId ?? "System") : null;
                _productRepository.Update(product);
            }
        }

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return movement.Id;
    }
}
