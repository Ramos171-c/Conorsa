using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using FluentValidation;
using EnterpriseBillingSystem.Domain.Entities;
using EnterpriseBillingSystem.Domain.Enums;
using EnterpriseBillingSystem.Domain.Repositories;
using EnterpriseBillingSystem.Application.Common.Interfaces;

namespace EnterpriseBillingSystem.Application.CashSessions.Commands;

public record CloseCashSessionCommand(
    Guid CashSessionId,
    decimal ClosingAmount,
    string? Notes
) : IRequest<Unit>;

public class CloseCashSessionCommandValidator : AbstractValidator<CloseCashSessionCommand>
{
    public CloseCashSessionCommandValidator()
    {
        RuleFor(x => x.CashSessionId)
            .NotEmpty().WithMessage("La sesión de caja es requerida.");

        RuleFor(x => x.ClosingAmount)
            .GreaterThanOrEqualTo(0).WithMessage("El monto de arqueo físico no puede ser negativo.");
    }
}

public class CloseCashSessionCommandHandler : IRequestHandler<CloseCashSessionCommand, Unit>
{
    private readonly ICashSessionRepository _cashSessionRepository;
    private readonly IPaymentMethodRepository _paymentMethodRepository;
    private readonly ICurrentUserService _currentUserService;
    private readonly IUnitOfWork _unitOfWork;

    public CloseCashSessionCommandHandler(
        ICashSessionRepository cashSessionRepository,
        IPaymentMethodRepository paymentMethodRepository,
        ICurrentUserService currentUserService,
        IUnitOfWork unitOfWork)
    {
        _cashSessionRepository = cashSessionRepository;
        _paymentMethodRepository = paymentMethodRepository;
        _currentUserService = currentUserService;
        _unitOfWork = unitOfWork;
    }

    public async Task<Unit> Handle(CloseCashSessionCommand request, CancellationToken cancellationToken)
    {
        // 1. Obtener la sesión con sus movimientos
        var session = await _cashSessionRepository.GetByIdWithDetailsAsync(request.CashSessionId, cancellationToken);
        if (session == null)
            throw new ArgumentException("La sesión de caja especificada no existe.");

        // 2. Regla: Un usuario no puede cerrar una sesión ya cerrada
        if (session.Status == CashSessionStatus.Closed)
            throw new InvalidOperationException("La sesión de caja ya se encuentra cerrada.");

        // 3. Calcular monto esperado (Opening + Entradas - Salidas)
        decimal entries = session.CashMovements
            .Where(m => m.MovementType == CashMovementType.Opening 
                     || m.MovementType == CashMovementType.SalePayment 
                     || m.MovementType == CashMovementType.CustomerPayment 
                     || m.MovementType == CashMovementType.SupplierRefund 
                     || m.MovementType == CashMovementType.CashIn)
            .Sum(m => m.Amount);

        decimal exits = session.CashMovements
            .Where(m => m.MovementType == CashMovementType.CashOut)
            .Sum(m => m.Amount);

        decimal expectedAmount = entries - exits;
        decimal differenceAmount = request.ClosingAmount - expectedAmount;

        // 4. Registrar diferencia de arqueo en la sesión
        session.ClosingAmount = request.ClosingAmount;
        session.ExpectedAmount = expectedAmount;
        session.DifferenceAmount = differenceAmount;
        session.Status = CashSessionStatus.Closed;
        session.ClosedAt = DateTime.UtcNow;
        session.ClosedByUserId = Guid.Parse(_currentUserService.UserId ?? throw new InvalidOperationException("Usuario no autenticado."));
        session.LastModifiedBy = _currentUserService.UserId ?? "System";
        session.LastModifiedOnUtc = DateTime.UtcNow;

        // 5. Si hay diferencia, registrar movimiento de ajuste
        if (differenceAmount != 0)
        {
            var cashPaymentMethod = await _paymentMethodRepository.GetByCodeAsync("EFEC", cancellationToken)
                ?? (await _paymentMethodRepository.FindAsync(p => p.IsCash && p.IsActive)).FirstOrDefault();

            if (cashPaymentMethod == null)
                throw new InvalidOperationException("No se encontró un método de pago en efectivo ('EFEC') activo configurado en el sistema.");

            session.CashMovements.Add(new CashMovement
            {
                Id = Guid.NewGuid(),
                CashSessionId = session.Id,
                MovementType = CashMovementType.ClosingAdjustment,
                PaymentMethodId = cashPaymentMethod.Id,
                Amount = Math.Abs(differenceAmount),
                Notes = differenceAmount > 0 
                    ? $"Ajuste de arqueo por sobrante en caja"
                    : $"Ajuste de arqueo por faltante en caja",
                Reason = differenceAmount > 0 ? "Sobrante de Arqueo" : "Faltante de Arqueo",
                CreatedAt = DateTime.UtcNow
            });
        }

        _cashSessionRepository.Update(session);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return Unit.Value;
    }
}
