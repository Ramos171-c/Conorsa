using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using EnterpriseBillingSystem.Domain.Entities;
using EnterpriseBillingSystem.Domain.Repositories;

namespace EnterpriseBillingSystem.Application.System.Queries;

public record BranchDto(Guid Id, string Code, string Name);

public record GetBranchesQuery : IRequest<IEnumerable<BranchDto>>;

public class GetBranchesQueryHandler : IRequestHandler<GetBranchesQuery, IEnumerable<BranchDto>>
{
    private readonly IRepository<Branch> _branchRepository;

    public GetBranchesQueryHandler(IRepository<Branch> branchRepository)
    {
        _branchRepository = branchRepository;
    }

    public async Task<IEnumerable<BranchDto>> Handle(GetBranchesQuery request, CancellationToken cancellationToken)
    {
        var branches = await _branchRepository.GetAllAsync();
        return branches
            .Where(b => b.IsActive && !b.IsDeleted)
            .Select(b => new BranchDto(b.Id, b.Code, b.Name))
            .ToList();
    }
}
