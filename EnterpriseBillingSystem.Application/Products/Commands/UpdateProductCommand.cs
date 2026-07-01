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

public record UpdateProductCommand(
    Guid Id,
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
    string? PriceChangeReason,
    bool IsActive,
    ICollection<ProductPresentationInputDto> Presentations,
    ICollection<CreateBranchProductOverrideDto> BranchOverrides
) : IRequest<bool>;

public class UpdateProductCommandValidator : AbstractValidator<UpdateProductCommand>
{
    private readonly IProductRepository _productRepository;
    private readonly ICategoryRepository _categoryRepository;
    private readonly IBrandRepository _brandRepository;
    private readonly IUnitOfMeasureRepository _uomRepository;
    private readonly IRepository<Tax> _taxRepository;

    public UpdateProductCommandValidator(
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

        RuleFor(x => x.Id)
            .NotEmpty().WithMessage("El Id es requerido.");

        RuleFor(x => x.InternalCode)
            .NotEmpty().WithMessage("El código interno (SKU) es requerido.")
            .MaximumLength(50).WithMessage("El código interno no puede exceder 50 caracteres.")
            .MustAsync(async (command, code, cancellation) =>
            {
                var existing = await _productRepository.ExistsSkuAsync(code, command.Id, cancellation);
                return !existing;
            }).WithMessage("Ya existe otro producto activo con este código interno.");

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
                var first = p.First();
                return first.IsBaseUnit && first.ConversionFactor == 1.00m;
            }).WithMessage("La presentación base (IsBaseUnit = true, Factor = 1) debe ser la primera creada en la lista de presentaciones.")
            .Must(p =>
            {
                if (p == null || p.Count <= 1) return true;
                return p.Skip(1).All(x => !x.IsBaseUnit && x.ConversionFactor != 1.00m);
            }).WithMessage("No se permite más de una presentación con factor de conversión 1.00 o marcada como unidad base.")
            .Must(p =>
            {
                if (p == null) return true;
                var activeFactors = p.Where(x => x.IsActive).Select(x => x.ConversionFactor).ToList();
                return activeFactors.Count == activeFactors.Distinct().Count();
            }).WithMessage("No se permiten factores de conversión duplicados entre las presentaciones activas.")
            .Must(p =>
            {
                if (p == null) return true;
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
                .MustAsync(async (command, barcode, cancellation) =>
                {
                    if (string.IsNullOrWhiteSpace(barcode)) return true;
                    var clean = barcode.Trim();
                    var existing = await _productRepository.ExistsBarcodeAsync(clean, command.Id, cancellation);
                    return !existing;
                }).WithMessage("El código de barras ya está registrado en otro producto.");
        });

        RuleFor(x => x)
            .Must(x => x.ProductType != ProductType.Service || (!x.TrackInventory && !x.RequiresSerialNumber && !x.RequiresBatchControl))
            .WithMessage("Los servicios no pueden tener activados el control de inventario, números de serie ni lotes.");
    }
}

public class UpdateProductCommandHandler : IRequestHandler<UpdateProductCommand, bool>
{
    private readonly IProductRepository _productRepository;
    private readonly ICurrentUserService _currentUserService;
    private readonly IRepository<ProductPriceHistory> _priceHistoryRepository;
    private readonly IRepository<SalesInvoiceDetail> _salesDetailRepository;
    private readonly IRepository<PurchaseInvoiceDetail> _purchaseDetailRepository;
    private readonly IRepository<InventoryMovementDetail> _movementDetailRepository;
    private readonly IUnitOfWork _unitOfWork;

