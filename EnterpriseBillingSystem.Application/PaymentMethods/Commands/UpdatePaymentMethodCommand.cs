using System;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using FluentValidation;
using EnterpriseBillingSystem.Domain.Repositories;

namespace EnterpriseBillingSystem.Application.PaymentMethods.Commands;

public record UpdatePaymentMethodCommand(
    Guid Id,
    string Name,
    bool IsCash,
    bool IsActive
) : IRequest<Unit>;

public class UpdatePaymentMethodCommandValidator : AbstractValidator<UpdatePaymentMethodCommand>
{
    public UpdatePaymentMethodCommandValidator()
    {
        RuleFor(x => x.Id)
            .NotEmpty().WithMessage("El Id es requerido.");

        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("El nombre es requerido.")
            .MaximumLength(50).WithMessage("El nombre no puede exceder los 50 caracteres.");
    }
}

public class UpdatePaymentMethodCommandHandler : IRequestHandler<UpdatePaymentMethodCommand, Unit>
{
    private readonly IPaymentMethodRepository _paymentMethodRepository;
    private readonly IUnitOfWork _unitOfWork;

    public UpdatePaymentMethodCommandHandler(
        IPaymentMethodRepository paymentMethodRepository,
        IUnitOfWork unitOfWork)
    {
        _paymentMethodRepository = paymentMethodRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<Unit> Handle(UpdatePaymentMethodCommand request, CancellationToken cancellationToken)
    {
        var method = await _paymentMethodRepository.GetByIdAsync(request.Id);
        if (method == null)
            throw new ArgumentException($"El método de pago con Id '{request.Id}' no existe.");

        method.Name = request.Name;
        method.IsCash = request.IsCash;
        method.IsActive = request.IsActive;
        method.LastModifiedBy = "System";
        method.LastModifiedOnUtc = DateTime.UtcNow;

        _paymentMethodRepository.Update(method);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return Unit.Value;
    }
}
