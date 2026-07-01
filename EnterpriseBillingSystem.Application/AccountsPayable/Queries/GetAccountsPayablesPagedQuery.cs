using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using EnterpriseBillingSystem.Application.Common.Models;
using EnterpriseBillingSystem.Domain.Repositories;

namespace EnterpriseBillingSystem.Application.AccountsPayable.Queries;

public record AccountsPayableDto(
    Guid Id,
    Guid SupplierId,
    string SupplierName,
    string SupplierCode,
    Guid PurchaseInvoiceId,
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

public record GetAccountsPayablesPagedQuery(
    Guid? SupplierId,
    string? Status,
    DateTime? StartDate,
    DateTime? EndDate,
    bool? IsOverdue,
    bool? isPending,
    int PageNumber = 1,
    int PageSize = 20
) : IRequest<PagedResult<AccountsPayableDto>>;

public class GetAccountsPayablesPagedQueryHandler : IRequestHandler<GetAccountsPayablesPagedQuery, PagedResult<AccountsPayableDto>>
{
    private readonly IAccountsPayableRepository _apRepository;

    public GetAccountsPayablesPagedQueryHandler(IAccountsPayableRepository apRepository)
    {
        _apRepository = apRepository;
    }

    public async Task<PagedResult<AccountsPayableDto>> Handle(GetAccountsPayablesPagedQuery request, CancellationToken cancellationToken)
    {
        var (items, totalCount) = await _apRepository.GetPagedAsync(
            request.SupplierId,
            request.Status,
            request.StartDate,
            request.EndDate,
            request.IsOverdue,
            request.isPending,
            request.PageNumber,
            request.PageSize,
            cancellationToken);

        var dtos = items.Select(a => new AccountsPayableDto(
            a.Id,
            a.SupplierId,
            a.Supplier?.Name ?? "N/A",
            a.Supplier?.SupplierCode ?? "N/A",
            a.PurchaseInvoiceId,
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

        return new PagedResult<AccountsPayableDto>(dtos, totalCount, request.PageNumber, request.PageSize);
    }
}
