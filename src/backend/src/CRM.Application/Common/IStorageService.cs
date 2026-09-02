namespace CRM.Application.Common;

public interface IStorageService
{
    Task<string> UploadAsync(Stream content, string fileName, string contentType, CancellationToken ct = default);
    Task DeleteAsync(string storageKey, CancellationToken ct = default);
    Task<string> GetPresignedUrlAsync(string storageKey, CancellationToken ct = default);
}
