using System;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using EnterpriseBillingSystem.Domain.Repositories;
using EnterpriseBillingSystem.Application.Products.DTOs;

namespace EnterpriseBillingSystem.Application.Products.Queries;

public record GetProductPresentationByBarcodeQuery(string Barcode) : IRequest<ProductPresentationDto?>;

public class GetProductPresentationByBarcodeQueryHandler : IRequestHandler<GetProductPresentationByBarcodeQuery, ProductPresentationDto?>
{
    private readonly IProductPresentationRepository _presentationRepository;

    public GetProductPresentationByBarcodeQueryHandler(IProductPresentationRepository presentationRepository)
    {
        _presentationRepository = presentationRepository;
    }

    public async Task<ProductPresentationDto?> Handle(GetProductPresentationByBarcodeQuery request, CancellationToken cancellationToken)
    {
        var p = await _presentationRepository.GetByBarcodeAsync(request.Barcode, cancellationToken);
        if (p == null) return null;

        return new ProductPresentationDto(
            Id: p.Id,
            ProductId: p.ProductId,
            ProductName: p.Product?.Name ?? string.Empty,
            ProductInternalCode: p.Product?.InternalCode ?? string.Empty,
            TaxPercentage: p.Product?.Tax?.Rate ?? 0m,
            UnitOfMeasureId: p.UnitOfMeasureId,
            UnitOfMeasureCode: p.UnitOfMeasure.Code,
            Name: p.Name,
            ConversionFactor: p.ConversionFactor,
            Barcode: p.Barcode,
            Cost: p.Cost,
            RetailPrice: p.RetailPrice,
            SemiWholesalePrice: p.SemiWholesalePrice,
            WholesalePrice: p.WholesalePrice,
            IsBaseUnit: p.IsBaseUnit,
            IsDefaultSalePresentation: p.IsDefaultSalePresentation,
            AllowPurchase: p.AllowPurchase,
            AllowSale: p.AllowSale,
            IsActive: p.IsActive
        );
    }
}
