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

public record CreateProductCommand(
    string InternalCode,
    string Name,
    string? Description,
    ProductType ProductType,
    ProductStatus ProductStatus,
    bool TrackInventory,
    bool RequiresSerialNumber,
    bool RequiresBatchControl,
    Guid CategoryId,
    Guid? BrandId,
    Guid DefaultUnitOfMeasureId,
    decimal CurrentCost,
    string? ImagePath,
    bool IsCatalogVisible,
    bool IsSoldOut,
    decimal MinimumStock,
    bool IsFavorite,
    int FavoriteOrder,
    bool AllowPromotions,
    bool HighlightInCatalog,
    string? ShortDescription,
    string? CatalogBadge,
    int DisplayOrder,
    bool AutoMarkSoldOut,
    Guid TaxId,
    ICollection<ProductPresentationInputDto> Presentations,
    ICollection<CreateBranchProductOverrideDto> BranchOverrides
) : IRequest<Guid>;

public record CreateBranchProductOverrideDto(
    Guid BranchId,
    decimal? LocalSalePrice,
    decimal? MinSalePrice,
    decimal? MaxDiscountPercentage,
    bool IsActive
);

public class CreateProductCommandValidator : AbstractValidator<CreateProductCommand>
{
    private readonly IProductRepository _productRepository;
    private readonly ICategoryRepository _categoryRepository;
    private readonly IBrandRepository _brandRepository;
    private readonly IUnitOfMeasureRepository _uomRepository;
    private readonly IRepository<Tax> _taxRepository;

