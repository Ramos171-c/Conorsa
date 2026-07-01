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

public record CreateProductPresentationCommand(Guid ProductId, ProductPresentationInputDto Presentation) : IRequest<Guid>;

public class CreateProductPresentationCommandValidator : AbstractValidator<CreateProductPresentationCommand>
{
    private readonly IProductRepository _productRepository;
    private readonly IProductPresentationRepository _presentationRepository;

    public CreateProductPresentationCommandValidator(
        IProductRepository productRepository,
        IProductPresentationRepository presentationRepository)
    {
        _productRepository = productRepository;
        _presentationRepository = presentationRepository;

        RuleFor(x => x.ProductId)
            .NotEmpty().WithMessage("El ProductId es requerido.");

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
                // Validar unicidad global del código de barras
                var existing = await _productRepository.ExistsBarcodeAsync(clean, null, cancellation);
                return !existing;
            }).WithMessage("El código de barras ya está registrado en otro producto.");
    }
}

public class CreateProductPresentationCommandHandler : IRequestHandler<CreateProductPresentationCommand, Guid>
{
    private readonly IProductRepository _productRepository;
    private readonly IRepository<ProductPriceHistory> _priceHistoryRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUserService _currentUserService;

    public CreateProductPresentationCommandHandler(
        IProductRepository productRepository,
        IRepository<ProductPriceHistory> priceHistoryRepository,
        IUnitOfWork unitOfWork,
        ICurrentUserService currentUserService)
    {
        _productRepository = productRepository;
        _priceHistoryRepository = priceHistoryRepository;
        _unitOfWork = unitOfWork;
        _currentUserService = currentUserService;
    }

    public async Task<Guid> Handle(CreateProductPresentationCommand request, CancellationToken cancellationToken)
    {
        var product = await _productRepository.GetByIdWithDetailsAsync(request.ProductId, cancellationToken);
        if (product == null) throw new KeyNotFoundException("Producto no encontrado.");

        var pres = request.Presentation;
        var cleanBarcode = string.IsNullOrWhiteSpace(pres.Barcode) ? null : pres.Barcode.Trim();
        var cleanName = pres.Name.Trim();

        // Validaciones de negocio contextuales sobre el producto:
        
        // 1. Costo mayor a cero para productos físicos activos
        if (product.ProductType == ProductType.Physical && pres.IsActive && pres.Cost <= 0)
        {
            throw new InvalidOperationException("El costo de la presentación activa debe ser mayor a 0 para productos físicos.");
        }

        // 2. Unicidad de nombres de presentaciones por producto
        if (product.Presentations.Any(p => !p.IsDeleted && p.Name.Trim().Equals(cleanName, StringComparison.OrdinalIgnoreCase)))
        {
            throw new InvalidOperationException($"Ya existe una presentación con el nombre '{cleanName}' para este producto.");
        }

        // 3. Unicidad de factores de conversión en presentaciones activas
        if (pres.IsActive && product.Presentations.Any(p => !p.IsDeleted && p.IsActive && p.ConversionFactor == pres.ConversionFactor))
        {
            throw new InvalidOperationException($"Ya existe otra presentación activa con el factor de conversión {pres.ConversionFactor} para este producto.");
        }

        // 4. Unicidad de código de barras a nivel de producto
        if (cleanBarcode != null && product.Presentations.Any(p => !p.IsDeleted && cleanBarcode.Equals(p.Barcode, StringComparison.OrdinalIgnoreCase)))
        {
            throw new InvalidOperationException($"El código de barras '{cleanBarcode}' ya está registrado en otra presentación de este producto.");
        }

        // 5. Presentación base
        if (pres.IsBaseUnit)
        {
            if (pres.ConversionFactor != 1.00m)
            {
                throw new InvalidOperationException("La presentación base debe tener obligatoriamente un factor de conversión igual a 1.00.");
            }
            if (product.Presentations.Any(p => !p.IsDeleted && p.IsBaseUnit))
            {
                throw new InvalidOperationException("Ya existe una presentación base para este producto. No se permite más de una presentación base.");
            }
        }
        else
        {
            // Validar que no se cree con factor 1.00 si no es base
            if (pres.ConversionFactor == 1.00m)
            {
                throw new InvalidOperationException("Solo la presentación base puede tener un factor de conversión de 1.00.");
            }
        }

        // If this is marked as default, clear others
        if (pres.IsDefaultSalePresentation)
        {
            if (!pres.IsActive)
            {
                throw new InvalidOperationException("La presentación predeterminada de venta debe estar activa.");
            }
            foreach (var p in product.Presentations)
            {
                p.IsDefaultSalePresentation = false;
            }
        }

        var newPres = new ProductPresentation
        {
            Id = Guid.NewGuid(),
            ProductId = product.Id,
            UnitOfMeasureId = pres.UnitOfMeasureId,
            Name = cleanName,
            ConversionFactor = pres.ConversionFactor,
            Barcode = cleanBarcode,
            Cost = pres.Cost,
            RetailPrice = pres.RetailPrice,
            SemiWholesalePrice = pres.SemiWholesalePrice,
            WholesalePrice = pres.WholesalePrice,
            IsBaseUnit = pres.IsBaseUnit,
            IsDefaultSalePresentation = pres.IsDefaultSalePresentation,
            AllowPurchase = pres.AllowPurchase,
            AllowSale = pres.AllowSale,
            IsActive = pres.IsActive
        };

        product.Presentations.Add(newPres);

        // Price log
        var history = new ProductPriceHistory
        {
            Id = Guid.NewGuid(),
            ProductId = product.Id,
            ProductPresentationId = newPres.Id,
            OldRetailPrice = 0,
            NewRetailPrice = newPres.RetailPrice,
            OldSemiWholesalePrice = 0,
            NewSemiWholesalePrice = newPres.SemiWholesalePrice,
            OldWholesalePrice = 0,
            NewWholesalePrice = newPres.WholesalePrice,
            OldCost = 0,
            NewCost = newPres.Cost,
            ChangedBy = _currentUserService.UserId ?? "System",
            ChangedAt = DateTime.UtcNow,
            Reason = "Creación de presentación individual"
        };
        await _priceHistoryRepository.AddAsync(history);

        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return newPres.Id;
    }
}
