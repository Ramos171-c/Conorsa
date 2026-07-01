using System;
using System.IO;
using System.Text.Json;
using System.Threading.Tasks;

namespace EnterpriseBillingSystem.Wpf.Services.Storage;

public class LocalStorageService : ILocalStorageService
{
    private readonly string _storageDir;

    public LocalStorageService()
    {
        _storageDir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "EnterpriseBillingSystem");
        if (!Directory.Exists(_storageDir))
        {
            Directory.CreateDirectory(_storageDir);
        }
    }

    private string GetFilePath(string key)
    {
        return Path.Combine(_storageDir, $"{key}.json");
    }

    public async Task SaveAsync<T>(string key, T value)
    {
        var filePath = GetFilePath(key);
        var json = JsonSerializer.Serialize(value);
        await File.WriteAllTextAsync(filePath, json);
    }

    public async Task<T?> GetAsync<T>(string key)
    {
        var filePath = GetFilePath(key);
        if (!File.Exists(filePath))
        {
            return default;
        }

        try
        {
            var json = await File.ReadAllTextAsync(filePath);
            return JsonSerializer.Deserialize<T>(json);
        }
        catch
        {
            return default;
        }
    }

    public Task ClearAsync(string key)
    {
        var filePath = GetFilePath(key);
        if (File.Exists(filePath))
        {
            File.Delete(filePath);
        }
        return Task.CompletedTask;
    }
}
