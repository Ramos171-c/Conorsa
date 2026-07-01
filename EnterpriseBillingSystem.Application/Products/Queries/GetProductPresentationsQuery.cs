using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using EnterpriseBillingSystem.Domain.Repositories;
using EnterpriseBillingSystem.Application.Products.DTOs;

namespace EnterpriseBillingSystem.Application.Products.Queries;

public record GetProductPresentationsQuery(Guid ProductId) : IRequest<IEnumerable<ProductPresentationDto>>;

public class GetProductPresentationsQueryHandler : IRequestHandler<GetProductPresentationsQuery, IEnumerable<ProductPresentationDto>>
{
    private readonly IProductPresentationRepository _presentationRepository;

    public GetProductPresentationsQueryHandler(IProductPresentationRepository presentationRepository)
    {
        _presentationRepository = presentationRepository;
    }

    public async Task<IEnumerable<ProductPresentationDto>> Handle(GetProductPresentationsQuery request, CancellationToken cancellationToken)
    {
        var list = await _presentationRepository.GetByProductIdAsync(request.ProductId, cancellationToken);
        return list.Select(p => new ProductPresentationDto(
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
        )).ToList();
    }
}