    public CreateProductCommandValidator(
        IProductRepository productRepository,
        ICategoryRepository categoryRepository,
        IBrandRepository brandRepository,
        IUnitOfMeasureRepository uomRepository,
        IRepository<Tax> taxRepository)
    {
        _productRepository = productRepository;
        _categoryRepository = categoryRepository;
        _brandRepository = brandRepository;
        _uomRepository = uomRepository;
        _taxRepository = taxRepository;

        RuleFor(x => x.InternalCode)
            .NotEmpty().WithMessage("El código interno (SKU) es requerido.")
            .MaximumLength(50).WithMessage("El código interno no puede exceder 50 caracteres.")
            .MustAsync(async (code, cancellation) =>
            {
                var existing = await _productRepository.ExistsSkuAsync(code, null, cancellation);
                return !existing;
            }).WithMessage("Ya existe un producto activo con este código interno.");

        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("El nombre del producto es requerido.")
            .MaximumLength(150).WithMessage("El nombre no puede exceder 150 caracteres.");

        RuleFor(x => x.Description)
            .MaximumLength(500).WithMessage("La descripción no puede exceder 500 caracteres.");

        RuleFor(x => x.ProductType)
            .IsInEnum().WithMessage("El tipo de producto especificado no es válido.");

        RuleFor(x => x.ProductStatus)
            .IsInEnum().WithMessage("El estado de producto especificado no es válido.");

        RuleFor(x => x.MinimumStock)
            .GreaterThanOrEqualTo(0).WithMessage("El stock mínimo no puede ser negativo.");

        RuleFor(x => x.CategoryId)
            .NotEmpty().WithMessage("La categoría es requerida.")
            .MustAsync(async (catId, cancellation) =>
            {
                var cat = await _categoryRepository.GetByIdAsync(catId);
                return cat != null;
            }).WithMessage("La categoría especificada no existe.");

        RuleFor(x => x.BrandId)
            .MustAsync(async (brandId, cancellation) =>
            {
                if (brandId == null) return true;
                var brand = await _brandRepository.GetByIdAsync(brandId.Value);
                return brand != null;
            }).WithMessage("La marca especificada no existe.");

        RuleFor(x => x.DefaultUnitOfMeasureId)
            .NotEmpty().WithMessage("La unidad de medida base es requerida.")
            .MustAsync(async (uomId, cancellation) =>
            {
                var uom = await _uomRepository.GetByIdAsync(uomId);
                return uom != null;
            }).WithMessage("La unidad de medida especificada no existe.");

        RuleFor(x => x.TaxId)
            .NotEmpty().WithMessage("El impuesto es requerido para productos inventariables.")
            .MustAsync(async (taxId, cancellation) =>
            {
                var tax = await _taxRepository.GetByIdAsync(taxId);
                return tax != null;
            }).WithMessage("El impuesto especificado no existe.");

        // Validaciones de Presentaciones
        RuleFor(x => x.Presentations)
            .NotEmpty().WithMessage("Al menos una presentación es requerida.")
            .Must(p => p != null && p.Count(x => x.IsDefaultSalePresentation) == 1)
            .WithMessage("Debe marcar exactamente una presentación como predeterminada de venta.")
            .Must(p => p != null && p.Count(x => x.IsBaseUnit) == 1)
            .WithMessage("Debe marcar exactamente una presentación como la unidad base.")
            .Must(p =>
            {
                if (p == null || p.Count == 0) return true;
                // La presentación base debe ser la primera creada
                var first = p.First();
                return first.IsBaseUnit && first.ConversionFactor == 1.00m;
            }).WithMessage("La presentación base (IsBaseUnit = true, Factor = 1) debe ser la primera creada en la lista de presentaciones.")
            .Must(p =>
            {
                if (p == null || p.Count <= 1) return true;
                // No se pueden agregar presentaciones secundarias con factor 1
                return p.Skip(1).All(x => !x.IsBaseUnit && x.ConversionFactor != 1.00m);
            }).WithMessage("No se permite más de una presentación con factor de conversión 1.00 o marcada como unidad base.")
            .Must(p =>
            {
                if (p == null) return true;
                // Validar que no existan factores de conversión repetidos para presentaciones activas del mismo producto
                var activeFactors = p.Where(x => x.IsActive).Select(x => x.ConversionFactor).ToList();
                return activeFactors.Count == activeFactors.Distinct().Count();
            }).WithMessage("No se permiten factores de conversión duplicados entre las presentaciones activas.")
            .Must(p =>
            {
                if (p == null) return true;
                // Validar que no existan nombres duplicados (insensible a mayúsculas/minúsculas)
                var names = p.Select(x => x.Name.Trim().ToLowerInvariant()).ToList();
                return names.Count == names.Distinct().Count();
            }).WithMessage("No se permiten presentaciones con nombres duplicados para el mismo producto.")
            .Must(p =>
            {
                if (p == null) return true;
                var barcodes = p.Where(x => !string.IsNullOrWhiteSpace(x.Barcode))
                                .Select(x => x.Barcode!.Trim().ToLowerInvariant()).ToList();
                return barcodes.Count == barcodes.Distinct().Count();
            }).WithMessage("No se permiten códigos de barra duplicados en las presentaciones del mismo producto.")
            .Must(p =>
            {
                if (p == null) return true;
                var defaultPres = p.FirstOrDefault(x => x.IsDefaultSalePresentation);
                return defaultPres == null || defaultPres.IsActive;
            }).WithMessage("La presentación predeterminada de venta debe estar activa (IsActive = true).")
            .Must((cmd, p) =>
            {
                if (cmd.ProductType == ProductType.Physical && p != null)
                {
                    return p.All(pres => !pres.IsActive || pres.Cost > 0);
                }
                return true;
            }).WithMessage("El costo de la presentación activa debe ser mayor a 0 para productos físicos.");

        RuleForEach(x => x.Presentations).ChildRules(p =>
        {
            p.RuleFor(x => x.Name)
                .NotEmpty().WithMessage("El nombre de la presentación es requerido.")
                .MaximumLength(150).WithMessage("El nombre no puede exceder 150 caracteres.");

            p.RuleFor(x => x.ConversionFactor)
                .GreaterThan(0).WithMessage("El factor de conversión debe ser mayor a 0.");

            p.RuleFor(x => x.Cost)
                .GreaterThanOrEqualTo(0).WithMessage("El costo no puede ser negativo.");

            p.RuleFor(x => x.RetailPrice)
                .GreaterThanOrEqualTo(0).WithMessage("El precio minorista no puede ser negativo.")
                .Must((pres, retailPrice) => retailPrice >= pres.Cost)
                .WithMessage("El precio minorista no puede ser menor que el costo.");

            p.RuleFor(x => x.SemiWholesalePrice)
                .GreaterThanOrEqualTo(0).WithMessage("El precio semi-mayorista no puede ser negativo.")
                .Must((pres, semiPrice) => semiPrice >= pres.Cost)
                .WithMessage("El precio semi-mayorista no puede ser menor que el costo.");

            p.RuleFor(x => x.WholesalePrice)
                .GreaterThanOrEqualTo(0).WithMessage("El precio mayorista no puede ser negativo.")
                .Must((pres, wholesalePrice) => wholesalePrice >= pres.Cost)
                .WithMessage("El precio mayorista no puede ser menor que el costo.");

            p.RuleFor(x => x.Barcode)
                .Must((pres, barcode) =>
                {
                    if (string.IsNullOrWhiteSpace(barcode)) return true;
                    var clean = barcode.Trim();
                    return clean.Length >= 3 && !clean.Contains(" ");
                }).WithMessage("El código de barras si se define debe tener mínimo 3 caracteres y no contener espacios.")
                .MustAsync(async (barcode, cancellation) =>
                {
                    if (string.IsNullOrWhiteSpace(barcode)) return true;
                    var clean = barcode.Trim();
                    var existing = await _productRepository.ExistsBarcodeAsync(clean, null, cancellation);
                    return !existing;
                }).WithMessage("El código de barras ya está registrado en otro producto.");
        });

        RuleFor(x => x)
            .Must(x => x.ProductType != ProductType.Service || (!x.TrackInventory && !x.RequiresSerialNumber && !x.RequiresBatchControl))
            .WithMessage("Los servicios no pueden tener activados el control de inventario, números de serie ni lotes.");
    }
}

