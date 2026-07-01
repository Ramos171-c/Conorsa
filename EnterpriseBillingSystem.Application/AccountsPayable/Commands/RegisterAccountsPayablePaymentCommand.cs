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

namespace EnterpriseBillingSystem.Application.AccountsPayable.Commands;

public record RegisterAccountsPayablePaymentCommand(
    Guid AccountsPayableId,
    Guid PaymentMethodId,
    decimal Amount,
    string? ReferenceNumber,
    string? Notes
) : IRequest<Guid>;

public class RegisterAccountsPayablePaymentCommandValidator : AbstractValidator<RegisterAccountsPayablePaymentCommand>
{
    public RegisterAccountsPayablePaymentCommandValidator()
    {
        RuleFor(x => x.AccountsPayableId)
            .NotEmpty().WithMessage("La cuenta por pagar es requerida.");

        RuleFor(x => x.PaymentMethodId)
            .NotEmpty().WithMessage("El método de pago es requerido.");

        RuleFor(x => x.Amount)
            .GreaterThan(0).WithMessage("El monto del abono debe ser mayor a 0.");
    }
}

public class RegisterAccountsPayablePaymentCommandHandler : IRequestHandler<RegisterAccountsPayablePaymentCommand, Guid>
{
    private readonly IAccountsPayableRepository _apRepository;
    private readonly ICashSessionRepository _cashSessionRepository;
    private readonly IPaymentMethodRepository _paymentMethodRepository;
    private readonly ICurrentUserService _currentUserService;
    private readonly IMediator _mediator;
    private readonly IUnitOfWork _unitOfWork;

    public RegisterAccountsPayablePaymentCommandHandler(
        IAccountsPayableRepository apRepository,
        ICashSessionRepository cashSessionRepository,
        IPaymentMethodRepository paymentMethodRepository,
        ICurrentUserService currentUserService,
        IMediator mediator,
        IUnitOfWork unitOfWork)
    {
        _apRepository = apRepository;
        _cashSessionRepository = cashSessionRepository;
        _paymentMethodRepository = paymentMethodRepository;
        _currentUserService = currentUserService;
        _mediator = mediator;
        _unitOfWork = unitOfWork;
    }

    public async Task<Guid> Handle(RegisterAccountsPayablePaymentCommand request, CancellationToken cancellationToken)
    {
        // 1. Validar sesión de caja abierta para el usuario actual
        var currentUserIdStr = _currentUserService.UserId ?? throw new InvalidOperationException("Usuario no autenticado.");
        var currentUserId = Guid.Parse(currentUserIdStr);

        var openSession = await _cashSessionRepository.GetOpenSessionByUserAsync(currentUserId, cancellationToken);
        if (openSession == null)
            throw new InvalidOperationException("Debe tener una sesión de caja abierta para poder registrar pagos de cuentas por pagar.");

        // 2. Obtener la cuenta por pagar
        var ap = await _apRepository.GetByIdWithDetailsAsync(request.AccountsPayableId, cancellationToken);
        if (ap == null)
            throw new ArgumentException("La cuenta por pagar especificada no existe.");

        // 3. Validar estado de la cuenta por pagar
        if (ap.Status == AccountsPayableStatus.Paid)
            throw new InvalidOperationException("La cuenta por pagar ya está completamente pagada.");
        if (ap.Status == AccountsPayableStatus.Cancelled)
            throw new InvalidOperationException("La cuenta por pagar se encuentra anulada.");

        // 4. Validar monto a abonar
        if (request.Amount > ap.CurrentBalance)
            throw new InvalidOperationException($"El monto a pagar ({request.Amount}) excede el saldo pendiente actual ({ap.CurrentBalance}).");

        // 5. Validar método de pago
        var paymentMethod = await _paymentMethodRepository.GetByIdAsync(request.PaymentMethodId);
        if (paymentMethod == null)
            throw new ArgumentException("El método de pago especificado no existe.");
        if (!paymentMethod.IsActive)
            throw new InvalidOperationException("El método de pago especificado no está activo.");

        // 6. Registrar el pago
        var payment = new AccountsPayablePayment
        {
            Id = Guid.NewGuid(),
            AccountsPayableId = ap.Id,
            CashSessionId = openSession.Id,
            PaymentMethodId = paymentMethod.Id,
            PaymentDate = DateTime.UtcNow,
            Amount = request.Amount,
            ReferenceNumber = request.ReferenceNumber,
            Notes = request.Notes
        };

        ap.PaidAmount += request.Amount;
        ap.CurrentBalance -= request.Amount;
        ap.LastPaymentDate = DateTime.UtcNow;

        // Cambiar estado según el saldo restante
        if (ap.CurrentBalance == 0m)
        {
            ap.Status = AccountsPayableStatus.Paid;
        }
        else
        {
            ap.Status = AccountsPayableStatus.PartiallyPaid;
        }

        ap.LastModifiedBy = currentUserIdStr;
        ap.LastModifiedOnUtc = DateTime.UtcNow;

        // 7. Crear el movimiento de caja automático
        var cashMovement = new CashMovement
        {
            Id = Guid.NewGuid(),
            CashSessionId = openSession.Id,
            MovementType = CashMovementType.SupplierRefund, // Egresos por pagos a proveedores
            PaymentMethodId = paymentMethod.Id,
            ReferenceDocument = ap.DocumentNumber,
            ReferenceId = ap.Id,
            Amount = request.Amount, // Almacenar siempre positivo
            Reason = "Pago a Proveedor a Cuenta por Pagar",
            Notes = $"Pago a CxP {ap.DocumentNumber}. Proveedor: {ap.Supplier.Name}. Ref: {request.ReferenceNumber}",
            CreatedAt = DateTime.UtcNow
        };

        // Agregar entidades a sus respectivos contextos y repositorios
        ap.Payments.Add(payment);
        openSession.CashMovements.Add(cashMovement);

        _apRepository.Update(ap);
        _cashSessionRepository.Update(openSession);

        // Generar asiento contable automático
        var jeDetails = new List<JournalEntryDetailInput>
        {
            new JournalEntryDetailInput("2100", request.Amount, 0, $"Pago a proveedor CxP {ap.DocumentNumber}"),
            new JournalEntryDetailInput("1110", 0, request.Amount, $"Pago a proveedor CxP {ap.DocumentNumber}")
        };

        var createJeCmd = new CreateJournalEntryCommand(
            EntryDate: DateTime.UtcNow,
            Description: $"Asiento por Pago a Proveedor CxP {ap.DocumentNumber}",
            ReferenceDocument: ap.DocumentNumber,
            ReferenceId: ap.Id,
            SourceModule: "Cash",
            Details: jeDetails,
            PostImmediately: true
        );

        await _mediator.Send(createJeCmd, cancellationToken);

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return payment.Id;
    }
}
