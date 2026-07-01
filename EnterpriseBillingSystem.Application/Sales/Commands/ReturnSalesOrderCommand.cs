using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using FluentValidation;
using EnterpriseBillingSystem.Domain.Entities;
using EnterpriseBillingSystem.Domain.Enums;
using EnterpriseBillingSystem.Domain.Repositories;

namespace EnterpriseBillingSystem.Application.Sales.Commands;

public record ReturnSalesOrderDetailInput(
    Guid SalesOrderDetailId,
    decimal Quantity
);

public record ReturnSalesOrderCommand(
    Guid SalesOrderId,
    List<ReturnSalesOrderDetailInput>? Items
) : IRequest<Unit>;

public class ReturnSalesOrderCommandValidator : AbstractValidator<ReturnSalesOrderCommand>
{
    public ReturnSalesOrderCommandValidator()
    {
        RuleFor(x => x.SalesOrderId)
            .NotEmpty().WithMessage("El Id del pedido es requerido.");
    }
}

public class ReturnSalesOrderCommandHandler : IRequestHandler<ReturnSalesOrderCommand, Unit>
{
    private readonly ISalesOrderRepository _salesOrderRepository;
    private readonly IUnitOfWork _unitOfWork;

    public ReturnSalesOrderCommandHandler(
        ISalesOrderRepository salesOrderRepository,
        IUnitOfWork unitOfWork)
    {
        _salesOrderRepository = salesOrderRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<Unit> Handle(ReturnSalesOrderCommand request, CancellationToken cancellationToken)
    {
        var order = await _salesOrderRepository.GetByIdWithDetailsAsync(request.SalesOrderId, cancellationToken);
        if (order == null)
            throw new ArgumentException($"El pedido con Id '{request.SalesOrderId}' no existe.");

        if (order.Status == SalesOrderStatus.Anulado)
            throw new InvalidOperationException("El pedido ya está anulado.");

        // Verificar que no tenga facturas confirmadas
        bool hasPostedInvoices = order.SalesInvoices.Any(si => si.Status == SalesInvoiceStatus.Posted);
        if (hasPostedInvoices)
            throw new InvalidOperationException("No se puede realizar una devolución en un pedido con facturas confirmadas asociadas.");

        if (request.Items == null || !request.Items.Any())
        {
            // Devolución Total
            order.Status = SalesOrderStatus.Anulado;
            foreach (var detail in order.Details)
            {
                detail.Quantity = 0;
                detail.DiscountAmount = 0;
                detail.TaxAmount = 0;
                detail.NetAmount = 0;
            }
            order.SubTotal = 0;
            order.DiscountAmount = 0;
            order.TaxAmount = 0;
            order.TotalAmount = 0;
            order.Notes = $"{order.Notes}\n[DEVOLUCIÓN TOTAL]: Pedido devuelto por completo el {DateTime.Now:dd/MM/yyyy HH:mm}.";
        }
        else
        {
            // Devolución Parcial
            var noteParts = new List<string>();
            foreach (var item in request.Items)
            {
                var detail = order.Details.FirstOrDefault(d => d.Id == item.SalesOrderDetailId);
                if (detail == null)
                    throw new ArgumentException($"El detalle del pedido con Id '{item.SalesOrderDetailId}' no existe en este pedido.");

                if (item.Quantity < 0)
                    throw new ArgumentException("La cantidad a devolver no puede ser negativa.");

                if (item.Quantity > detail.Quantity)
                    throw new ArgumentException($"No se puede devolver una cantidad ({item.Quantity}) mayor a la cantidad facturada/pedida ({detail.Quantity}) para el producto '{detail.Product?.Name ?? item.SalesOrderDetailId.ToString()}'.");

                detail.Quantity -= item.Quantity;
                
                // Recalcular montos de línea
                var grossAmount = detail.Quantity * detail.UnitPrice;
                detail.DiscountAmount = grossAmount * (detail.DiscountPercentage / 100);
                var taxableAmount = grossAmount - detail.DiscountAmount;
                detail.TaxAmount = taxableAmount * (detail.TaxPercentage / 100);
                detail.NetAmount = taxableAmount + detail.TaxAmount;

                noteParts.Add($"- {item.Quantity} de '{detail.Product?.Name ?? detail.ProductId.ToString()}'");
            }

            // Recalcular totales del pedido
            order.SubTotal = order.Details.Sum(d => d.Quantity * d.UnitPrice);
            order.DiscountAmount = order.Details.Sum(d => d.Quantity * d.UnitPrice * (d.DiscountPercentage / 100));
            order.TaxAmount = order.Details.Sum(d => d.TaxAmount);
            order.TotalAmount = order.SubTotal - order.DiscountAmount + order.TaxAmount;

            // Si el total queda en 0, se anula
            if (order.TotalAmount <= 0)
            {
                order.Status = SalesOrderStatus.Anulado;
            }

            var notesString = string.Join(", ", noteParts);
            order.Notes = $"{order.Notes}\n[DEVOLUCIÓN PARCIAL]: Devuelto: {notesString} el {DateTime.Now:dd/MM/yyyy HH:mm}.";
        }

        order.LastModifiedBy = "System";
        order.LastModifiedOnUtc = DateTime.UtcNow;

        _salesOrderRepository.Update(order);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return Unit.Value;
    }
}
