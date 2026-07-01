using MediatR;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace EnterpriseBillingSystem.Application.System.Queries;

public record GetSystemStatusQuery : IRequest<SystemStatusDto>;

public record SystemStatusDto(string Status, DateTime Timestamp, string Version);

public class GetSystemStatusQueryHandler : IRequestHandler<GetSystemStatusQuery, SystemStatusDto>
{
    public Task<SystemStatusDto> Handle(GetSystemStatusQuery request, CancellationToken cancellationToken)
    {
        var result = new SystemStatusDto(
            Status: "Online",
            Timestamp: DateTime.UtcNow,
            Version: "1.0.0-beta"
        );
        return Task.FromResult(result);
    }
}
