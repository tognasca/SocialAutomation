namespace SocialAutomation.Domain.Interfaces.Services;

public interface IStorageService
{
    Task<string> UploadFileAsync(Stream fileStream, string fileName, string contentType, Guid tenantId, CancellationToken ct = default);
    Task DeleteFileAsync(string fileUrl, Guid tenantId, CancellationToken ct = default);
}