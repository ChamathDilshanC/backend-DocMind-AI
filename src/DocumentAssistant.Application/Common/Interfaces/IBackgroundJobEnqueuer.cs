namespace DocumentAssistant.Application.Common.Interfaces;

/// <summary>Thin wrapper over Hangfire's IBackgroundJobClient so Application never references Hangfire directly.</summary>
public interface IBackgroundJobEnqueuer
{
    void EnqueueDocumentProcessing(Guid documentId);
}
