using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using EnterpriseBillingSystem.Application.Common.Models;
using EnterpriseBillingSystem.Application.Inventory.DTOs;
using EnterpriseBillingSystem.Domain.Enums;
using EnterpriseBillingSystem.Domain.Repositories;

namespace EnterpriseBillingSystem.Application.Inventory.Queries;

public record GetKardexQuery(
    Guid BranchWarehouseId,
    Guid ProductId,
    DateTime? StartDate = null,
    DateTime? EndDate = null,
    int PageNumber = 1,
    int PageSize = 10
) : IRequest<PagedResult<KardexDto>>;

public class GetKardexQueryHandler : IRequestHandler<GetKardexQuery, PagedResult<KardexDto>>
{
    private readonly IInventoryMovementRepository _movementRepository;

    public GetKardexQueryHandler(IInventoryMovementRepository movementRepository)
    {
        _movementRepository = movementRepository;
    }

    public async Task<PagedResult<KardexDto>> Handle(GetKardexQuery request, CancellationToken cancellationToken)
    {
        var (items, totalCount) = await _movementRepository.GetKardexAsync(
            request.BranchWarehouseId,
            request.ProductId,
            request.StartDate,
            request.EndDate,
            request.PageNumber,
            request.PageSize,
            cancellationToken);

        var dtos = items.Select(d =>
        {
            var movement = d.InventoryMovement;
            
            // Determinar si es entrada o salida para la bodega consultada
            bool isEntry = false;
            if (movement.MovementType == MovementType.Entry || 
                movement.MovementType == MovementType.PositiveAdjustment ||
                movement.MovementType == MovementType.TransferIn)
            {
                isEntry = true;
            }
            else if (movement.MovementType == MovementType.Exit || 
                     movement.MovementType == MovementType.NegativeAdjustment ||
                     movement.MovementType == MovementType.TransferOut)
            {
                // Si es un TransferOut, es salida para la bodega origen (From), y entrada para la bodega destino (To)
                isEntry = movement.ToBranchWarehouseId == request.BranchWarehouseId;
            }

            // Nombre amigable del tipo de movimiento
            string movementTypeName = movement.MovementType switch
            {
                MovementType.Entry => "Entrada",
                MovementType.Exit => "Salida",
                MovementType.PositiveAdjustment => "Ajuste Positivo",
                MovementType.NegativeAdjustment => "Ajuste Negativo",
                MovementType.TransferOut => movement.ToBranchWarehouseId == request.BranchWarehouseId 
                    ? "Transferencia - Entrada" 
                    : "Transferencia - Salida",
                MovementType.TransferIn => "Transferencia - Entrada",
                _ => movement.MovementType.ToString()
            };

            return new KardexDto(
                DetailId: d.Id,
                MovementId: movement.Id,
                MovementNumber: movement.MovementNumber,
                MovementType: movement.MovementType,
                MovementTypeName: movementTypeName,
                MovementDate: movement.MovementDate,
                ReferenceDocument: movement.ReferenceDocument,
                Notes: movement.Notes,
                Quantity: d.Quantity,
                UnitOfMeasureCode: d.UnitOfMeasure.Code,
                ConversionFactor: d.ConversionFactor,
                QuantityInBaseUnit: d.QuantityInBaseUnit,
                IsEntry: isEntry
            );
        }).ToList();

        return new PagedResult<KardexDto>(
            dtos,
            totalCount,
            request.PageNumber,
            request.PageSize
        );
    }
}
