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
using EnterpriseBillingSystem.Application.Common.Interfaces;
using EnterpriseBillingSystem.Application.JournalEntries.Commands;

namespace EnterpriseBillingSystem.Application.CashMovements.Commands;

public record CreateCashOutCommand(
    Guid CashSessionId,
    Guid PaymentMethodId,
    decimal Amount,
    string Reason,
    string? Notes
) : IRequest<Guid>;

public class CreateCashOutCommandValidator : AbstractValidator<CreateCashOutCommand>
{
    public CreateCashOutCommandValidator()
    {
        RuleFor(x => x.CashSessionId)
            .NotEmpty().WithMessage("La sesión de caja es requerida.");

        RuleFor(x => x.PaymentMethodId)
            .NotEmpty().WithMessage("El método de pago es requerido.");

        RuleFor(x => x.Amount)
            .GreaterThan(0).WithMessage("El monto de egreso debe ser mayor a 0.");

        RuleFor(x => x.Reason)
            .NotEmpty().WithMessage("El motivo de egreso (Reason) es obligatorio para salidas de dinero.")
            .MaximumLength(250).WithMessage("El motivo de egreso no puede exceder los 250 caracteres.");
    }
}

public class CreateCashOutCommandHandler : IRequestHandler<CreateCashOutCommand, Guid>
{
    private readonly ICashSessionRepository _cashSessionRepository;
    private readonly IPaymentMethodRepository _paymentMethodRepository;
    private readonly ICurrentUserService _currentUserService;
    private readonly IMediator _mediator;
    private readonly IUnitOfWork _unitOfWork;

    public CreateCashOutCommandHandler(
        ICashSessionRepository cashSessionRepository,
        IPaymentMethodRepository paymentMethodRepository,
        ICurrentUserService currentUserService,
        IMediator mediator,
        IUnitOfWork unitOfWork)
    {
        _cashSessionRepository = cashSessionRepository;
        _paymentMethodRepository = paymentMethodRepository;
        _currentUserService = currentUserService;
        _mediator = mediator;
        _unitOfWork = unitOfWork;
    }

    public async Task<Guid> Handle(CreateCashOutCommand request, CancellationToken cancellationToken)
    {
        // 1. Obtener sesión con movimientos
        var session = await _cashSessionRepository.GetByIdWithDetailsAsync(request.CashSessionId, cancellationToken);
        if (session == null)
            throw new ArgumentException("La sesión de caja especificada no existe.");

        if (session.Status != CashSessionStatus.Open)
            throw new InvalidOperationException("No se pueden registrar movimientos de egreso sobre sesiones de caja cerradas.");

        // 2. Validar método de pago
        var paymentMethod = await _paymentMethodRepository.GetByIdAsync(request.PaymentMethodId);
        if (paymentMethod == null)
            throw new ArgumentException("El método de pago especificado no existe.");
        if (!paymentMethod.IsActive)
            throw new InvalidOperationException("El método de pago especificado no está activo.");

        // 3. Si el método de pago es efectivo, validar saldo disponible en caja
        if (paymentMethod.IsCash)
        {
            decimal cashEntries = session.CashMovements
                .Where(m => m.PaymentMethod != null && m.PaymentMethod.IsCash && (
                         m.MovementType == CashMovementType.Opening 
                      || m.MovementType == CashMovementType.SalePayment 
                      || m.MovementType == CashMovementType.CustomerPayment 
                      || m.MovementType == CashMovementType.SupplierRefund 
                      || m.MovementType == CashMovementType.CashIn))
                .Sum(m => m.Amount);

            decimal cashExits = session.CashMovements
                .Where(m => m.PaymentMethod != null && m.PaymentMethod.IsCash && m.MovementType == CashMovementType.CashOut)
                .Sum(m => m.Amount);

            decimal currentCash = cashEntries - cashExits;
            if (request.Amount > currentCash)
            {
                throw new InvalidOperationException($"No hay suficiente saldo de efectivo en caja para procesar este egreso. Disponible: {currentCash}, Requerido: {request.Amount}.");
            }
        }

        var movement = new CashMovement
        {
            Id = Guid.NewGuid(),
            CashSessionId = request.CashSessionId,
            MovementType = CashMovementType.CashOut,
            PaymentMethodId = request.PaymentMethodId,
            Amount = request.Amount,
            Reason = request.Reason,
            Notes = request.Notes,
            CreatedAt = DateTime.UtcNow
        };

        session.CashMovements.Add(movement);
        _cashSessionRepository.Update(session);

        // Generar asiento contable automático
        var jeDetails = new List<JournalEntryDetailInput>
        {
            // Dr 6100 Ajustes Operativos / Cr 1110 Caja General
            new JournalEntryDetailInput("6100", request.Amount, 0, $"Egreso manual de caja ({request.Reason}) - {request.Notes}"),
            new JournalEntryDetailInput("1110", 0, request.Amount, $"Egreso manual de caja ({request.Reason}) - {request.Notes}")
        };

        var createJeCmd = new CreateJournalEntryCommand(
            EntryDate: DateTime.UtcNow,
            Description: $"Asiento por Egreso de Efectivo: {request.Reason} - {request.Notes ?? ""}",
            ReferenceDocument: "CASH-OUT",
            ReferenceId: movement.Id,
            SourceModule: "Cash",
            Details: jeDetails,
            PostImmediately: true
        );

        await _mediator.Send(createJeCmd, cancellationToken);

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return movement.Id;
    }
}