public class CreateProductCommandHandler : IRequestHandler<CreateProductCommand, Guid>
{
    private readonly IProductRepository _productRepository;
    private readonly ICurrentUserService _currentUserService;
    private readonly IUnitOfWork _unitOfWork;

    public CreateProductCommandHandler(
        IProductRepository productRepository,
        ICurrentUserService currentUserService,
        IUnitOfWork unitOfWork)
    {
        _productRepository = productRepository;
        _currentUserService = currentUserService;
        _unitOfWork = unitOfWork;
    }

    public async Task<Guid> Handle(CreateProductCommand request, CancellationToken cancellationToken)
    {
        var imagePath = request.ImagePath;
        if (!string.IsNullOrWhiteSpace(imagePath) && (imagePath.StartsWith("http://", StringComparison.OrdinalIgnoreCase) || imagePath.StartsWith("https://", StringComparison.OrdinalIgnoreCase)))
        {
            try
            {
                var uri = new Uri(imagePath);
                imagePath = uri.AbsolutePath;
            }
            catch
            {
                // Ignore parsing errors
            }
        }

        var product = new Product
        {
            InternalCode = request.InternalCode,
            Name = request.Name,
            Description = request.Description,
            ProductType = request.ProductType,
            ProductStatus = request.ProductStatus,
            TrackInventory = request.TrackInventory,
            RequiresSerialNumber = request.RequiresSerialNumber,
            RequiresBatchControl = request.RequiresBatchControl,
            CategoryId = request.CategoryId,
            BrandId = request.BrandId,
            DefaultUnitOfMeasureId = request.DefaultUnitOfMeasureId,
            CurrentCost = request.CurrentCost,
            ImagePath = imagePath,
            IsCatalogVisible = request.IsCatalogVisible,
            IsSoldOut = request.IsSoldOut,
            SoldOutAt = request.IsSoldOut ? DateTime.UtcNow : null,
            SoldOutBy = request.IsSoldOut ? (_currentUserService.UserId ?? "System") : null,
            MinimumStock = request.MinimumStock,
            IsFavorite = request.IsFavorite,
            FavoriteOrder = request.FavoriteOrder,
            AllowPromotions = request.AllowPromotions,
            HighlightInCatalog = request.HighlightInCatalog,
            ShortDescription = request.ShortDescription,
            CatalogBadge = request.CatalogBadge,
            DisplayOrder = request.DisplayOrder,
            AutoMarkSoldOut = request.AutoMarkSoldOut,
            TaxId = request.TaxId,
            IsActive = true
        };

        if (request.Presentations != null)
        {
            foreach (var pres in request.Presentations)
            {
                var cleanBarcode = string.IsNullOrWhiteSpace(pres.Barcode) ? null : pres.Barcode.Trim();
                product.Presentations.Add(new ProductPresentation
                {
                    UnitOfMeasureId = pres.UnitOfMeasureId,
                    Name = pres.Name.Trim(),
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
                });
            }
        }

        if (request.BranchOverrides != null)
        {
            foreach (var ovr in request.BranchOverrides)
            {
                product.BranchProducts.Add(new BranchProduct
                {
                    BranchId = ovr.BranchId,
                    LocalSalePrice = ovr.LocalSalePrice,
                    MinSalePrice = ovr.MinSalePrice,
                    MaxDiscountPercentage = ovr.MaxDiscountPercentage,
                    IsActive = ovr.IsActive
                });
            }
        }

        await _productRepository.AddAsync(product);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return product.Id;
    }
}
