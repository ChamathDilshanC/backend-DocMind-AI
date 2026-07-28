namespace DocumentAssistant.Infrastructure.BackgroundJobs;

public interface IDocumentProcessingJob
{
    Task ProcessDocumentAsync(Guid documentId, CancellationToken cancellationToken);
}
