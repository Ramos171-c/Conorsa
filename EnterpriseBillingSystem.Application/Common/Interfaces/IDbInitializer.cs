using System.Threading.Tasks;

namespace EnterpriseBillingSystem.Application.Common.Interfaces;

public interface IDbInitializer
{
    Task InitializeAsync();
}
