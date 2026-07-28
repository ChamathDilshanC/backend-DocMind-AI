using DocumentAssistant.API.Hubs;
using DocumentAssistant.Application.Common.Interfaces;
using Microsoft.AspNetCore.SignalR;

namespace DocumentAssistant.API.Services;

public class SignalRNotificationService(IHubContext<AppHub> hubContext) : INotificationService
{
    public Task SendDocumentStatusChangedAsync(Guid userId, Guid documentId, string status, string? error = null, CancellationToken cancellationToken = default) =>
        hubContext.Clients.Group(AppHub.GroupNameFor(userId))
            .SendAsync("DocumentStatusChanged", new { documentId, status, error }, cancellationToken);

    public Task SendDocumentProgressAsync(Guid userId, Guid documentId, string stage, int? percent = null, CancellationToken cancellationToken = default) =>
        hubContext.Clients.Group(AppHub.GroupNameFor(userId))
            .SendAsync("DocumentProgress", new { documentId, stage, percent }, cancellationToken);

    public Task SendChatTokenAsync(Guid userId, Guid conversationId, Guid messageId, string token, CancellationToken cancellationToken = default) =>
        hubContext.Clients.Group(AppHub.GroupNameFor(userId))
            .SendAsync("ReceiveAnswerToken", new { conversationId, messageId, token }, cancellationToken);

    public Task SendChatCompletedAsync(Guid userId, Guid conversationId, Guid messageId, CancellationToken cancellationToken = default) =>
        hubContext.Clients.Group(AppHub.GroupNameFor(userId))
            .SendAsync("AnswerCompleted", new { conversationId, messageId }, cancellationToken);
}
