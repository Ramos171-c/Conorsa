using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using EnterpriseBillingSystem.Application.Common.Models;
using EnterpriseBillingSystem.Domain.Repositories;

namespace EnterpriseBillingSystem.Application.PaymentMethods.Queries;

// ─── DTO ──────────────────────────────────────────────────────────────────────

public record PaymentMethodDto(
    Guid Id,
    string Code,
    string Name,
    bool IsCash,
    bool IsActive
);

// ─── Queries ─────────────────────────────────────────────────────────────────

public record GetPaymentMethodsQuery(
    string? SearchTerm,
    int PageNumber = 1,
    int PageSize = 20
) : IRequest<PagedResult<PaymentMethodDto>>;

public record GetPaymentMethodByIdQuery(Guid Id) : IRequest<PaymentMethodDto?>;

// ─── Handlers ────────────────────────────────────────────────────────────────

public class GetPaymentMethodsQueryHandler : IRequestHandler<GetPaymentMethodsQuery, PagedResult<PaymentMethodDto>>
{
    private readonly IPaymentMethodRepository _repository;

    public GetPaymentMethodsQueryHandler(IPaymentMethodRepository repository)
    {
        _repository = repository;
    }

    public async Task<PagedResult<PaymentMethodDto>> Handle(GetPaymentMethodsQuery request, CancellationToken cancellationToken)
    {
        var (items, totalCount) = await _repository.GetPagedAsync(
            request.SearchTerm, request.PageNumber, request.PageSize, cancellationToken);

        var dtos = items.Select(m => new PaymentMethodDto(
            m.Id, m.Code, m.Name, m.IsCash, m.IsActive));

        return new PagedResult<PaymentMethodDto>(dtos.ToList(), totalCount, request.PageNumber, request.PageSize);
    }
}

public class GetPaymentMethodByIdQueryHandler : IRequestHandler<GetPaymentMethodByIdQuery, PaymentMethodDto?>
{
    private readonly IPaymentMethodRepository _repository;

    public GetPaymentMethodByIdQueryHandler(IPaymentMethodRepository repository)
    {
        _repository = repository;
    }

    public async Task<PaymentMethodDto?> Handle(GetPaymentMethodByIdQuery request, CancellationToken cancellationToken)
    {
        var method = await _repository.GetByIdAsync(request.Id);
        if (method == null) return null;

        return new PaymentMethodDto(
            method.Id, method.Code, method.Name, method.IsCash, method.IsActive);
    }
}
