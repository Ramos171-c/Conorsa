using MediatR;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;
using EnterpriseBillingSystem.Domain.Entities;

namespace EnterpriseBillingSystem.Application.Auth.Commands;

public record ChangePasswordCommand(string Username, string CurrentPassword, string NewPassword) : IRequest<bool>;

public class ChangePasswordCommandHandler : IRequestHandler<ChangePasswordCommand, bool>
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly ILogger<ChangePasswordCommandHandler> _logger;

    public ChangePasswordCommandHandler(UserManager<ApplicationUser> userManager, ILogger<ChangePasswordCommandHandler> logger)
    {
        _userManager = userManager;
        _logger = logger;
    }

    public async Task<bool> Handle(ChangePasswordCommand request, CancellationToken cancellationToken)
    {
        var user = await _userManager.FindByNameAsync(request.Username);
        if (user == null)
        {
            _logger.LogWarning("Cambio de contraseña fallido: El usuario '{Username}' no existe.", request.Username);
            return false;
        }

        var result = await _userManager.ChangePasswordAsync(user, request.CurrentPassword, request.NewPassword);
        if (!result.Succeeded)
        {
            var errors = string.Join(", ", result.Errors.Select(e => e.Description));
            _logger.LogWarning("Cambio de contraseña fallido para usuario '{Username}': {Errors}", request.Username, errors);
            return false;
        }

        user.ForcePasswordChange = false;
        await _userManager.UpdateAsync(user);

        _logger.LogInformation("Usuario '{Username}' cambió su contraseña con éxito. La bandera ForcePasswordChange se ha desactivado.", request.Username);
        return true;
    }
}
