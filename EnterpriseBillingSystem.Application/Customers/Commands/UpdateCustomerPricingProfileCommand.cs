using System;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using FluentValidation;
using EnterpriseBillingSystem.Domain.Repositories;

namespace EnterpriseBillingSystem.Application.Customers.Commands;

public record UpdateCustomerPricingProfileCommand(
    Guid CustomerId,
    Guid PricingProfileId
) : IRequest<bool>;

public class UpdateCustomerPricingProfileCommandValidator : AbstractValidator<UpdateCustomerPricingProfileCommand>
{
    private readonly ICustomerRepository _customerRepository;
    private readonly IRepository<Domain.Entities.CustomerPricingProfile> _pricingProfileRepository;

    public UpdateCustomerPricingProfileCommandValidator(
        ICustomerRepository customerRepository,
        IRepository<Domain.Entities.CustomerPricingProfile> pricingProfileRepository)
    {
        _customerRepository = customerRepository;
        _pricingProfileRepository = pricingProfileRepository;

        RuleFor(x => x.CustomerId)
            .NotEmpty().WithMessage("El ID del cliente es requerido.");

        RuleFor(x => x.PricingProfileId)
            .NotEmpty().WithMessage("El ID del perfil de precios es requerido.")
            .MustAsync(async (profileId, cancellation) =>
            {
                var profile = await _pricingProfileRepository.GetByIdAsync(profileId);
                return profile != null;
            }).WithMessage("El perfil de precios especificado no existe.");
    }
}

public class UpdateCustomerPricingProfileCommandHandler : IRequestHandler<UpdateCustomerPricingProfileCommand, bool>
{
    private readonly ICustomerRepository _customerRepository;
    private readonly IUnitOfWork _unitOfWork;

    public UpdateCustomerPricingProfileCommandHandler(
        ICustomerRepository customerRepository,
        IUnitOfWork unitOfWork)
    {
        _customerRepository = customerRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<bool> Handle(UpdateCustomerPricingProfileCommand request, CancellationToken cancellationToken)
    {
        var customer = await _customerRepository.GetByIdWithDetailsAsync(request.CustomerId, cancellationToken);
        if (customer == null) return false;

        customer.CustomerPricingProfileId = request.PricingProfileId;
        
        _customerRepository.Update(customer);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return true;
    }
}
