using System;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using EnterpriseBillingSystem.Domain.Repositories;

namespace EnterpriseBillingSystem.Application.PaymentMethods.Commands;

public record DeletePaymentMethodCommand(Guid Id) : IRequest<Unit>;

public class DeletePaymentMethodCommandHandler : IRequestHandler<DeletePaymentMethodCommand, Unit>
{
    private readonly IPaymentMethodRepository _paymentMethodRepository;
    private readonly IUnitOfWork _unitOfWork;

    public DeletePaymentMethodCommandHandler(
        IPaymentMethodRepository paymentMethodRepository,
        IUnitOfWork unitOfWork)
    {
        _paymentMethodRepository = paymentMethodRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<Unit> Handle(DeletePaymentMethodCommand request, CancellationToken cancellationToken)
    {
        var method = await _paymentMethodRepository.GetByIdAsync(request.Id);
        if (method == null)
            throw new ArgumentException($"El método de pago con Id '{request.Id}' no existe.");

        method.IsDeleted = true;
        method.LastModifiedBy = "System";
        method.LastModifiedOnUtc = DateTime.UtcNow;

        _paymentMethodRepository.Update(method);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return Unit.Value;
    }
}
