using System;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using EnterpriseBillingSystem.Application.Banks.DTOs;
using EnterpriseBillingSystem.Domain.Repositories;

namespace EnterpriseBillingSystem.Application.Banks.Queries;

public record GetBankByIdQuery(Guid Id) : IRequest<BankDto?>;

public class GetBankByIdQueryHandler : IRequestHandler<GetBankByIdQuery, BankDto?>
{
    private readonly IBankRepository _bankRepository;

    public GetBankByIdQueryHandler(IBankRepository bankRepository)
    {
        _bankRepository = bankRepository;
    }

    public async Task<BankDto?> Handle(GetBankByIdQuery request, CancellationToken cancellationToken)
    {
        var bank = await _bankRepository.GetByIdAsync(request.Id);
        if (bank == null) return null;

        return new BankDto(bank.Id, bank.Code, bank.Name, bank.SwiftCode, bank.Country, bank.IsActive);
    }
}
