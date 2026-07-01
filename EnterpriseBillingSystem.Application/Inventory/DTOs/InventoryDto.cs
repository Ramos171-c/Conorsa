using System;

namespace EnterpriseBillingSystem.Application.Inventory.DTOs;

public record InventoryDto(
    Guid Id,
    Guid BranchWarehouseId,
    string WarehouseCode,
    string WarehouseName,
    Guid ProductId,
    string ProductName,
    string ProductInternalCode,
    decimal PhysicalStock,
    decimal ReservedStock,
    decimal CommittedStock,
    decimal AvailableStock
);
