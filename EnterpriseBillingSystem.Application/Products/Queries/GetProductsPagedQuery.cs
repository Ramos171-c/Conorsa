using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using Microsoft.AspNetCore.Identity;
using EnterpriseBillingSystem.Domain.Entities;
using EnterpriseBillingSystem.Domain.Repositories;
using EnterpriseBillingSystem.Application.Products.DTOs;
using EnterpriseBillingSystem.Application.Taxes.DTOs;
using EnterpriseBillingSystem.Application.Common.Models;
using EnterpriseBillingSystem.Application.Common.Interfaces;

namespace EnterpriseBillingSystem.Application.Products.Queries;

public record GetProductsPagedQuery(
    int PageNumber = 1,
    int PageSize = 10,
    string? SearchTerm = null,
    Guid? CategoryId = null,
    Guid? BrandId = null,
    bool? IsForPos = null
) : IRequest<PagedResult<ProductDto>>;

public class GetProductsPagedQueryHandler : IRequestHandler<GetProductsPagedQuery, PagedResult<ProductDto>>
{
    private readonly IProductRepository _productRepository;
    private readonly ICurrentUserService _currentUserService;
    private readonly UserManager<ApplicationUser> _userManager;

    public GetProductsPagedQueryHandler(
        IProductRepository productRepository,
        ICurrentUserService currentUserService,
        UserManager<ApplicationUser> userManager)
    {
        _productRepository = productRepository;
        _currentUserService = currentUserService;
        _userManager = userManager;
    }

    public async Task<PagedResult<ProductDto>> Handle(GetProductsPagedQuery request, CancellationToken cancellationToken)
    {
        var (items, totalCount) = await _productRepository.GetPagedAsync(
            request.PageNumber,
            request.PageSize,
            request.SearchTerm,
            request.CategoryId,
            request.BrandId,
            request.IsForPos,
            cancellationToken);

        bool isVendedor = false;
        bool isCostSeller = false;
        bool isDetailSeller = false;

        if (!string.IsNullOrWhiteSpace(_currentUserService.UserId))
        {
            var user = await _userManager.FindByIdAsync(_currentUserService.UserId)
                    ?? await _userManager.FindByNameAsync(_currentUserService.UserId);
            if (user != null)
            {
                isVendedor = await _userManager.IsInRoleAsync(user, "VENDEDOR");
                isCostSeller = isVendedor && user.SellerCategory == Domain.Enums.SellerCategory.Cost;
                isDetailSeller = isVendedor && user.SellerCategory == Domain.Enums.SellerCategory.Detail;
            }
        }

        var dtos = items.Select(product =>
        {
            var taxDtos = product.Tax != null
                ? new List<TaxDto> { new TaxDto(product.Tax.Id, product.Tax.Name, product.Tax.Rate, product.Tax.IsActive) }
                : new List<TaxDto>();

            var presentationDtos = product.Presentations
                .Where(pr => pr.IsActive && !pr.IsDeleted)
                .Select(pr =>
                {
                    decimal costSellerPrice = Math.Round(pr.Cost * 1.02m, 2);
                    return new ProductPresentationDto(
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
                        RetailPrice: isCostSeller ? costSellerPrice : pr.RetailPrice,
                        SemiWholesalePrice: isCostSeller ? costSellerPrice : pr.SemiWholesalePrice,
                        WholesalePrice: isCostSeller ? costSellerPrice : pr.WholesalePrice,
                        IsBaseUnit: pr.IsBaseUnit,
                        IsDefaultSalePresentation: pr.IsDefaultSalePresentation,
                        AllowPurchase: pr.AllowPurchase,
                        AllowSale: pr.AllowSale,
                        AllowDetailChannel: pr.AllowDetailChannel,
                        AllowCostChannel: pr.AllowCostChannel,
                        IsActive: pr.IsActive
                    );
                })
                .ToList();

            var defaultPresentation = presentationDtos.FirstOrDefault(pr => pr.IsDefaultSalePresentation)
                ?? presentationDtos.FirstOrDefault();

            decimal resolvedDefaultPrice = isCostSeller
                ? (defaultPresentation != null ? Math.Round(defaultPresentation.Cost * 1.02m, 2) : 0m)
                : (defaultPresentation?.RetailPrice ?? 0m);

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
                DefaultSalePrice: resolvedDefaultPrice,
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
                DefaultPrice: resolvedDefaultPrice,
                ImageUrl: product.ImagePath,
                Availability: product.IsSoldOut ? "Sold Out" : "Available",
                Taxes: taxDtos,
                BranchProducts: branchProductDtos
            );
        }).ToList();

        if (isVendedor)
        {
            // 1. Filtrar surtidos del sistema móvil
            dtos = dtos.Where(dto => 
                !dto.InternalCode.StartsWith("SURTIDO", StringComparison.OrdinalIgnoreCase)
            ).ToList();
        }

        // 2. Filtrar productos del canal Costo si el vendedor es de tipo Detalle
        if (isDetailSeller)
        {
            dtos = dtos.Where(dto => !dto.Presentations.Any(p => p.AllowCostChannel && !p.AllowDetailChannel)).ToList();
        }

        // 3. Filtrar cualquier producto sin presentaciones activas (para evitar productos vacíos o con precio 0)
        dtos = dtos.Where(dto => dto.Presentations != null && dto.Presentations.Any()).ToList();

        return new PagedResult<ProductDto>(dtos, totalCount, request.PageNumber, request.PageSize);
    }
}
