using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using EnterpriseBillingSystem.Application.Common.Models;
using EnterpriseBillingSystem.Domain.Entities;
using EnterpriseBillingSystem.Domain.Repositories;

namespace EnterpriseBillingSystem.Application.CashSessions.Queries;

// ─── DTOs ────────────────────────────────────────────────────────────────────

public record CashMovementDto(
    Guid Id,
    Guid CashSessionId,
    string MovementType,
    Guid PaymentMethodId,
    string PaymentMethodName,
    string? ReferenceDocument,
    Guid? ReferenceId,
    decimal Amount,
    string? Notes,
    string? Reason,
    DateTime CreatedAt
);

public record CashSessionListItemDto(
    Guid Id,
    string SessionNumber,
    Guid CashRegisterId,
    string CashRegisterName,
    Guid OpenedByUserId,
    string OpenedByUserName,
    decimal OpeningAmount,
    decimal? ClosingAmount,
    decimal? ExpectedAmount,
    decimal? DifferenceAmount,
    DateTime OpenedAt,
    DateTime? ClosedAt,
    string Status
);

public record CashSessionDetailDto(
    Guid Id,
    string SessionNumber,
    Guid CashRegisterId,
    string CashRegisterName,
    Guid OpenedByUserId,
    string OpenedByUserName,
    Guid? ClosedByUserId,
    string? ClosedByUserName,
    decimal OpeningAmount,
    decimal? ClosingAmount,
    decimal? ExpectedAmount,
    decimal? DifferenceAmount,
    DateTime OpenedAt,
    DateTime? ClosedAt,
    string Status,
    string? Notes,
    List<CashMovementDto> Movements
);

// ─── Queries ─────────────────────────────────────────────────────────────────

public record GetCashSessionsQuery(
    Guid? CashRegisterId,
    string? Status,
    int PageNumber = 1,
    int PageSize = 20
) : IRequest<PagedResult<CashSessionListItemDto>>;

public record GetCashSessionByIdQuery(Guid Id) : IRequest<CashSessionDetailDto?>;

public record GetCashMovementsQuery(
    Guid CashSessionId
) : IRequest<List<CashMovementDto>>;

// ─── Handlers ────────────────────────────────────────────────────────────────

public class GetCashSessionsQueryHandler : IRequestHandler<GetCashSessionsQuery, PagedResult<CashSessionListItemDto>>
{
    private readonly ICashSessionRepository _repository;

    public GetCashSessionsQueryHandler(ICashSessionRepository repository)
    {
        _repository = repository;
    }

    public async Task<PagedResult<CashSessionListItemDto>> Handle(GetCashSessionsQuery request, CancellationToken cancellationToken)
    {
        var (items, totalCount) = await _repository.GetPagedAsync(
            request.CashRegisterId, request.Status, request.PageNumber, request.PageSize, cancellationToken);

        var dtos = items.Select(s => new CashSessionListItemDto(
            s.Id,
            s.SessionNumber,
            s.CashRegisterId,
            s.CashRegister?.Name ?? "N/A",
            s.OpenedByUserId,
            s.OpenedByUser?.UserName ?? "N/A",
            s.OpeningAmount,
            s.ClosingAmount,
            s.ExpectedAmount,
            s.DifferenceAmount,
            s.OpenedAt,
            s.ClosedAt,
            s.Status.ToString()));

        return new PagedResult<CashSessionListItemDto>(dtos.ToList(), totalCount, request.PageNumber, request.PageSize);
    }
}

public class GetCashSessionByIdQueryHandler : IRequestHandler<GetCashSessionByIdQuery, CashSessionDetailDto?>
{
    private readonly ICashSessionRepository _repository;

    public GetCashSessionByIdQueryHandler(ICashSessionRepository repository)
    {
        _repository = repository;
    }

    public async Task<CashSessionDetailDto?> Handle(GetCashSessionByIdQuery request, CancellationToken cancellationToken)
    {
        var session = await _repository.GetByIdWithDetailsAsync(request.Id, cancellationToken);
        if (session == null) return null;

        var movements = session.CashMovements.Select(m => new CashMovementDto(
            m.Id,
            m.CashSessionId,
            m.MovementType.ToString(),
            m.PaymentMethodId,
            m.PaymentMethod?.Name ?? "N/A",
            m.ReferenceDocument,
            m.ReferenceId,
            m.Amount,
            m.Notes,
            m.Reason,
            m.CreatedAt)).ToList();

        return new CashSessionDetailDto(
            session.Id,
            session.SessionNumber,
            session.CashRegisterId,
            session.CashRegister?.Name ?? "N/A",
            session.OpenedByUserId,
            session.OpenedByUser?.UserName ?? "N/A",
            session.ClosedByUserId,
            session.ClosedByUser?.UserName,
            session.OpeningAmount,
            session.ClosingAmount,
            session.ExpectedAmount,
            session.DifferenceAmount,
            session.OpenedAt,
            session.ClosedAt,
            session.Status.ToString(),
            session.Notes,
            movements);
    }
}

public class GetCashMovementsQueryHandler : IRequestHandler<GetCashMovementsQuery, List<CashMovementDto>>
{
    private readonly ICashSessionRepository _repository;

    public GetCashMovementsQueryHandler(ICashSessionRepository repository)
    {
        _repository = repository;
    }

    public async Task<List<CashMovementDto>> Handle(GetCashMovementsQuery request, CancellationToken cancellationToken)
    {
        var session = await _repository.GetByIdWithDetailsAsync(request.CashSessionId, cancellationToken);
        if (session == null) return new List<CashMovementDto>();

        return session.CashMovements
            .OrderBy(m => m.CreatedAt)
            .Select(m => new CashMovementDto(
                m.Id,
                m.CashSessionId,
                m.MovementType.ToString(),
                m.PaymentMethodId,
                m.PaymentMethod?.Name ?? "N/A",
                m.ReferenceDocument,
                m.ReferenceId,
                m.Amount,
                m.Notes,
                m.Reason,
                m.CreatedAt))
            .ToList();
    }
}
