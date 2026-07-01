using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using EnterpriseBillingSystem.Domain.Repositories;
using EnterpriseBillingSystem.Application.Products.DTOs;
using EnterpriseBillingSystem.Application.Taxes.DTOs;

namespace EnterpriseBillingSystem.Application.Products.Queries;

public record GetCatalogProductsQuery() : IRequest<IEnumerable<ProductDto>>;

public class GetCatalogProductsQueryHandler : IRequestHandler<GetCatalogProductsQuery, IEnumerable<ProductDto>>
{
    private readonly IProductRepository _productRepository;

    public GetCatalogProductsQueryHandler(IProductRepository productRepository)
    {
        _productRepository = productRepository;
    }

    public async Task<IEnumerable<ProductDto>> Handle(GetCatalogProductsQuery request, CancellationToken cancellationToken)
    {
        var products = await _productRepository.GetCatalogProductsAsync(cancellationToken);
        return products.Select(product =>
        {
            var taxDtos = product.Tax != null
                ? new List<TaxDto> { new TaxDto(product.Tax.Id, product.Tax.Name, product.Tax.Rate, product.Tax.IsActive) }
                : new List<TaxDto>();

            var presentationDtos = product.Presentations
                .Where(pr => pr.IsActive && !pr.IsDeleted)
                .Select(pr => new ProductPresentationDto(
                    Id: pr.Id,
                    ProductId: pr.ProductId,
                    ProductName: product.Name,
                    ProductInternalCode: product.InternalCode,
                    TaxPercentage: product.Tax?.Rate ?? 0m,
                    UnitOfMeasureId: pr.UnitOfMeasureId,
                    UnitOfMeasureCode: pr.UnitOfMeasure.Code,
                    Name: pr.Name,
                    ConversionFactor: pr.ConversionFactor,
                    Barcode: pr.Barcode,
                    Cost: pr.Cost,
                    RetailPrice: pr.RetailPrice,
                    SemiWholesalePrice: pr.SemiWholesalePrice,
                    WholesalePrice: pr.WholesalePrice,
                    IsBaseUnit: pr.IsBaseUnit,
                    IsDefaultSalePresentation: pr.IsDefaultSalePresentation,
                    AllowPurchase: pr.AllowPurchase,
                    AllowSale: pr.AllowSale,
                    IsActive: pr.IsActive
                ))
                .ToList();

            var defaultPresentation = presentationDtos.FirstOrDefault(pr => pr.IsDefaultSalePresentation)
                ?? presentationDtos.FirstOrDefault();

            var branchProductDtos = product.BranchProducts
                .Select(bp => new BranchProductDto(
                    bp.BranchId,
                    bp.Branch.Name,
                    bp.LocalSalePrice,
                    bp.MinSalePrice,
                    bp.MaxDiscountPercentage,
                    bp.IsActive))
                .ToList();

            return new ProductDto(
                Id: product.Id,
                InternalCode: product.InternalCode,
                Barcode: defaultPresentation?.Barcode,
                Name: product.Name,
                Description: product.Description,
                ProductType: product.ProductType,
                ProductStatus: product.ProductStatus,
                TrackInventory: product.TrackInventory,
                RequiresSerialNumber: product.RequiresSerialNumber,
                RequiresBatchControl: product.RequiresBatchControl,
                CategoryId: product.CategoryId,
                CategoryName: product.Category.Name,
                BrandId: product.BrandId,
                BrandName: product.Brand?.Name,
                DefaultUnitOfMeasureId: product.DefaultUnitOfMeasureId,
                DefaultUnitOfMeasureCode: product.DefaultUnitOfMeasure.Code,
                DefaultPurchasePrice: defaultPresentation?.Cost ?? 0m,
                DefaultSalePrice: defaultPresentation?.RetailPrice ?? 0m,
                CurrentCost: product.CurrentCost,
                ImagePath: product.ImagePath,
                IsCatalogVisible: product.IsCatalogVisible,
                IsSoldOut: product.IsSoldOut,
                SoldOutAt: product.SoldOutAt,
                SoldOutBy: product.SoldOutBy,
                MinimumStock: product.MinimumStock,
                IsFavorite: product.IsFavorite,
                FavoriteOrder: product.FavoriteOrder,
                AllowPromotions: product.AllowPromotions,
                HighlightInCatalog: product.HighlightInCatalog,
                ShortDescription: product.ShortDescription,
                CatalogBadge: product.CatalogBadge,
                DisplayOrder: product.DisplayOrder,
                AutoMarkSoldOut: product.AutoMarkSoldOut,
                IsActive: product.IsActive,
                Presentations: presentationDtos,
                DefaultPresentation: defaultPresentation,
                DefaultPrice: defaultPresentation?.RetailPrice ?? 0m,
                ImageUrl: product.ImagePath,
                Availability: product.IsSoldOut ? "Sold Out" : "Available",
                Taxes: taxDtos,
                BranchProducts: branchProductDtos
            );
        }).ToList();
    }
}
