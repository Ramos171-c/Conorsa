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
using EnterpriseBillingSystem.Application.Products.DTOs;

namespace EnterpriseBillingSystem.Application.Products.Commands;

public record UpdateProductPresentationCommand(Guid Id, ProductPresentationInputDto Presentation) : IRequest<bool>;

public class UpdateProductPresentationCommandValidator : AbstractValidator<UpdateProductPresentationCommand>
{
    private readonly IProductRepository _productRepository;

    public UpdateProductPresentationCommandValidator(IProductRepository productRepository)
    {
        _productRepository = productRepository;

        RuleFor(x => x.Id)
            .NotEmpty().WithMessage("El Id de la presentación es requerido.");

        RuleFor(x => x.Presentation).NotNull().WithMessage("La presentación es requerida.");

        RuleFor(x => x.Presentation.Name)
            .NotEmpty().WithMessage("El nombre de la presentación es requerido.")
            .MaximumLength(150).WithMessage("El nombre no puede exceder 150 caracteres.");

        RuleFor(x => x.Presentation.ConversionFactor)
            .GreaterThan(0).WithMessage("El factor de conversión debe ser mayor a 0.");

        RuleFor(x => x.Presentation.Cost)
            .GreaterThanOrEqualTo(0).WithMessage("El costo no puede ser negativo.");

        RuleFor(x => x.Presentation.RetailPrice)
            .GreaterThanOrEqualTo(0).WithMessage("El precio minorista no puede ser negativo.")
            .Must((cmd, retailPrice) => retailPrice >= cmd.Presentation.Cost)
            .WithMessage("El precio minorista no puede ser menor que el costo.");

        RuleFor(x => x.Presentation.SemiWholesalePrice)
            .GreaterThanOrEqualTo(0).WithMessage("El precio semi-mayorista no puede ser negativo.")
            .Must((cmd, semiPrice) => semiPrice >= cmd.Presentation.Cost)
            .WithMessage("El precio semi-mayorista no puede ser menor que el costo.");

        RuleFor(x => x.Presentation.WholesalePrice)
            .GreaterThanOrEqualTo(0).WithMessage("El precio mayorista no puede ser negativo.")
            .Must((cmd, wholesalePrice) => wholesalePrice >= cmd.Presentation.Cost)
            .WithMessage("El precio mayorista no puede ser menor que el costo.");

        RuleFor(x => x.Presentation.Barcode)
            .Must((pres, barcode) =>
            {
                if (string.IsNullOrWhiteSpace(barcode)) return true;
                var clean = barcode.Trim();
                return clean.Length >= 3 && !clean.Contains(" ");
            }).WithMessage("El código de barras si se define debe tener mínimo 3 caracteres y no contener espacios.")
            .MustAsync(async (command, barcode, cancellation) =>
            {
                if (string.IsNullOrWhiteSpace(barcode)) return true;
                var clean = barcode.Trim();
                // Validar unicidad global del código de barras (excluyendo el producto actual)
                var existing = await _productRepository.ExistsBarcodeAsync(clean, null, cancellation);
                // Si existe globalmente, pero pertenece a este mismo producto/presentación, se maneja en el handler con el id
                return true; // Se valida con mayor detalle en el Handler
            });
    }
}

public class UpdateProductPresentationCommandHandler : IRequestHandler<UpdateProductPresentationCommand, bool>
{
    private readonly IProductPresentationRepository _presentationRepository;
    private readonly IProductRepository _productRepository;
    private readonly IRepository<ProductPriceHistory> _priceHistoryRepository;
    private readonly IRepository<SalesInvoiceDetail> _salesDetailRepository;
    private readonly IRepository<PurchaseInvoiceDetail> _purchaseDetailRepository;
    private readonly IRepository<InventoryMovementDetail> _movementDetailRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUserService _currentUserService;

