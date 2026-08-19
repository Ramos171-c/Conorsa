using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using EnterpriseBillingSystem.Application.Common.Models;
using EnterpriseBillingSystem.Domain.Entities;
using EnterpriseBillingSystem.Domain.Repositories;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace EnterpriseBillingSystem.Application.Sales.Queries;

// ─── DTOs ────────────────────────────────────────────────────────────────────

public record SalesOrderDetailItemDto(
    Guid Id,
    Guid ProductId,
    string ProductName,
    string ProductCode,
    Guid UnitOfMeasureId,
    string UnitOfMeasure,
    decimal Quantity,
    decimal UnitPrice,
    decimal DiscountPercentage,
    decimal DiscountAmount,
    decimal TaxPercentage,
    decimal TaxAmount,
    decimal NetAmount,
    decimal? DeliveredQuantity = null,
    decimal? MissingQuantity = null,
    string? ProductDescription = null
);

public record SalesOrderListItemDto(
    Guid Id,
    string OrderNumber,
    Guid CustomerId,
    string CustomerName,
    DateTime OrderDate,
    decimal SubTotal,
    decimal DiscountAmount,
    decimal TaxAmount,
    decimal TotalAmount,
    string Status,
    string? CreatedBy
);

public record SalesOrderDetailDto(
    Guid Id,
    string OrderNumber,
    Guid CustomerId,
    string CustomerName,
    string CustomerCode,
    DateTime OrderDate,
    decimal SubTotal,
    decimal DiscountAmount,
    decimal TaxAmount,
    decimal TotalAmount,
    string Status,
    string? Notes,
    DateTime CreatedOnUtc,
    List<SalesOrderDetailItemDto> Details,
    string? CreatedBy
);

// ─── Queries ─────────────────────────────────────────────────────────────────

public record GetSalesOrdersQuery(
    Guid? CustomerId,
    string? Status,
    DateTime? FromDate,
    DateTime? ToDate,
    int PageNumber = 1,
    int PageSize = 20,
    string? CreatedBy = null,
    Guid? RouteId = null
) : IRequest<PagedResult<SalesOrderListItemDto>>;

public record GetSalesOrderByIdQuery(Guid SalesOrderId) : IRequest<SalesOrderDetailDto?>;

// ─── Handlers ────────────────────────────────────────────────────────────────

public class GetSalesOrdersQueryHandler : IRequestHandler<GetSalesOrdersQuery, PagedResult<SalesOrderListItemDto>>
{
    private readonly ISalesOrderRepository _repository;
    private readonly UserManager<ApplicationUser> _userManager;

    public GetSalesOrdersQueryHandler(ISalesOrderRepository repository, UserManager<ApplicationUser> userManager)
    {
        _repository = repository;
        _userManager = userManager;
    }

    public async Task<PagedResult<SalesOrderListItemDto>> Handle(GetSalesOrdersQuery request, CancellationToken cancellationToken)
    {
        var (items, totalCount) = await _repository.GetPagedAsync(
            request.CustomerId, request.Status, request.FromDate, request.ToDate,
            request.PageNumber, request.PageSize, request.CreatedBy, request.RouteId, cancellationToken);

        var users = await _userManager.Users.ToListAsync(cancellationToken);

        var dtos = items.Select(so => new SalesOrderListItemDto(
            so.Id,
            so.OrderNumber,
            so.CustomerId,
            so.Customer?.Name ?? string.Empty,
            so.OrderDate,
            so.SubTotal,
            so.DiscountAmount,
            so.TaxAmount,
            so.TotalAmount,
            so.Status.ToString(),
            SalesOrderUserHelper.ResolveSellerFullName(so.CreatedBy, users)));

        return new PagedResult<SalesOrderListItemDto>(dtos.ToList(), totalCount, request.PageNumber, request.PageSize);
    }
}

