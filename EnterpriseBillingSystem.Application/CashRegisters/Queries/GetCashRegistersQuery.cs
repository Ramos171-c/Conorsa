using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using EnterpriseBillingSystem.Application.Common.Models;
using EnterpriseBillingSystem.Domain.Repositories;

namespace EnterpriseBillingSystem.Application.CashRegisters.Queries;

// ─── DTO ──────────────────────────────────────────────────────────────────────

public record CashRegisterDto(
    Guid Id,
    string Code,
    string Name,
    Guid? BranchId,
    bool IsDefault,
    bool IsActive
);

// ─── Queries ─────────────────────────────────────────────────────────────────

public record GetCashRegistersQuery(
    Guid? BranchId,
    string? SearchTerm,
    int PageNumber = 1,
    int PageSize = 20
) : IRequest<PagedResult<CashRegisterDto>>;

public record GetCashRegisterByIdQuery(Guid Id) : IRequest<CashRegisterDto?>;

// ─── Handlers ────────────────────────────────────────────────────────────────

public class GetCashRegistersQueryHandler : IRequestHandler<GetCashRegistersQuery, PagedResult<CashRegisterDto>>
{
    private readonly ICashRegisterRepository _repository;

    public GetCashRegistersQueryHandler(ICashRegisterRepository repository)
    {
        _repository = repository;
    }

    public async Task<PagedResult<CashRegisterDto>> Handle(GetCashRegistersQuery request, CancellationToken cancellationToken)
    {
        var (items, totalCount) = await _repository.GetPagedAsync(
            request.BranchId, request.SearchTerm, request.PageNumber, request.PageSize, cancellationToken);

        var dtos = items.Select(r => new CashRegisterDto(
            r.Id, r.Code, r.Name, r.BranchId, r.IsDefault, r.IsActive));

        return new PagedResult<CashRegisterDto>(dtos.ToList(), totalCount, request.PageNumber, request.PageSize);
    }
}

public class GetCashRegisterByIdQueryHandler : IRequestHandler<GetCashRegisterByIdQuery, CashRegisterDto?>
{
    private readonly ICashRegisterRepository _repository;

    public GetCashRegisterByIdQueryHandler(ICashRegisterRepository repository)
    {
        _repository = repository;
    }

    public async Task<CashRegisterDto?> Handle(GetCashRegisterByIdQuery request, CancellationToken cancellationToken)
    {
        var register = await _repository.GetByIdAsync(request.Id);
        if (register == null) return null;

        return new CashRegisterDto(
            register.Id, register.Code, register.Name, register.BranchId, register.IsDefault, register.IsActive);
    }
}
