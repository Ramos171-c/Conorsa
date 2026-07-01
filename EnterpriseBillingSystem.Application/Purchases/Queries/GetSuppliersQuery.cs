using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using EnterpriseBillingSystem.Domain.Entities;
using EnterpriseBillingSystem.Domain.Repositories;

namespace EnterpriseBillingSystem.Application.Purchases.Queries;

// ─── DTOs de respuesta ───────────────────────────────────────────────────────

public record SupplierListItemDto(
    Guid Id,
    string SupplierCode,
    string IdentificationNumber,
    string IdentificationType,
    string Name,
    string? LegalName,
    string CategoryName,
    string? Phone,
    string? Email,
    string Status
);

public record SupplierDetailDto(
    Guid Id,
    string SupplierCode,
    string IdentificationNumber,
    string IdentificationType,
    string Name,
    string? LegalName,
    Guid SupplierCategoryId,
    string CategoryName,
    string? Phone,
    string? Email,
    string? Address,
    string? ContactName,
    string Status,
    bool IsActive,
    DateTime CreatedOnUtc
);

public record PagedResult<T>(IEnumerable<T> Items, int TotalCount, int PageNumber, int PageSize);

// ─── Queries ─────────────────────────────────────────────────────────────────

public record GetSuppliersQuery(
    string? Search,
    Guid? CategoryId,
    string? Status,
    int PageNumber = 1,
    int PageSize = 20
) : IRequest<PagedResult<SupplierListItemDto>>;

public record GetSupplierByIdQuery(Guid SupplierId) : IRequest<SupplierDetailDto?>;

// ─── Handlers ────────────────────────────────────────────────────────────────

public class GetSuppliersQueryHandler : IRequestHandler<GetSuppliersQuery, PagedResult<SupplierListItemDto>>
{
    private readonly ISupplierRepository _supplierRepository;

    public GetSuppliersQueryHandler(ISupplierRepository supplierRepository)
    {
        _supplierRepository = supplierRepository;
    }

    public async Task<PagedResult<SupplierListItemDto>> Handle(GetSuppliersQuery request, CancellationToken cancellationToken)
    {
        var (items, totalCount) = await _supplierRepository.GetPagedAsync(
            request.Search,
            request.CategoryId,
            request.Status,
            request.PageNumber,
            request.PageSize,
            cancellationToken);

        var dtos = items.Select(s => new SupplierListItemDto(
            s.Id,
            s.SupplierCode,
            s.IdentificationNumber,
            s.IdentificationType.ToString(),
            s.Name,
            s.LegalName,
            s.SupplierCategory?.Name ?? string.Empty,
            s.Phone,
            s.Email,
            s.Status.ToString()
        ));

        return new PagedResult<SupplierListItemDto>(dtos, totalCount, request.PageNumber, request.PageSize);
    }
}

public class GetSupplierByIdQueryHandler : IRequestHandler<GetSupplierByIdQuery, SupplierDetailDto?>
{
    private readonly ISupplierRepository _supplierRepository;

    public GetSupplierByIdQueryHandler(ISupplierRepository supplierRepository)
    {
        _supplierRepository = supplierRepository;
    }

    public async Task<SupplierDetailDto?> Handle(GetSupplierByIdQuery request, CancellationToken cancellationToken)
    {
        var entity = await _supplierRepository.GetByIdAsync(request.SupplierId);
        if (entity == null) return null;

        return new SupplierDetailDto(
            entity.Id,
            entity.SupplierCode,
            entity.IdentificationNumber,
            entity.IdentificationType.ToString(),
            entity.Name,
            entity.LegalName,
            entity.SupplierCategoryId,
            entity.SupplierCategory?.Name ?? string.Empty,
            entity.Phone,
            entity.Email,
            entity.Address,
            entity.ContactName,
            entity.Status.ToString(),
            entity.IsActive,
            entity.CreatedOnUtc
        );
    }
}
