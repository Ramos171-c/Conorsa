using System.Threading.Tasks;

namespace EnterpriseBillingSystem.Wpf.Services.Storage;

public interface ILocalStorageService
{
    Task SaveAsync<T>(string key, T value);
    Task<T?> GetAsync<T>(string key);
    Task ClearAsync(string key);
}
