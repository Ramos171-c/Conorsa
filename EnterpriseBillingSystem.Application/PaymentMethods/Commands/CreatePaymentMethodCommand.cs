using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using FluentValidation;
using EnterpriseBillingSystem.Domain.Entities;
using EnterpriseBillingSystem.Domain.Repositories;

namespace EnterpriseBillingSystem.Application.PaymentMethods.Commands;

public record CreatePaymentMethodCommand(
    string Code,
    string Name,
    bool IsCash
) : IRequest<Guid>;

public class CreatePaymentMethodCommandValidator : AbstractValidator<CreatePaymentMethodCommand>
{
    private readonly IPaymentMethodRepository _paymentMethodRepository;

    public CreatePaymentMethodCommandValidator(IPaymentMethodRepository paymentMethodRepository)
    {
        _paymentMethodRepository = paymentMethodRepository;

        RuleFor(x => x.Code)
            .NotEmpty().WithMessage("El código es requerido.")
            .MaximumLength(20).WithMessage("El código no puede exceder los 20 caracteres.")
            .MustAsync(async (code, cancellation) =>
            {
                var existing = await _paymentMethodRepository.GetByCodeAsync(code, cancellation);
                return existing == null;
            }).WithMessage("Ya existe un método de pago con este código.");

        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("El nombre es requerido.")
            .MaximumLength(50).WithMessage("El nombre no puede exceder los 50 caracteres.");
    }
}

public class CreatePaymentMethodCommandHandler : IRequestHandler<CreatePaymentMethodCommand, Guid>
{
    private readonly IPaymentMethodRepository _paymentMethodRepository;
    private readonly IUnitOfWork _unitOfWork;

    public CreatePaymentMethodCommandHandler(
        IPaymentMethodRepository paymentMethodRepository,
        IUnitOfWork unitOfWork)
    {
        _paymentMethodRepository = paymentMethodRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<Guid> Handle(CreatePaymentMethodCommand request, CancellationToken cancellationToken)
    {
        var method = new PaymentMethod
        {
            Code = request.Code.ToUpper(),
            Name = request.Name,
            IsCash = request.IsCash,
            IsActive = true,
            CreatedBy = "System",
            CreatedOnUtc = DateTime.UtcNow
        };

        await _paymentMethodRepository.AddAsync(method);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return method.Id;
    }
}
