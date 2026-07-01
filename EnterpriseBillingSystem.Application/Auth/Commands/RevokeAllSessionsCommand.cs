using MediatR;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;
using EnterpriseBillingSystem.Domain.Entities;
using EnterpriseBillingSystem.Domain.Repositories;

namespace EnterpriseBillingSystem.Application.Auth.Commands;

public record RevokeAllSessionsCommand(string Username) : IRequest<bool>;

public class RevokeAllSessionsCommandHandler : IRequestHandler<RevokeAllSessionsCommand, bool>
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly IRepository<RefreshToken> _refreshTokenRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<RevokeAllSessionsCommandHandler> _logger;

    public RevokeAllSessionsCommandHandler(
        UserManager<ApplicationUser> userManager,
        IRepository<RefreshToken> refreshTokenRepository,
        IUnitOfWork unitOfWork,
        ILogger<RevokeAllSessionsCommandHandler> logger)
    {
        _userManager = userManager;
        _refreshTokenRepository = refreshTokenRepository;
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task<bool> Handle(RevokeAllSessionsCommand request, CancellationToken cancellationToken)
    {
        var user = await _userManager.FindByNameAsync(request.Username);
        if (user == null)
        {
            _logger.LogWarning("Revocación de sesiones fallida: El usuario '{Username}' no existe.", request.Username);
            return false;
        }

        var activeTokens = await _refreshTokenRepository.FindAsync(t => t.UserId == user.Id && !t.IsRevoked && !t.IsUsed);
        var count = 0;
        foreach (var token in activeTokens)
        {
            token.IsRevoked = true;
            count++;
        }

        if (count > 0)
        {
            await _unitOfWork.SaveChangesAsync(cancellationToken);
        }

        _logger.LogInformation("Se revocaron con éxito todas las sesiones ({Count} tokens activos) para el usuario '{Username}'.", count, request.Username);
        return true;
    }
}
