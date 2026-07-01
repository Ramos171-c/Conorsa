using System;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using EnterpriseBillingSystem.Domain.Enums;
using EnterpriseBillingSystem.Domain.Repositories;

namespace EnterpriseBillingSystem.Application.AccountsPayable.Commands;

public record UpdateOverdueAccountsPayableCommand() : IRequest<int>;

public class UpdateOverdueAccountsPayableCommandHandler : IRequestHandler<UpdateOverdueAccountsPayableCommand, int>
{
    private readonly IAccountsPayableRepository _apRepository;
    private readonly IUnitOfWork _unitOfWork;

    public UpdateOverdueAccountsPayableCommandHandler(
        IAccountsPayableRepository apRepository,
        IUnitOfWork unitOfWork)
    {
        _apRepository = apRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<int> Handle(UpdateOverdueAccountsPayableCommand request, CancellationToken cancellationToken)
    {
        var today = DateTime.UtcNow;
        var overdueAccounts = await _apRepository.GetOverdueAccountsAsync(today, cancellationToken);
        
        int count = 0;
        foreach (var ap in overdueAccounts)
        {
            ap.Status = AccountsPayableStatus.Overdue;
            ap.LastModifiedBy = "SystemJob";
            ap.LastModifiedOnUtc = DateTime.UtcNow;
            
            _apRepository.Update(ap);
            count++;
        }

        if (count > 0)
        {
            await _unitOfWork.SaveChangesAsync(cancellationToken);
        }

        return count;
    }
}
