using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using EnterpriseBillingSystem.Domain.Enums;
using EnterpriseBillingSystem.Domain.Repositories;

namespace EnterpriseBillingSystem.Application.AccountsReceivable.Commands;

public record UpdateOverdueAccountsReceivableCommand() : IRequest<int>;

public class UpdateOverdueAccountsReceivableCommandHandler : IRequestHandler<UpdateOverdueAccountsReceivableCommand, int>
{
    private readonly IAccountsReceivableRepository _arRepository;
    private readonly IUnitOfWork _unitOfWork;

    public UpdateOverdueAccountsReceivableCommandHandler(
        IAccountsReceivableRepository arRepository,
        IUnitOfWork unitOfWork)
    {
        _arRepository = arRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<int> Handle(UpdateOverdueAccountsReceivableCommand request, CancellationToken cancellationToken)
    {
        var today = DateTime.UtcNow;
        var overdueAccounts = await _arRepository.GetOverdueAccountsAsync(today, cancellationToken);
        
        int count = 0;
        foreach (var ar in overdueAccounts)
        {
            ar.Status = AccountsReceivableStatus.Overdue;
            ar.LastModifiedBy = "SystemJob";
            ar.LastModifiedOnUtc = DateTime.UtcNow;
            
            _arRepository.Update(ar);
            count++;
        }

        if (count > 0)
        {
            await _unitOfWork.SaveChangesAsync(cancellationToken);
        }

        return count;
    }
}
