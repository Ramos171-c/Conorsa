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
using EnterpriseBillingSystem.Application.JournalEntries.Commands;
using EnterpriseBillingSystem.Application.Common.Interfaces;

namespace EnterpriseBillingSystem.Application.Inventory.Commands;

public record AdjustInventoryCommand(
    Guid BranchWarehouseId,
    Guid ProductId,
    decimal Quantity,
    bool IsPositive,
    Guid ProductPresentationId,
    string? ReferenceDocument,
    string? Notes
) : IRequest<Guid>;

public class AdjustInventoryCommandValidator : AbstractValidator<AdjustInventoryCommand>
{
    public AdjustInventoryCommandValidator()
    {
        RuleFor(x => x.BranchWarehouseId)
            .NotEmpty().WithMessage("La bodega de sucursal es requerida.");

        RuleFor(x => x.ProductId)
            .NotEmpty().WithMessage("El producto es requerido.");

        RuleFor(x => x.Quantity)
            .GreaterThan(0).WithMessage("La cantidad a ajustar debe ser mayor a 0.");

        RuleFor(x => x.ProductPresentationId)
            .NotEmpty().WithMessage("La presentación del producto es requerida.");
    }
}

public class AdjustInventoryCommandHandler : IRequestHandler<AdjustInventoryCommand, Guid>
{
    private readonly IInventoryRepository _inventoryRepository;
    private readonly IInventoryMovementRepository _movementRepository;
    private readonly IProductRepository _productRepository;
    private readonly IRepository<BranchWarehouse> _branchWarehouseRepository;
    private readonly ICurrentUserService _currentUserService;
    private readonly IMediator _mediator;
    private readonly IUnitOfWork _unitOfWork;

    public AdjustInventoryCommandHandler(
        IInventoryRepository inventoryRepository,
        IInventoryMovementRepository movementRepository,
        IProductRepository productRepository,
        IRepository<BranchWarehouse> branchWarehouseRepository,
        ICurrentUserService currentUserService,
        IMediator mediator,
        IUnitOfWork unitOfWork)
    {
        _inventoryRepository = inventoryRepository;
        _movementRepository = movementRepository;
        _productRepository = productRepository;
        _branchWarehouseRepository = branchWarehouseRepository;
        _currentUserService = currentUserService;
        _mediator = mediator;
        _unitOfWork = unitOfWork;
    }

