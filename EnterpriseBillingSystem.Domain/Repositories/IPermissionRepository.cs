using System.Collections.Generic;
using System.Threading.Tasks;
using EnterpriseBillingSystem.Domain.Entities;

namespace EnterpriseBillingSystem.Domain.Repositories;

public interface IPermissionRepository : IRepository<Permission>
{
    Task<IEnumerable<string>> GetPermissionsByRoleNameAsync(string roleName);
}
