namespace DocumentAssistant.Application.Common.Interfaces;

public interface IStorageService
{
    /// <summary>Saves the stream and returns the storage path used to retrieve it later.</summary>
    Task<string> SaveAsync(Guid userId, Guid documentId, string fileName, Stream content, CancellationToken cancellationToken = default);
    Task<Stream> OpenReadAsync(string storagePath, CancellationToken cancellationToken = default);
    Task DeleteAsync(string storagePath, CancellationToken cancellationToken = default);
}
