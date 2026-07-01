using System;
using EnterpriseBillingSystem.Domain.Enums;

namespace EnterpriseBillingSystem.Application.Inventory.DTOs;

public record InventoryMovementDto(
    Guid Id,
    string MovementNumber,
    MovementType MovementType,
    string MovementTypeName,
    Guid? FromBranchWarehouseId,
    string? FromWarehouseName,
    Guid? ToBranchWarehouseId,
    string? ToWarehouseName,
    string? ReferenceDocument,
    string? Notes,
    DateTime MovementDate
);
