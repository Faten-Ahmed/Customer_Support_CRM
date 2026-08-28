using CRM.Application.Common;

namespace CRM.Infrastructure.Storage;

// Stub — real MinIO/S3 implementation follows in BE infrastructure tasks.
public class StorageService : IStorageService
{
    public Task<string> UploadAsync(Stream content, string fileName, string contentType, CancellationToken ct = default)
        => Task.FromResult(Guid.NewGuid().ToString());

    public Task DeleteAsync(string storageKey, CancellationToken ct = default)
        => Task.CompletedTask;

    public Task<string> GetPresignedUrlAsync(string storageKey, CancellationToken ct = default)
        => Task.FromResult(string.Empty);
}
