using CRM.Application.Common;
using Microsoft.Extensions.Options;
using Minio;
using Minio.DataModel.Args;

namespace CRM.Infrastructure.Storage;

public class StorageService : IStorageService
{
    private readonly IMinioClient _minio;
    private readonly string _bucket;
    private readonly int _expirySeconds;

    public StorageService(IOptions<MinIOSettings> options)
    {
        var cfg = options.Value;
        _bucket = cfg.BucketName;
        _expirySeconds = cfg.PresignedUrlExpirySeconds;

        _minio = new MinioClient()
            .WithEndpoint(cfg.Endpoint)
            .WithCredentials(cfg.AccessKey, cfg.SecretKey)
            .WithSSL(cfg.UseSSL)
            .Build();
    }

    public async Task<string> UploadAsync(
        Stream content, string fileName, string contentType,
        CancellationToken ct = default)
    {
        await EnsureBucketAsync(ct);

        // Buffer to get length (stream from IFormFile may not be seekable)
        using var ms = new MemoryStream();
        await content.CopyToAsync(ms, ct);
        ms.Position = 0;

        var key = $"{Guid.NewGuid():N}/{SanitizeName(fileName)}";

        await _minio.PutObjectAsync(new PutObjectArgs()
            .WithBucket(_bucket)
            .WithObject(key)
            .WithStreamData(ms)
            .WithObjectSize(ms.Length)
            .WithContentType(contentType),
            ct);

        return key;
    }

    public async Task DeleteAsync(string storageKey, CancellationToken ct = default)
    {
        await _minio.RemoveObjectAsync(new RemoveObjectArgs()
            .WithBucket(_bucket)
            .WithObject(storageKey),
            ct);
    }

    public async Task<string> GetPresignedUrlAsync(
        string storageKey, CancellationToken ct = default)
    {
        return await _minio.PresignedGetObjectAsync(new PresignedGetObjectArgs()
            .WithBucket(_bucket)
            .WithObject(storageKey)
            .WithExpiry(_expirySeconds));
    }

    private async Task EnsureBucketAsync(CancellationToken ct)
    {
        var exists = await _minio.BucketExistsAsync(
            new BucketExistsArgs().WithBucket(_bucket), ct);
        if (!exists)
            await _minio.MakeBucketAsync(
                new MakeBucketArgs().WithBucket(_bucket), ct);
    }

    private static string SanitizeName(string name)
        => string.Concat(name.Where(c => char.IsLetterOrDigit(c) || c is '.' or '-' or '_'));
}
