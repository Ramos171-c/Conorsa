using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using EnterpriseBillingSystem.Domain.Repositories;

namespace EnterpriseBillingSystem.Application.Products.Queries;

public record LowStockProductDto(
    string Code,
    string Name,
    decimal CurrentStock,
    decimal MinimumStock
);

public record GetLowStockProductsQuery() : IRequest<IEnumerable<LowStockProductDto>>;

public class GetLowStockProductsQueryHandler : IRequestHandler<GetLowStockProductsQuery, IEnumerable<LowStockProductDto>>
{
    private readonly IInventoryRepository _inventoryRepository;

    public GetLowStockProductsQueryHandler(IInventoryRepository inventoryRepository)
    {
        _inventoryRepository = inventoryRepository;
    }

    public async Task<IEnumerable<LowStockProductDto>> Handle(GetLowStockProductsQuery request, CancellationToken cancellationToken)
    {
        var lowStockItems = await _inventoryRepository.GetLowStockItemsAsync(cancellationToken);
        return lowStockItems.Select(x => new LowStockProductDto(
            Code: x.Product.InternalCode,
            Name: x.Product.Name,
            CurrentStock: x.CurrentStock,
            MinimumStock: x.Product.MinimumStock
        )).ToList();
    }
}
