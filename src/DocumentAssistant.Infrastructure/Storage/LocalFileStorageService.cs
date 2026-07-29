using DocumentAssistant.Application.Common.Interfaces;
using Microsoft.Extensions.Options;

namespace DocumentAssistant.Infrastructure.Storage;

/// <summary>Local-disk storage for dev/self-hosted use. Swappable for Azure Blob/S3/MinIO behind IStorageService.</summary>
public class LocalFileStorageService(IOptions<StorageOptions> options) : IStorageService
{
    private readonly string _rootPath = options.Value.RootPath;

    public async Task<string> SaveAsync(Guid userId, Guid documentId, string fileName, Stream content, CancellationToken cancellationToken = default)
    {
        var directory = Path.Combine(_rootPath, userId.ToString(), documentId.ToString());
        Directory.CreateDirectory(directory);

        var safeFileName = Path.GetFileName(fileName);
        var fullPath = Path.Combine(directory, safeFileName);

        await using var fileStream = new FileStream(fullPath, FileMode.Create, FileAccess.Write, FileShare.None);
        await content.CopyToAsync(fileStream, cancellationToken);

        return Path.Combine(userId.ToString(), documentId.ToString(), safeFileName);
    }

    public Task<Stream> OpenReadAsync(string storagePath, CancellationToken cancellationToken = default)
    {
        var fullPath = Path.Combine(_rootPath, storagePath);
        Stream stream = new FileStream(fullPath, FileMode.Open, FileAccess.Read, FileShare.Read);
        return Task.FromResult(stream);
    }

    public Task DeleteAsync(string storagePath, CancellationToken cancellationToken = default)
    {
        var fullPath = Path.Combine(_rootPath, storagePath);
        if (File.Exists(fullPath))
        {
            File.Delete(fullPath);

            var directory = Path.GetDirectoryName(fullPath);
            if (directory is not null && Directory.Exists(directory) && Directory.GetFiles(directory).Length == 0)
            {
                Directory.Delete(directory);
            }
        }

        return Task.CompletedTask;
    }
}