    public async Task<Guid> Handle(AdjustInventoryCommand request, CancellationToken cancellationToken)
    {
        // 1. Validar bodega
        var branchWarehouse = await _branchWarehouseRepository.GetByIdAsync(request.BranchWarehouseId);
        if (branchWarehouse == null)
            throw new ArgumentException("La bodega de sucursal especificada no existe.");
        if (!branchWarehouse.IsActive)
            throw new InvalidOperationException("La bodega de sucursal especificada no está activa.");

        // 2. Validar producto
        var product = await _productRepository.GetByIdWithDetailsAsync(request.ProductId, cancellationToken);
        if (product == null)
            throw new ArgumentException("El producto especificado no existe.");
        if (!product.IsActive)
            throw new InvalidOperationException("El producto especificado no está activo.");
        if (product.ProductType == ProductType.Service)
            throw new InvalidOperationException("No se puede registrar movimientos de inventario para productos de tipo Servicio.");
        if (!product.TrackInventory)
            throw new InvalidOperationException("El producto especificado no maneja control de inventario.");

        // 3. Obtener presentación
        var presentation = product.Presentations.FirstOrDefault(p => p.Id == request.ProductPresentationId);
        if (presentation == null)
            throw new ArgumentException("La presentación especificada no existe para este producto.");
        if (!presentation.IsActive)
            throw new InvalidOperationException("La presentación especificada no está activa.");

        decimal conversionFactor = presentation.ConversionFactor;
        decimal quantityInBaseUnit = request.Quantity * conversionFactor;

        // 4. Obtener o crear inventario
        bool isNewInventory = false;
        var inventory = await _inventoryRepository.GetByWarehouseAndProductAsync(request.BranchWarehouseId, request.ProductId, cancellationToken);
        if (inventory == null)
        {
            if (!request.IsPositive)
                throw new InvalidOperationException("No se puede realizar un ajuste negativo si no existen existencias iniciales registradas.");

            inventory = new Domain.Entities.Inventory
            {
                Id = Guid.NewGuid(),
                BranchWarehouseId = request.BranchWarehouseId,
                ProductId = request.ProductId,
                PhysicalStock = 0,
                ReservedStock = 0,
                CommittedStock = 0,
                CreatedBy = _currentUserService.UserId ?? "System",
                CreatedOnUtc = DateTime.UtcNow
            };
            isNewInventory = true;
        }

        MovementType movementType;
        Guid? fromWarehouseId = null;
        Guid? toWarehouseId = null;

        if (request.IsPositive)
        {
            inventory.PhysicalStock += quantityInBaseUnit;
            movementType = MovementType.PositiveAdjustment;
            toWarehouseId = request.BranchWarehouseId;
        }
        else
        {
            // Validar stock disponible para evitar negativos
            if (!branchWarehouse.AllowNegativeInventory && inventory.AvailableStock < quantityInBaseUnit)
                throw new InvalidOperationException($"Existencias insuficientes para realizar el ajuste negativo. Disponible: {inventory.AvailableStock}, Requerido: {quantityInBaseUnit}.");

            inventory.PhysicalStock -= quantityInBaseUnit;
            movementType = MovementType.NegativeAdjustment;
            fromWarehouseId = request.BranchWarehouseId;
        }

        if (isNewInventory)
        {
            await _inventoryRepository.AddAsync(inventory);
        }
        else
        {
            _inventoryRepository.Update(inventory);
        }

        // 5. Crear movimiento de inventario
        var movementNumber = await _movementRepository.GenerateMovementNumberAsync(cancellationToken);
        var movement = new InventoryMovement
        {
            Id = Guid.NewGuid(),
            MovementNumber = movementNumber,
            MovementType = movementType,
            FromBranchWarehouseId = fromWarehouseId,
            ToBranchWarehouseId = toWarehouseId,
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

        // 6. AutoMarkSoldOut
        if (product.AutoMarkSoldOut)
        {
            var activeWarehouses = await _branchWarehouseRepository.FindAsync(bw => bw.IsActive);
            var activeWarehouseIds = activeWarehouses.Select(w => w.Id).ToList();
            var inventories = await _inventoryRepository.FindAsync(i => i.ProductId == product.Id);
            
            // Consolidar existencias considerando que el inventario actual ya fue modificado en memoria
            var otherWarehousesStock = inventories
                .Where(i => i.Id != inventory.Id && activeWarehouseIds.Contains(i.BranchWarehouseId))
                .Sum(i => i.PhysicalStock);
            
            var totalPhysicalStock = otherWarehousesStock;
            if (activeWarehouseIds.Contains(inventory.BranchWarehouseId))
            {
                totalPhysicalStock += inventory.PhysicalStock;
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

        // Generar asiento contable automático
        var cost = quantityInBaseUnit * product.CurrentCost;
        if (cost > 0)
        {
            var jeDetails = new List<JournalEntryDetailInput>();
            if (request.IsPositive)
            {
                // Entrada: Dr 1300 Inventarios / Cr 6100 Ajustes Operativos
                jeDetails.Add(new JournalEntryDetailInput("1300", cost, 0, $"Ajuste Positivo Inventario - {product.Name}"));
                jeDetails.Add(new JournalEntryDetailInput("6100", 0, cost, $"Ajuste Positivo Inventario - {product.Name}"));
            }
            else
            {
                // Salida: Dr 6100 Ajustes Operativos / Cr 1300 Inventarios
                jeDetails.Add(new JournalEntryDetailInput("6100", cost, 0, $"Ajuste Negativo Inventario - {product.Name}"));
                jeDetails.Add(new JournalEntryDetailInput("1300", 0, cost, $"Ajuste Negativo Inventario - {product.Name}"));
            }

            var createJeCmd = new CreateJournalEntryCommand(
                EntryDate: DateTime.UtcNow,
                Description: $"Asiento por Ajuste de Inventario: {movement.MovementNumber}",
                ReferenceDocument: movement.MovementNumber,
                ReferenceId: movement.Id,
                SourceModule: "Inventory",
                Details: jeDetails,
                PostImmediately: true
            );

            await _mediator.Send(createJeCmd, cancellationToken);
        }

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return movement.Id;
    }
}
