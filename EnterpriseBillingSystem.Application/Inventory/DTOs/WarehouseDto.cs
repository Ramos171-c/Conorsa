using System;

namespace EnterpriseBillingSystem.Application.Inventory.DTOs;

public record WarehouseDto(
    Guid Id,
    string Code,
    string Name,
    string? Description,
    bool IsActive
);
