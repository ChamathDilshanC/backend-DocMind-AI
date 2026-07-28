using DocumentAssistant.Application.Common.Interfaces;
using Hangfire;

namespace DocumentAssistant.Infrastructure.BackgroundJobs;

public class HangfireBackgroundJobEnqueuer(IBackgroundJobClient backgroundJobClient) : IBackgroundJobEnqueuer
{
    public void EnqueueDocumentProcessing(Guid documentId) =>
        backgroundJobClient.Enqueue<IDocumentProcessingJob>(job => job.ProcessDocumentAsync(documentId, CancellationToken.None));
}
