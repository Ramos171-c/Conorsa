using System;
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

public record CreateCashInCommand(
    Guid CashSessionId,
    Guid PaymentMethodId,
    decimal Amount,
    string? Notes
) : IRequest<Guid>;

public class CreateCashInCommandValidator : AbstractValidator<CreateCashInCommand>
{
    public CreateCashInCommandValidator()
    {
        RuleFor(x => x.CashSessionId)
            .NotEmpty().WithMessage("La sesión de caja es requerida.");

        RuleFor(x => x.PaymentMethodId)
            .NotEmpty().WithMessage("El método de pago es requerido.");

        RuleFor(x => x.Amount)
            .GreaterThan(0).WithMessage("El monto de ingreso debe ser mayor a 0.");
    }
}

public class CreateCashInCommandHandler : IRequestHandler<CreateCashInCommand, Guid>
{
    private readonly ICashSessionRepository _cashSessionRepository;
    private readonly IPaymentMethodRepository _paymentMethodRepository;
    private readonly ICurrentUserService _currentUserService;
    private readonly IMediator _mediator;
    private readonly IUnitOfWork _unitOfWork;

    public CreateCashInCommandHandler(
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

    public async Task<Guid> Handle(CreateCashInCommand request, CancellationToken cancellationToken)
    {
        // 1. Validar sesión
        var session = await _cashSessionRepository.GetByIdAsync(request.CashSessionId);
        if (session == null)
            throw new ArgumentException("La sesión de caja especificada no existe.");

        if (session.Status != CashSessionStatus.Open)
            throw new InvalidOperationException("No se pueden registrar movimientos de ingreso sobre sesiones de caja cerradas.");

        // 2. Validar método de pago
        var paymentMethod = await _paymentMethodRepository.GetByIdAsync(request.PaymentMethodId);
        if (paymentMethod == null)
            throw new ArgumentException("El método de pago especificado no existe.");
        if (!paymentMethod.IsActive)
            throw new InvalidOperationException("El método de pago especificado no está activo.");

        // 3. Crear movimiento
        var movement = new CashMovement
        {
            Id = Guid.NewGuid(),
            CashSessionId = request.CashSessionId,
            MovementType = CashMovementType.CashIn,
            PaymentMethodId = request.PaymentMethodId,
            Amount = request.Amount,
            Notes = request.Notes,
            CreatedAt = DateTime.UtcNow
        };

        session.CashMovements.Add(movement);
        _cashSessionRepository.Update(session);

        // Generar asiento contable automático
        var jeDetails = new List<JournalEntryDetailInput>
        {
            // Dr 1110 Caja General / Cr 6100 Ajustes Operativos
            new JournalEntryDetailInput("1110", request.Amount, 0, $"Ingreso manual de caja - {request.Notes}"),
            new JournalEntryDetailInput("6100", 0, request.Amount, $"Ingreso manual de caja - {request.Notes}")
        };

        var createJeCmd = new CreateJournalEntryCommand(
            EntryDate: DateTime.UtcNow,
            Description: $"Asiento por Ingreso de Efectivo: {request.Notes ?? "Ingreso manual"}",
            ReferenceDocument: "CASH-IN",
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