    public UpdateProductCommandHandler(
        IProductRepository productRepository,
        ICurrentUserService currentUserService,
        IRepository<ProductPriceHistory> priceHistoryRepository,
        IRepository<SalesInvoiceDetail> salesDetailRepository,
        IRepository<PurchaseInvoiceDetail> purchaseDetailRepository,
        IRepository<InventoryMovementDetail> movementDetailRepository,
        IUnitOfWork unitOfWork)
    {
        _productRepository = productRepository;
        _currentUserService = currentUserService;
        _priceHistoryRepository = priceHistoryRepository;
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

    public async Task<bool> Handle(UpdateProductCommand request, CancellationToken cancellationToken)
    {
        var product = await _productRepository.GetByIdWithDetailsAsync(request.Id, cancellationToken);
        if (product == null) return false;

        // Validar si se intenta cambiar la presentación por defecto y si hay documentos pendientes
        var currentDefaultPres = product.Presentations.FirstOrDefault(p => p.IsDefaultSalePresentation);
        var requestedDefaultPresDto = request.Presentations.FirstOrDefault(p => p.IsDefaultSalePresentation);
        
        if (currentDefaultPres != null && requestedDefaultPresDto != null && currentDefaultPres.Id != requestedDefaultPresDto.Id)
        {
            // Validar si existen documentos en borrador o activos utilizando la presentación por defecto anterior
            var salesInDraft = await _salesDetailRepository.FindAsync(d => d.ProductPresentationId == currentDefaultPres.Id && d.SalesInvoice.Status == SalesInvoiceStatus.Draft);
            if (salesInDraft.Any())
            {
                throw new InvalidOperationException($"No se permite cambiar la presentación por defecto de venta mientras existan facturas en borrador que la utilicen.");
            }
        }

        product.InternalCode = request.InternalCode;
        product.Name = request.Name;
        product.Description = request.Description;
        product.ProductType = request.ProductType;
        product.ProductStatus = request.ProductStatus;
        product.TrackInventory = request.TrackInventory;
        product.RequiresSerialNumber = request.RequiresSerialNumber;
        product.RequiresBatchControl = request.RequiresBatchControl;
        product.CategoryId = request.CategoryId;
        product.BrandId = request.BrandId;
        product.DefaultUnitOfMeasureId = request.DefaultUnitOfMeasureId;
        product.CurrentCost = request.CurrentCost;
        
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
        product.ImagePath = imagePath;

        product.IsCatalogVisible = request.IsCatalogVisible;

        if (product.IsSoldOut != request.IsSoldOut)
        {
            product.IsSoldOut = request.IsSoldOut;
            product.SoldOutAt = request.IsSoldOut ? DateTime.UtcNow : null;
            product.SoldOutBy = request.IsSoldOut ? (_currentUserService.UserId ?? "System") : null;
        }

        product.MinimumStock = request.MinimumStock;
        product.IsFavorite = request.IsFavorite;
        product.FavoriteOrder = request.FavoriteOrder;
        product.AllowPromotions = request.AllowPromotions;
        product.HighlightInCatalog = request.HighlightInCatalog;
        product.ShortDescription = request.ShortDescription;
        product.CatalogBadge = request.CatalogBadge;
        product.DisplayOrder = request.DisplayOrder;
        product.AutoMarkSoldOut = request.AutoMarkSoldOut;
        product.TaxId = request.TaxId;
        product.IsActive = request.IsActive;

        // Sync Presentations
        var presentationsToKeep = new List<Guid>();

        foreach (var presDto in request.Presentations)
        {
            var cleanBarcode = string.IsNullOrWhiteSpace(presDto.Barcode) ? null : presDto.Barcode.Trim();
            if (presDto.Id.HasValue && presDto.Id.Value != Guid.Empty)
            {
                // Update Existing Presentation
                var existing = product.Presentations.FirstOrDefault(p => p.Id == presDto.Id.Value);
                if (existing != null)
                {
                    var hasMovements = await PresentationHasMovementsAsync(existing.Id);
                    if (hasMovements)
                    {
                        if (existing.UnitOfMeasureId != presDto.UnitOfMeasureId)
                        {
                            throw new InvalidOperationException($"No se permite modificar la Unidad de Medida de la presentación '{existing.Name}' porque ya registra movimientos de inventario históricos.");
                        }
                        if (existing.ConversionFactor != presDto.ConversionFactor)
                        {
                            throw new InvalidOperationException($"No se permite modificar el factor de conversión de la presentación '{existing.Name}' porque ya registra movimientos de inventario históricos.");
                        }
                    }

                    // Impedir modificar factor de presentación base (siempre debe ser 1)
                    if (existing.IsBaseUnit && presDto.ConversionFactor != 1.00m)
                    {
                        throw new InvalidOperationException("No se permite modificar el factor de conversión de la unidad de medida base (debe ser 1.00).");
                    }
                    if (existing.IsBaseUnit && !presDto.IsActive)
                    {
                        throw new InvalidOperationException("No se permite desactivar la presentación base.");
                    }

                    var changesList = new List<string>();
                    if (existing.Cost != presDto.Cost) changesList.Add($"Costo: {existing.Cost} -> {presDto.Cost}");
                    if (existing.RetailPrice != presDto.RetailPrice) changesList.Add($"RetailPrice: {existing.RetailPrice} -> {presDto.RetailPrice}");
                    if (existing.SemiWholesalePrice != presDto.SemiWholesalePrice) changesList.Add($"SemiWholesalePrice: {existing.SemiWholesalePrice} -> {presDto.SemiWholesalePrice}");
                    if (existing.WholesalePrice != presDto.WholesalePrice) changesList.Add($"WholesalePrice: {existing.WholesalePrice} -> {presDto.WholesalePrice}");
                    if (existing.Barcode != cleanBarcode) changesList.Add($"Barcode: {existing.Barcode ?? "NULL"} -> {cleanBarcode ?? "NULL"}");
                    if (existing.ConversionFactor != presDto.ConversionFactor) changesList.Add($"Factor: {existing.ConversionFactor} -> {presDto.ConversionFactor}");
                    if (existing.IsBaseUnit != presDto.IsBaseUnit) changesList.Add($"IsBaseUnit: {existing.IsBaseUnit} -> {presDto.IsBaseUnit}");
                    if (existing.IsDefaultSalePresentation != presDto.IsDefaultSalePresentation) changesList.Add($"IsDefaultSalePresentation: {existing.IsDefaultSalePresentation} -> {presDto.IsDefaultSalePresentation}");

                    var oldRetailPrice = existing.RetailPrice;
                    var oldSemiWholesalePrice = existing.SemiWholesalePrice;
                    var oldWholesalePrice = existing.WholesalePrice;
                    var oldCost = existing.Cost;

                    existing.Name = presDto.Name.Trim();
                    existing.UnitOfMeasureId = presDto.UnitOfMeasureId;
                    existing.ConversionFactor = presDto.ConversionFactor;
                    existing.Barcode = cleanBarcode;
                    existing.Cost = presDto.Cost;
                    existing.RetailPrice = presDto.RetailPrice;
                    existing.SemiWholesalePrice = presDto.SemiWholesalePrice;
                    existing.WholesalePrice = presDto.WholesalePrice;
                    existing.IsBaseUnit = presDto.IsBaseUnit;
                    existing.IsDefaultSalePresentation = presDto.IsDefaultSalePresentation;
                    existing.AllowPurchase = presDto.AllowPurchase;
                    existing.AllowSale = presDto.AllowSale;
                    existing.IsActive = presDto.IsActive;

                    presentationsToKeep.Add(existing.Id);

                    if (changesList.Count > 0)
                    {
                        var reasonText = request.PriceChangeReason ?? "Cambio administrativo de presentación";
                        var descriptiveReason = $"{reasonText} ({string.Join(", ", changesList)})";
                        var history = new ProductPriceHistory
                        {
                            Id = Guid.NewGuid(),
                            ProductId = product.Id,
                            ProductPresentationId = existing.Id,
                            OldRetailPrice = oldRetailPrice,
                            NewRetailPrice = presDto.RetailPrice,
                            OldSemiWholesalePrice = oldSemiWholesalePrice,
                            NewSemiWholesalePrice = presDto.SemiWholesalePrice,
                            OldWholesalePrice = oldWholesalePrice,
                            NewWholesalePrice = presDto.WholesalePrice,
                            OldCost = oldCost,
                            NewCost = presDto.Cost,
                            ChangedBy = _currentUserService.UserId ?? "System",
                            ChangedAt = DateTime.UtcNow,
                            Reason = descriptiveReason
                        };
                        await _priceHistoryRepository.AddAsync(history);
                    }
                }
            }
            else
            {
                // Add New Presentation
                var newPres = new ProductPresentation
                {
                    Id = Guid.NewGuid(),
                    ProductId = product.Id,
                    UnitOfMeasureId = presDto.UnitOfMeasureId,
                    Name = presDto.Name.Trim(),
                    ConversionFactor = presDto.ConversionFactor,
                    Barcode = cleanBarcode,
                    Cost = presDto.Cost,
                    RetailPrice = presDto.RetailPrice,
                    SemiWholesalePrice = presDto.SemiWholesalePrice,
                    WholesalePrice = presDto.WholesalePrice,
                    IsBaseUnit = presDto.IsBaseUnit,
                    IsDefaultSalePresentation = presDto.IsDefaultSalePresentation,
                    AllowPurchase = presDto.AllowPurchase,
                    AllowSale = presDto.AllowSale,
                    IsActive = presDto.IsActive
                };
                product.Presentations.Add(newPres);
                presentationsToKeep.Add(newPres.Id);

                // Initial price log
                var history = new ProductPriceHistory
                {
                    Id = Guid.NewGuid(),
                    ProductId = product.Id,
                    ProductPresentationId = newPres.Id,
                    OldRetailPrice = 0,
                    NewRetailPrice = presDto.RetailPrice,
                    OldSemiWholesalePrice = 0,
                    NewSemiWholesalePrice = presDto.SemiWholesalePrice,
                    OldWholesalePrice = 0,
                    NewWholesalePrice = presDto.WholesalePrice,
                    OldCost = 0,
                    NewCost = presDto.Cost,
                    ChangedBy = _currentUserService.UserId ?? "System",
                    ChangedAt = DateTime.UtcNow,
                    Reason = "Creación de presentación"
                };
                await _priceHistoryRepository.AddAsync(history);
            }
        }

        // Soft delete presentations not in request
        foreach (var p in product.Presentations.ToList())
        {
            if (!presentationsToKeep.Contains(p.Id))
            {
                var hasMovements = await PresentationHasMovementsAsync(p.Id);
                if (hasMovements)
                {
                    throw new InvalidOperationException($"No se permite eliminar la presentación '{p.Name}' porque ya registra movimientos de inventario históricos. Debe quedar como inactiva (IsActive = false).");
                }
                
                // Impedir eliminar presentación base
                if (p.IsBaseUnit)
                {
                    throw new InvalidOperationException("No se permite eliminar la presentación base del producto.");
                }

                p.IsActive = false;
                p.IsDeleted = true; // Soft delete
            }
        }

        // Sync Branch Overrides
        product.BranchProducts.Clear();
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

        _productRepository.Update(product);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return true;
    }
}
