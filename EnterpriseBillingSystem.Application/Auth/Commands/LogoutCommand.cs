using MediatR;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using EnterpriseBillingSystem.Domain.Entities;
using EnterpriseBillingSystem.Domain.Repositories;

namespace EnterpriseBillingSystem.Application.Auth.Commands;

public record LogoutCommand(string RefreshToken) : IRequest<bool>;

public class LogoutCommandHandler : IRequestHandler<LogoutCommand, bool>
{
    private readonly IRepository<RefreshToken> _refreshTokenRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<LogoutCommandHandler> _logger;

    public LogoutCommandHandler(
        IRepository<RefreshToken> refreshTokenRepository,
        IUnitOfWork unitOfWork,
        ILogger<LogoutCommandHandler> logger)
    {
        _refreshTokenRepository = refreshTokenRepository;
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task<bool> Handle(LogoutCommand request, CancellationToken cancellationToken)
    {
        var tokens = await _refreshTokenRepository.FindAsync(t => t.Token == request.RefreshToken);
        var dbRefreshToken = tokens.FirstOrDefault();

        if (dbRefreshToken == null)
        {
            _logger.LogWarning("Intento de cierre de sesión fallido: El Refresh Token '{Token}' no existe.", request.RefreshToken);
            return false;
        }

        dbRefreshToken.IsRevoked = true;
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Usuario con ID '{UserId}' cerró sesión. Refresh Token '{Token}' revocado.", dbRefreshToken.UserId, request.RefreshToken);
        return true;
    }
}
