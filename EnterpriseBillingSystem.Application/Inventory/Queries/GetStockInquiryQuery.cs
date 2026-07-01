using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using EnterpriseBillingSystem.Application.Common.Models;
using EnterpriseBillingSystem.Application.Inventory.DTOs;
using EnterpriseBillingSystem.Domain.Repositories;

namespace EnterpriseBillingSystem.Application.Inventory.Queries;

public record GetStockInquiryQuery(
    Guid? BranchWarehouseId,
    Guid? ProductId,
    int PageNumber = 1,
    int PageSize = 10
) : IRequest<PagedResult<InventoryDto>>;

public class GetStockInquiryQueryHandler : IRequestHandler<GetStockInquiryQuery, PagedResult<InventoryDto>>
{
    private readonly IInventoryRepository _inventoryRepository;

    public GetStockInquiryQueryHandler(IInventoryRepository inventoryRepository)
    {
        _inventoryRepository = inventoryRepository;
    }

    public async Task<PagedResult<InventoryDto>> Handle(GetStockInquiryQuery request, CancellationToken cancellationToken)
    {
        var (items, totalCount) = await _inventoryRepository.GetStockInquiryAsync(
            request.BranchWarehouseId,
            request.ProductId,
            request.PageNumber,
            request.PageSize,
            cancellationToken);

        var dtos = items.Select(i => new InventoryDto(
            Id: i.Id,
            BranchWarehouseId: i.BranchWarehouseId,
            WarehouseCode: i.BranchWarehouse.Warehouse.Code,
            WarehouseName: i.BranchWarehouse.Warehouse.Name,
            ProductId: i.ProductId,
            ProductName: i.Product.Name,
            ProductInternalCode: i.Product.InternalCode,
            PhysicalStock: i.PhysicalStock,
            ReservedStock: i.ReservedStock,
            CommittedStock: i.CommittedStock,
            AvailableStock: i.AvailableStock
        )).ToList();

        return new PagedResult<InventoryDto>(
            dtos,
            totalCount,
            request.PageNumber,
            request.PageSize
        );
    }
}
