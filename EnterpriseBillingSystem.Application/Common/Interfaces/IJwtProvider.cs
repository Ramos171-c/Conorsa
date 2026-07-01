using System.Collections.Generic;
using EnterpriseBillingSystem.Domain.Entities;

namespace EnterpriseBillingSystem.Application.Common.Interfaces;

public interface IJwtProvider
{
    (string Token, int ExpiryMinutes) GenerateToken(ApplicationUser user, string role, IEnumerable<string> permissions);
}
