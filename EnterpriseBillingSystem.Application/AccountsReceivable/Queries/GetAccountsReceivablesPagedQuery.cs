using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using EnterpriseBillingSystem.Application.Common.Models;
using EnterpriseBillingSystem.Domain.Repositories;

namespace EnterpriseBillingSystem.Application.AccountsReceivable.Queries;

public record AccountsReceivableDto(
    Guid Id,
    Guid CustomerId,
    string CustomerName,
    string CustomerCode,
    Guid SalesInvoiceId,
    string DocumentNumber,
    DateTime InvoiceDate,
    DateTime DueDate,
    decimal OriginalAmount,
    decimal PaidAmount,
    decimal CurrentBalance,
    string Status,
    DateTime? LastPaymentDate,
    string? Notes
);

public record GetAccountsReceivablesPagedQuery(
    Guid? CustomerId,
    string? Status,
    DateTime? StartDate,
    DateTime? EndDate,
    bool? IsOverdue,
    bool? IsPending,
    int PageNumber = 1,
    int PageSize = 20
) : IRequest<PagedResult<AccountsReceivableDto>>;

public class GetAccountsReceivablesPagedQueryHandler : IRequestHandler<GetAccountsReceivablesPagedQuery, PagedResult<AccountsReceivableDto>>
{
    private readonly IAccountsReceivableRepository _arRepository;

    public GetAccountsReceivablesPagedQueryHandler(IAccountsReceivableRepository arRepository)
    {
        _arRepository = arRepository;
    }

    public async Task<PagedResult<AccountsReceivableDto>> Handle(GetAccountsReceivablesPagedQuery request, CancellationToken cancellationToken)
    {
        var (items, totalCount) = await _arRepository.GetPagedAsync(
            request.CustomerId,
            request.Status,
            request.StartDate,
            request.EndDate,
            request.IsOverdue,
            request.IsPending,
            request.PageNumber,
            request.PageSize,
            cancellationToken);

        var dtos = items.Select(a => new AccountsReceivableDto(
            a.Id,
            a.CustomerId,
            a.Customer?.Name ?? "N/A",
            a.Customer?.CustomerCode ?? "N/A",
            a.SalesInvoiceId,
            a.DocumentNumber,
            a.InvoiceDate,
            a.DueDate,
            a.OriginalAmount,
            a.PaidAmount,
            a.CurrentBalance,
            a.Status.ToString(),
            a.LastPaymentDate,
            a.Notes
        )).ToList();

        return new PagedResult<AccountsReceivableDto>(dtos, totalCount, request.PageNumber, request.PageSize);
    }
}