public class GetSalesOrderByIdQueryHandler : IRequestHandler<GetSalesOrderByIdQuery, SalesOrderDetailDto?>
{
    private readonly ISalesOrderRepository _repository;
    private readonly IInventoryRepository _inventoryRepository;
    private readonly UserManager<ApplicationUser> _userManager;

    public GetSalesOrderByIdQueryHandler(
        ISalesOrderRepository repository,
        IInventoryRepository inventoryRepository,
        UserManager<ApplicationUser> userManager)
    {
        _repository = repository;
        _inventoryRepository = inventoryRepository;
        _userManager = userManager;
    }

    public async Task<SalesOrderDetailDto?> Handle(GetSalesOrderByIdQuery request, CancellationToken cancellationToken)
    {
        var order = await _repository.GetByIdWithDetailsAsync(request.SalesOrderId, cancellationToken);
        if (order == null) return null;

        var users = await _userManager.Users.ToListAsync(cancellationToken);

        var productIds = order.Details.Select(d => d.ProductId).Distinct().ToList();
        var stockDict = await _inventoryRepository.GetAvailableStockByProductIdsAsync(productIds, cancellationToken);

        var details = order.Details.Select(d => {
            decimal originalQty = d.OriginalPresaleQuantity ?? d.Quantity;
            decimal deliveredQty;
            decimal missingQty;

            if (order.Status == SalesOrderStatus.EnCamino || order.Status == SalesOrderStatus.Completado)
            {
                // Si ya fue despachado a EnCamino o Completado, d.Quantity es la cantidad entregada real
                deliveredQty = d.Quantity;
                missingQty = Math.Max(0m, originalQty - deliveredQty);
            }
            else
            {
                // Si aún está en Recibido o EnProceso, auto-calcular con base en el stock real disponible en inventario
                decimal availableStock = stockDict.TryGetValue(d.ProductId, out var st) ? Math.Max(0m, st) : 0m;
                deliveredQty = Math.Min(originalQty, availableStock);
                missingQty = Math.Max(0m, originalQty - deliveredQty);
            }

            return new SalesOrderDetailItemDto(
                d.Id,
                d.ProductId,
                d.Product?.Name ?? string.Empty,
                d.Product?.InternalCode ?? string.Empty,
                d.UnitOfMeasureId,
                d.UnitOfMeasure?.Code ?? string.Empty,
                originalQty,
                d.UnitPrice,
                d.DiscountPercentage,
                d.DiscountAmount,
                d.TaxPercentage,
                d.TaxAmount,
                d.NetAmount,
                deliveredQty,
                missingQty,
                d.Product?.Description
            );
        }).ToList();

        return new SalesOrderDetailDto(
            order.Id,
            order.OrderNumber,
            order.CustomerId,
            order.Customer?.Name ?? string.Empty,
            order.Customer?.CustomerCode ?? string.Empty,
            order.OrderDate,
            order.SubTotal,
            order.DiscountAmount,
            order.TaxAmount,
            order.TotalAmount,
            order.Status.ToString(),
            order.Notes,
            order.CreatedOnUtc,
            details,
            SalesOrderUserHelper.ResolveSellerFullName(order.CreatedBy, users));
    }
}

public static class SalesOrderUserHelper
{
    public static string ResolveSellerFullName(string? createdBy, List<ApplicationUser> users)
    {
        if (string.IsNullOrWhiteSpace(createdBy)) return "N/A";

        var user = users.FirstOrDefault(u =>
            string.Equals(u.UserName, createdBy, StringComparison.OrdinalIgnoreCase) ||
            string.Equals(u.Id.ToString(), createdBy, StringComparison.OrdinalIgnoreCase) ||
            string.Equals(u.Email, createdBy, StringComparison.OrdinalIgnoreCase));

        if (user != null)
        {
            var fullName = $"{user.FirstName} {user.LastName}".Trim();
            if (!string.IsNullOrWhiteSpace(fullName))
                return fullName;
            if (!string.IsNullOrWhiteSpace(user.UserName))
                return user.UserName;
        }

        return createdBy;
    }
}

