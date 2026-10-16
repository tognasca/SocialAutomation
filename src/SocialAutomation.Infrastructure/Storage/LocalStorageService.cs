using Microsoft.Extensions.Configuration;
using SocialAutomation.Domain.Interfaces.Services;

namespace SocialAutomation.Infrastructure.Storage;

public class LocalStorageService : IStorageService
{
    private readonly string _storagePath;

    public LocalStorageService(IConfiguration configuration)
    {
        _storagePath = configuration["Storage:LocalPath"] ?? Path.Combine(AppContext.BaseDirectory, "uploads");
        if (!Directory.Exists(_storagePath))
        {
            Directory.CreateDirectory(_storagePath);
        }
    }

    public async Task<string> UploadFileAsync(Stream fileStream, string fileName, string contentType, Guid tenantId, CancellationToken ct = default)
    {
        var tenantFolder = Path.Combine(_storagePath, tenantId.ToString());
        if (!Directory.Exists(tenantFolder))
        {
            Directory.CreateDirectory(tenantFolder);
        }

        var uniqueFileName = $"{Guid.NewGuid():N}_{fileName}";
        var filePath = Path.Combine(tenantFolder, uniqueFileName);

        using var destination = File.Create(filePath);
        await fileStream.CopyToAsync(destination, ct);

        // Retorna URL acessível localmente
        return $"/uploads/{tenantId}/{uniqueFileName}";
    }

    public Task DeleteFileAsync(string fileUrl, Guid tenantId, CancellationToken ct = default)
    {
        var fileName = Path.GetFileName(fileUrl);
        var filePath = Path.Combine(_storagePath, tenantId.ToString(), fileName);
        if (File.Exists(filePath))
        {
            File.Delete(filePath);
        }
        return Task.CompletedTask;
    }
}