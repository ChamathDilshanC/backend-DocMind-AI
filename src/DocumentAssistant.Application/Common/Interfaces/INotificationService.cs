namespace DocumentAssistant.Application.Common.Interfaces;

/// <summary>Abstraction over SignalR so Application/Infrastructure never reference the Hub directly.</summary>
public interface INotificationService
{
    Task SendDocumentStatusChangedAsync(Guid userId, Guid documentId, string status, string? error = null, CancellationToken cancellationToken = default);
    Task SendDocumentProgressAsync(Guid userId, Guid documentId, string stage, int? percent = null, CancellationToken cancellationToken = default);
    Task SendChatTokenAsync(Guid userId, Guid conversationId, Guid messageId, string token, CancellationToken cancellationToken = default);
    Task SendChatCompletedAsync(Guid userId, Guid conversationId, Guid messageId, CancellationToken cancellationToken = default);
}
