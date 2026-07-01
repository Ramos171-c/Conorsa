using System;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using FluentValidation;
using EnterpriseBillingSystem.Domain.Entities;
using EnterpriseBillingSystem.Domain.Repositories;

namespace EnterpriseBillingSystem.Application.Banks.Commands;

public record CreateBankCommand(
    string Code,
    string Name,
    string SwiftCode,
    string Country
) : IRequest<Guid>;

public class CreateBankCommandValidator : AbstractValidator<CreateBankCommand>
{
    private readonly IBankRepository _bankRepository;

    public CreateBankCommandValidator(IBankRepository bankRepository)
    {
        _bankRepository = bankRepository;

        RuleFor(x => x.Code)
            .NotEmpty().WithMessage("El código del banco es requerido.")
            .MaximumLength(50).WithMessage("El código no puede exceder 50 caracteres.")
            .MustAsync(async (code, cancellation) =>
            {
                return !await _bankRepository.ExistsCodeAsync(code, cancellation);
            }).WithMessage("Ya existe un banco con este código.");

        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("El nombre del banco es requerido.")
            .MaximumLength(200).WithMessage("El nombre no puede exceder 200 caracteres.");

        RuleFor(x => x.SwiftCode)
            .MaximumLength(20).WithMessage("El código SWIFT no puede exceder 20 caracteres.");

        RuleFor(x => x.Country)
            .MaximumLength(100).WithMessage("El país no puede exceder 100 caracteres.");
    }
}

public class CreateBankCommandHandler : IRequestHandler<CreateBankCommand, Guid>
{
    private readonly IBankRepository _bankRepository;
    private readonly IUnitOfWork _unitOfWork;

    public CreateBankCommandHandler(IBankRepository bankRepository, IUnitOfWork unitOfWork)
    {
        _bankRepository = bankRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<Guid> Handle(CreateBankCommand request, CancellationToken cancellationToken)
    {
        var bank = new Bank
        {
            Code = request.Code,
            Name = request.Name,
            SwiftCode = request.SwiftCode,
            Country = request.Country,
            IsActive = true
        };

        await _bankRepository.AddAsync(bank);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return bank.Id;
    }
}
