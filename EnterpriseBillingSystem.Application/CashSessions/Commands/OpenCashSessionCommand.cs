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

public record OpenCashSessionCommand(
    Guid CashRegisterId,
    decimal OpeningAmount,
    string? Notes
) : IRequest<Guid>;

public class OpenCashSessionCommandValidator : AbstractValidator<OpenCashSessionCommand>
{
    public OpenCashSessionCommandValidator()
    {
        RuleFor(x => x.CashRegisterId)
            .NotEmpty().WithMessage("La caja física es requerida.");

        RuleFor(x => x.OpeningAmount)
            .GreaterThanOrEqualTo(0).WithMessage("El monto de apertura no puede ser negativo.");
    }
}

public class OpenCashSessionCommandHandler : IRequestHandler<OpenCashSessionCommand, Guid>
{
    private readonly ICashSessionRepository _cashSessionRepository;
    private readonly ICashRegisterRepository _cashRegisterRepository;
    private readonly IPaymentMethodRepository _paymentMethodRepository;
    private readonly ICurrentUserService _currentUserService;
    private readonly IUnitOfWork _unitOfWork;

    public OpenCashSessionCommandHandler(
        ICashSessionRepository cashSessionRepository,
        ICashRegisterRepository cashRegisterRepository,
        IPaymentMethodRepository paymentMethodRepository,
        ICurrentUserService currentUserService,
        IUnitOfWork unitOfWork)
    {
        _cashSessionRepository = cashSessionRepository;
        _cashRegisterRepository = cashRegisterRepository;
        _paymentMethodRepository = paymentMethodRepository;
        _currentUserService = currentUserService;
        _unitOfWork = unitOfWork;
    }

    public async Task<Guid> Handle(OpenCashSessionCommand request, CancellationToken cancellationToken)
    {
        // 1. Validar caja física
        var register = await _cashRegisterRepository.GetByIdAsync(request.CashRegisterId);
        if (register == null)
            throw new ArgumentException("La caja física especificada no existe.");
        if (!register.IsActive)
            throw new InvalidOperationException("La caja física especificada no está activa.");

        var currentUserId = Guid.Parse(_currentUserService.UserId ?? throw new InvalidOperationException("Usuario no autenticado."));

        // 2. Regla: Solo puede existir una sesión abierta por caja
        var openSessionOnRegister = await _cashSessionRepository.GetOpenSessionByRegisterAsync(request.CashRegisterId, cancellationToken);
        if (openSessionOnRegister != null)
            throw new InvalidOperationException($"La caja '{register.Name}' ya tiene una sesión abierta por el usuario '{openSessionOnRegister.OpenedByUser?.UserName ?? "otro usuario"}'.");

        // 3. Regla: Un usuario no puede tener múltiples sesiones abiertas
        var openSessionByUser = await _cashSessionRepository.GetOpenSessionByUserAsync(currentUserId, cancellationToken);
        if (openSessionByUser != null)
            throw new InvalidOperationException($"El usuario actual ya tiene una sesión abierta (Número: {openSessionByUser.SessionNumber}) en la caja '{openSessionByUser.CashRegister?.Name}'.");

        // 4. Obtener método de pago de efectivo
        var cashPaymentMethod = await _paymentMethodRepository.GetByCodeAsync("EFEC", cancellationToken)
            ?? (await _paymentMethodRepository.FindAsync(p => p.IsCash && p.IsActive)).FirstOrDefault();

        if (cashPaymentMethod == null)
            throw new InvalidOperationException("No se encontró un método de pago en efectivo ('EFEC') activo configurado en el sistema.");

        // 5. Generar número de sesión
        var sessionNumber = await _cashSessionRepository.GenerateSessionNumberAsync(cancellationToken);

        // 6. Crear sesión
        var session = new CashSession
        {
            Id = Guid.NewGuid(),
            SessionNumber = sessionNumber,
            CashRegisterId = request.CashRegisterId,
            OpenedByUserId = currentUserId,
            OpeningAmount = request.OpeningAmount,
            OpenedAt = DateTime.UtcNow,
            Status = CashSessionStatus.Open,
            Notes = request.Notes,
            CreatedBy = _currentUserService.UserId ?? "System",
            CreatedOnUtc = DateTime.UtcNow
        };

        // 7. Crear movimiento inicial de apertura
        session.CashMovements.Add(new CashMovement
        {
            Id = Guid.NewGuid(),
            CashSessionId = session.Id,
            MovementType = CashMovementType.Opening,
            PaymentMethodId = cashPaymentMethod.Id,
            Amount = request.OpeningAmount,
            Notes = $"Saldo inicial de apertura de sesión",
            CreatedAt = DateTime.UtcNow
        });

        await _cashSessionRepository.AddAsync(session);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return session.Id;
    }
}
