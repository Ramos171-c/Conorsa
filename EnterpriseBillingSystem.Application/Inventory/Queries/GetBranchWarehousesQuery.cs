using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using EnterpriseBillingSystem.Application.Common.Interfaces;
using EnterpriseBillingSystem.Application.Inventory.DTOs;
using EnterpriseBillingSystem.Domain.Entities;
using EnterpriseBillingSystem.Domain.Repositories;

namespace EnterpriseBillingSystem.Application.Inventory.Queries;

public record GetBranchWarehousesQuery() : IRequest<IEnumerable<WarehouseDto>>;

public class GetBranchWarehousesQueryHandler : IRequestHandler<GetBranchWarehousesQuery, IEnumerable<WarehouseDto>>
{
    private readonly IWarehouseRepository _warehouseRepository;
    private readonly ICurrentUserService _currentUserService;

    public GetBranchWarehousesQueryHandler(
        IWarehouseRepository warehouseRepository,
        ICurrentUserService currentUserService)
    {
        _warehouseRepository = warehouseRepository;
        _currentUserService = currentUserService;
    }

    public async Task<IEnumerable<WarehouseDto>> Handle(GetBranchWarehousesQuery request, CancellationToken cancellationToken)
    {
        var branchId = _currentUserService.BranchId;
        if (branchId == null)
        {
            return Array.Empty<WarehouseDto>();
        }

        var branchWarehouses = await _warehouseRepository.GetBranchWarehousesAsync(branchId.Value, cancellationToken);
        
        return branchWarehouses.Select(bw => new WarehouseDto(
            Id: bw.Id, // Note: We return BranchWarehouse.Id as the WarehouseDto Id so that the WPF app can bind directly to it
            Code: bw.Warehouse.Code,
            Name: bw.Warehouse.Name,
            Description: bw.Warehouse.Description,
            IsActive: bw.Warehouse.IsActive
        )).ToList();
    }
}
