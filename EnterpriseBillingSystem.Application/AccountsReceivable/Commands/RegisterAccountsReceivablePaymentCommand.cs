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
using EnterpriseBillingSystem.Application.JournalEntries.Commands;

namespace EnterpriseBillingSystem.Application.AccountsReceivable.Commands;

public record RegisterAccountsReceivablePaymentCommand(
    Guid AccountsReceivableId,
    Guid PaymentMethodId,
    decimal Amount,
    string? ReferenceNumber,
    string? Notes
) : IRequest<Guid>;

public class RegisterAccountsReceivablePaymentCommandValidator : AbstractValidator<RegisterAccountsReceivablePaymentCommand>
{
    public RegisterAccountsReceivablePaymentCommandValidator()
    {
        RuleFor(x => x.AccountsReceivableId)
            .NotEmpty().WithMessage("La cuenta por cobrar es requerida.");

        RuleFor(x => x.PaymentMethodId)
            .NotEmpty().WithMessage("El método de pago es requerido.");

        RuleFor(x => x.Amount)
            .GreaterThan(0).WithMessage("El monto del abono debe ser mayor a 0.");
    }
}

public class RegisterAccountsReceivablePaymentCommandHandler : IRequestHandler<RegisterAccountsReceivablePaymentCommand, Guid>
{
    private readonly IAccountsReceivableRepository _arRepository;
    private readonly ICashSessionRepository _cashSessionRepository;
    private readonly IPaymentMethodRepository _paymentMethodRepository;
    private readonly ICurrentUserService _currentUserService;
    private readonly IMediator _mediator;
    private readonly IUnitOfWork _unitOfWork;

    public RegisterAccountsReceivablePaymentCommandHandler(
        IAccountsReceivableRepository arRepository,
        ICashSessionRepository cashSessionRepository,
        IPaymentMethodRepository paymentMethodRepository,
        ICurrentUserService currentUserService,
        IMediator mediator,
        IUnitOfWork unitOfWork)
    {
        _arRepository = arRepository;
        _cashSessionRepository = cashSessionRepository;
        _paymentMethodRepository = paymentMethodRepository;
        _currentUserService = currentUserService;
        _mediator = mediator;
        _unitOfWork = unitOfWork;
    }

    public async Task<Guid> Handle(RegisterAccountsReceivablePaymentCommand request, CancellationToken cancellationToken)
    {
        // 1. Validar sesión de caja abierta para el usuario actual
        var currentUserIdStr = _currentUserService.UserId ?? throw new InvalidOperationException("Usuario no autenticado.");
        var currentUserId = Guid.Parse(currentUserIdStr);

        var openSession = await _cashSessionRepository.GetOpenSessionByUserAsync(currentUserId, cancellationToken);
        if (openSession == null)
            throw new InvalidOperationException("Debe tener una sesión de caja abierta para poder registrar abonos de cuentas por cobrar.");

        // 2. Obtener la cuenta por cobrar
        var ar = await _arRepository.GetByIdWithDetailsAsync(request.AccountsReceivableId, cancellationToken);
        if (ar == null)
            throw new ArgumentException("La cuenta por cobrar especificada no existe.");

        // 3. Validar estado de la cuenta por cobrar
        if (ar.Status == AccountsReceivableStatus.Paid)
            throw new InvalidOperationException("La cuenta por cobrar ya está completamente pagada.");
        if (ar.Status == AccountsReceivableStatus.Cancelled)
            throw new InvalidOperationException("La cuenta por cobrar se encuentra anulada.");

        // 4. Validar monto a abonar
        if (request.Amount > ar.CurrentBalance)
            throw new InvalidOperationException($"El monto a abonar ({request.Amount}) excede el saldo pendiente actual ({ar.CurrentBalance}).");

        // 5. Validar método de pago
        var paymentMethod = await _paymentMethodRepository.GetByIdAsync(request.PaymentMethodId);
        if (paymentMethod == null)
            throw new ArgumentException("El método de pago especificado no existe.");
        if (!paymentMethod.IsActive)
            throw new InvalidOperationException("El método de pago especificado no está activo.");

        // 6. Registrar el pago
        var payment = new AccountsReceivablePayment
        {
            Id = Guid.NewGuid(),
            AccountsReceivableId = ar.Id,
            CashSessionId = openSession.Id,
            PaymentMethodId = paymentMethod.Id,
            PaymentDate = DateTime.UtcNow,
            Amount = request.Amount,
            ReferenceNumber = request.ReferenceNumber,
            Notes = request.Notes
        };

        ar.PaidAmount += request.Amount;
        ar.CurrentBalance -= request.Amount;
        ar.LastPaymentDate = DateTime.UtcNow;

        // Cambiar estado según el saldo restante
        if (ar.CurrentBalance == 0m)
        {
            ar.Status = AccountsReceivableStatus.Paid;
        }
        else
        {
            ar.Status = AccountsReceivableStatus.PartiallyPaid;
        }

        ar.LastModifiedBy = currentUserIdStr;
        ar.LastModifiedOnUtc = DateTime.UtcNow;

        // 7. Crear el movimiento de caja automático
        var cashMovement = new CashMovement
        {
            Id = Guid.NewGuid(),
            CashSessionId = openSession.Id,
            MovementType = CashMovementType.CustomerPayment,
            PaymentMethodId = paymentMethod.Id,
            ReferenceDocument = ar.DocumentNumber,
            ReferenceId = ar.Id,
            Amount = request.Amount, // Siempre positivo
            Reason = "Abono de Cliente a Cuenta por Cobrar",
            Notes = $"Abono a CxC {ar.DocumentNumber}. Cliente: {ar.Customer.Name}. Ref: {request.ReferenceNumber}",
            CreatedAt = DateTime.UtcNow
        };

        // Agregar entidades a sus respectivos contextos y repositorios
        ar.Payments.Add(payment);
        openSession.CashMovements.Add(cashMovement);

        _arRepository.Update(ar);
        _cashSessionRepository.Update(openSession);

        // Generar asiento contable automático
        var jeDetails = new List<JournalEntryDetailInput>
        {
            new JournalEntryDetailInput("1110", request.Amount, 0, $"Cobro a cliente CxC {ar.DocumentNumber}"),
            new JournalEntryDetailInput("1200", 0, request.Amount, $"Cobro a cliente CxC {ar.DocumentNumber}")
        };

        var createJeCmd = new CreateJournalEntryCommand(
            EntryDate: DateTime.UtcNow,
            Description: $"Asiento por Cobro a Cliente CxC {ar.DocumentNumber}",
            ReferenceDocument: ar.DocumentNumber,
            ReferenceId: ar.Id,
            SourceModule: "Cash",
            Details: jeDetails,
            PostImmediately: true
        );

        await _mediator.Send(createJeCmd, cancellationToken);

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return payment.Id;
    }
}
