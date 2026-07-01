using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using EnterpriseBillingSystem.Domain.Entities;
using EnterpriseBillingSystem.Application.Common.Models;

namespace EnterpriseBillingSystem.Application.Auth.Queries;

public record UserDto(
    Guid Id,
    string Username,
    string Email,
    string FirstName,
    string LastName,
    bool IsActive,
    Guid DefaultBranchId,
    string DefaultBranchName,
    string Role,
    string? Cedula,
    string? PhoneNumber,
    string? Address,
    string? Municipality,
    string? City,
    string? EmergencyContactName,
    string? EmergencyContactPhone,
    Guid? RouteId,
    string? Route,
    bool IsEmployeeActive
);

public record GetUsersQuery(
    string? SearchTerm = null,
    int PageNumber = 1,
    int PageSize = 20
) : IRequest<PagedResult<UserDto>>;

public class GetUsersQueryHandler : IRequestHandler<GetUsersQuery, PagedResult<UserDto>>
{
    private readonly UserManager<ApplicationUser> _userManager;

    public GetUsersQueryHandler(UserManager<ApplicationUser> userManager)
    {
        _userManager = userManager;
    }

    public async Task<PagedResult<UserDto>> Handle(GetUsersQuery request, CancellationToken cancellationToken)
    {
        var query = _userManager.Users
            .Include(u => u.DefaultBranch)
            .Include(u => u.Route)
            .Where(u => !u.IsDeleted);

        if (!string.IsNullOrWhiteSpace(request.SearchTerm))
        {
            var search = request.SearchTerm.ToLower();
            query = query.Where(u => (u.UserName != null && u.UserName.ToLower().Contains(search)) ||
                                     u.FirstName.ToLower().Contains(search) ||
                                     u.LastName.ToLower().Contains(search) ||
                                     (u.Email != null && u.Email.ToLower().Contains(search)));
        }

        var totalCount = await query.CountAsync(cancellationToken);

        var users = await query
            .OrderBy(u => u.UserName)
            .Skip((request.PageNumber - 1) * request.PageSize)
            .Take(request.PageSize)
            .ToListAsync(cancellationToken);

        var dtos = new List<UserDto>();
        foreach (var user in users)
        {
            var roles = await _userManager.GetRolesAsync(user);
            var roleName = roles.FirstOrDefault() ?? "User";

            dtos.Add(new UserDto(
                Id: user.Id,
                Username: user.UserName ?? string.Empty,
                Email: user.Email ?? string.Empty,
                FirstName: user.FirstName,
                LastName: user.LastName,
                IsActive: user.IsActive,
                DefaultBranchId: user.DefaultBranchId,
                DefaultBranchName: user.DefaultBranch?.Name ?? "N/A",
                Role: roleName,
                Cedula: user.Cedula,
                PhoneNumber: user.PhoneNumber,
                Address: user.Address,
                Municipality: user.Municipality,
                City: user.City,
                EmergencyContactName: user.EmergencyContactName,
                EmergencyContactPhone: user.EmergencyContactPhone,
                RouteId: user.RouteId,
                Route: user.Route?.Name,
                IsEmployeeActive: user.IsEmployeeActive
            ));
        }

        return new PagedResult<UserDto>(dtos, totalCount, request.PageNumber, request.PageSize);
    }
}
