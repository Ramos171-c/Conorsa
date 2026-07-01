using MediatR;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Identity;
using EnterpriseBillingSystem.Domain.Entities;
using EnterpriseBillingSystem.Domain.Repositories;
using EnterpriseBillingSystem.Application.Auth.DTOs;

namespace EnterpriseBillingSystem.Application.Auth.Queries;

public record GetCurrentUserProfileQuery(string Username) : IRequest<UserProfileDto?>;

public class GetCurrentUserProfileQueryHandler : IRequestHandler<GetCurrentUserProfileQuery, UserProfileDto?>
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly IPermissionRepository _permissionRepository;

    public GetCurrentUserProfileQueryHandler(
        UserManager<ApplicationUser> userManager,
        IPermissionRepository permissionRepository)
    {
        _userManager = userManager;
        _permissionRepository = permissionRepository;
    }

    public async Task<UserProfileDto?> Handle(GetCurrentUserProfileQuery request, CancellationToken cancellationToken)
    {
        var user = await _userManager.FindByNameAsync(request.Username);
        if (user == null)
            return null;

        var roles = await _userManager.GetRolesAsync(user);
        var roleName = roles.FirstOrDefault() ?? "User";
        var permissions = await _permissionRepository.GetPermissionsByRoleNameAsync(roleName);

        return new UserProfileDto(
            Id: user.Id,
            Username: user.UserName ?? string.Empty,
            Email: user.Email ?? string.Empty,
            FirstName: user.FirstName,
            LastName: user.LastName,
            DefaultBranchId: user.DefaultBranchId,
            Role: roleName,
            Permissions: permissions
        );
    }
}