    public UpdateProductPresentationCommandHandler(
        IProductPresentationRepository presentationRepository,
        IProductRepository productRepository,
        IRepository<ProductPriceHistory> priceHistoryRepository,
        IRepository<SalesInvoiceDetail> salesDetailRepository,
        IRepository<PurchaseInvoiceDetail> purchaseDetailRepository,
        IRepository<InventoryMovementDetail> movementDetailRepository,
        IUnitOfWork unitOfWork,
        ICurrentUserService currentUserService)
    {
        _presentationRepository = presentationRepository;
        _productRepository = productRepository;
        _priceHistoryRepository = priceHistoryRepository;
        _salesDetailRepository = salesDetailRepository;
        _purchaseDetailRepository = purchaseDetailRepository;
        _movementDetailRepository = movementDetailRepository;
        _unitOfWork = unitOfWork;
        _currentUserService = currentUserService;
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

    public async Task<bool> Handle(UpdateProductPresentationCommand request, CancellationToken cancellationToken)
    {
        var existing = await _presentationRepository.GetByIdAsync(request.Id, cancellationToken);
        if (existing == null) return false;

        var product = await _productRepository.GetByIdWithDetailsAsync(existing.ProductId, cancellationToken);
        if (product == null) return false;

        var pres = request.Presentation;
        var cleanBarcode = string.IsNullOrWhiteSpace(pres.Barcode) ? null : pres.Barcode.Trim();
        var cleanName = pres.Name.Trim();

        // Validaciones de negocio contextuales sobre el producto:

        // 1. Costo mayor a cero para productos físicos activos
        if (product.ProductType == ProductType.Physical && pres.IsActive && pres.Cost <= 0)
        {
            throw new InvalidOperationException("El costo de la presentación activa debe ser mayor a 0 para productos físicos.");
        }

        // 2. Unicidad de nombres de presentaciones por producto (excluyendo la actual)
        if (product.Presentations.Any(p => p.Id != existing.Id && !p.IsDeleted && p.Name.Trim().Equals(cleanName, StringComparison.OrdinalIgnoreCase)))
        {
            throw new InvalidOperationException($"Ya existe otra presentación con el nombre '{cleanName}' para este producto.");
        }

        // 3. Unicidad de factores de conversión en presentaciones activas (excluyendo la actual)
        if (pres.IsActive && product.Presentations.Any(p => p.Id != existing.Id && !p.IsDeleted && p.IsActive && p.ConversionFactor == pres.ConversionFactor))
        {
            throw new InvalidOperationException($"Ya existe otra presentación activa con el factor de conversión {pres.ConversionFactor} para este producto.");
        }

        // 4. Unicidad de código de barras a nivel de producto (excluyendo la actual)
        if (cleanBarcode != null && product.Presentations.Any(p => p.Id != existing.Id && !p.IsDeleted && cleanBarcode.Equals(p.Barcode, StringComparison.OrdinalIgnoreCase)))
        {
            throw new InvalidOperationException($"El código de barras '{cleanBarcode}' ya está registrado en otra presentación de este producto.");
        }

        // 5. Unicidad global de código de barras
        if (cleanBarcode != null)
        {
            var globalBarcodeExists = await _productRepository.ExistsBarcodeAsync(cleanBarcode, product.Id, cancellationToken);
            if (globalBarcodeExists)
            {
                throw new InvalidOperationException($"El código de barras '{cleanBarcode}' ya está registrado en otro producto.");
            }
        }

        // 6. Inmutabilidad por movimientos
        var hasMovements = await PresentationHasMovementsAsync(existing.Id);
        if (hasMovements)
        {
            if (existing.UnitOfMeasureId != pres.UnitOfMeasureId)
            {
                throw new InvalidOperationException($"No se permite modificar la Unidad de Medida de la presentación '{existing.Name}' porque ya registra movimientos de inventario históricos.");
            }
            if (existing.ConversionFactor != pres.ConversionFactor)
            {
                throw new InvalidOperationException($"No se permite modificar el factor de conversión de la presentación '{existing.Name}' porque ya registra movimientos de inventario históricos.");
            }
        }

        // 7. Presentación base
        if (existing.IsBaseUnit)
        {
            if (pres.ConversionFactor != 1.00m)
            {
                throw new InvalidOperationException("No se permite modificar el factor de conversión de la unidad de medida base (debe ser 1.00).");
            }
            if (!pres.IsActive)
            {
                throw new InvalidOperationException("No se permite desactivar la presentación base.");
            }
            if (!pres.IsBaseUnit)
            {
                throw new InvalidOperationException("No se puede desactivar el indicador de unidad base directamente desde la presentación base. Debe marcar otra como unidad base.");
            }
        }
        else if (pres.IsBaseUnit)
        {
            if (pres.ConversionFactor != 1.00m)
            {
                throw new InvalidOperationException("La presentación base debe tener un factor de conversión de 1.00.");
            }
            // Desmarcar la base anterior
            foreach (var p in product.Presentations.Where(x => x.Id != existing.Id))
            {
                p.IsBaseUnit = false;
            }
        }
        else
        {
            if (pres.ConversionFactor == 1.00m)
            {
                throw new InvalidOperationException("Solo la presentación base puede tener un factor de conversión de 1.00.");
            }
        }

        // 8. Presentación por defecto
        if (pres.IsDefaultSalePresentation)
        {
            if (!pres.IsActive)
            {
                throw new InvalidOperationException("La presentación predeterminada de venta debe estar activa.");
            }
            foreach (var p in product.Presentations.Where(x => x.Id != existing.Id))
            {
                p.IsDefaultSalePresentation = false;
            }
        }
        else if (existing.IsDefaultSalePresentation)
        {
            // Validar si existen documentos en borrador o activos utilizando la presentación por defecto anterior si se intenta quitar
            var salesInDraft = await _salesDetailRepository.FindAsync(d => d.ProductPresentationId == existing.Id && d.SalesInvoice.Status == SalesInvoiceStatus.Draft);
            if (salesInDraft.Any())
            {
                throw new InvalidOperationException($"No se permite cambiar la presentación por defecto de venta mientras existan facturas en borrador que la utilicen.");
            }
        }

        var priceChanged = existing.RetailPrice != pres.RetailPrice || 
                            existing.SemiWholesalePrice != pres.SemiWholesalePrice || 
                            existing.WholesalePrice != pres.WholesalePrice || 
                            existing.Cost != pres.Cost ||
                            existing.Barcode != cleanBarcode ||
                            existing.Name != cleanName ||
                            existing.ConversionFactor != pres.ConversionFactor ||
                            existing.IsBaseUnit != pres.IsBaseUnit ||
                            existing.IsDefaultSalePresentation != pres.IsDefaultSalePresentation;
                            
        var oldRetailPrice = existing.RetailPrice;
        var oldSemiWholesalePrice = existing.SemiWholesalePrice;
        var oldWholesalePrice = existing.WholesalePrice;
        var oldCost = existing.Cost;

        existing.Name = cleanName;
        existing.UnitOfMeasureId = pres.UnitOfMeasureId;
        existing.ConversionFactor = pres.ConversionFactor;
        existing.Barcode = cleanBarcode;
        existing.Cost = pres.Cost;
        existing.RetailPrice = pres.RetailPrice;
        existing.SemiWholesalePrice = pres.SemiWholesalePrice;
        existing.WholesalePrice = pres.WholesalePrice;
        existing.IsBaseUnit = pres.IsBaseUnit;
        existing.IsDefaultSalePresentation = pres.IsDefaultSalePresentation;
        existing.AllowPurchase = pres.AllowPurchase;
        existing.AllowSale = pres.AllowSale;
        existing.IsActive = pres.IsActive;

        if (priceChanged)
        {
            var changesList = new List<string>();
            if (oldCost != pres.Cost) changesList.Add($"Costo: {oldCost} -> {pres.Cost}");
            if (oldRetailPrice != pres.RetailPrice) changesList.Add($"Retail: {oldRetailPrice} -> {pres.RetailPrice}");
            if (oldSemiWholesalePrice != pres.SemiWholesalePrice) changesList.Add($"Semi: {oldSemiWholesalePrice} -> {pres.SemiWholesalePrice}");
            if (oldWholesalePrice != pres.WholesalePrice) changesList.Add($"Wholesale: {oldWholesalePrice} -> {pres.WholesalePrice}");

            var reason = changesList.Count > 0 
                ? $"Actualización individual de presentación ({string.Join(", ", changesList)})"
                : "Actualización individual de presentación (otros metadatos)";

            var history = new ProductPriceHistory
            {
                Id = Guid.NewGuid(),
                ProductId = product.Id,
                ProductPresentationId = existing.Id,
                OldRetailPrice = oldRetailPrice,
                NewRetailPrice = pres.RetailPrice,
                OldSemiWholesalePrice = oldSemiWholesalePrice,
                NewSemiWholesalePrice = pres.SemiWholesalePrice,
                OldWholesalePrice = oldWholesalePrice,
                NewWholesalePrice = pres.WholesalePrice,
                OldCost = oldCost,
                NewCost = pres.Cost,
                ChangedBy = _currentUserService.UserId ?? "System",
                ChangedAt = DateTime.UtcNow,
                Reason = reason
            };
            await _priceHistoryRepository.AddAsync(history);
        }

        _presentationRepository.Update(existing);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return true;
    }
}
