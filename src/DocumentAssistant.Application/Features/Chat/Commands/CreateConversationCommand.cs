using DocumentAssistant.Application.Common.Exceptions;
using DocumentAssistant.Application.Common.Interfaces;
using DocumentAssistant.Application.Features.Chat.Common;
using DocumentAssistant.Domain.Entities;
using MediatR;

namespace DocumentAssistant.Application.Features.Chat.Commands;

public record CreateConversationCommand(string? Title) : IRequest<ConversationDto>;

public class CreateConversationCommandHandler(IApplicationDbContext context, ICurrentUserService currentUserService)
    : IRequestHandler<CreateConversationCommand, ConversationDto>
{
    public async Task<ConversationDto> Handle(CreateConversationCommand request, CancellationToken cancellationToken)
    {
        var userId = currentUserService.UserId ?? throw new UnauthorizedException("Not authenticated.");

        var conversation = new Conversation { UserId = userId, Title = string.IsNullOrWhiteSpace(request.Title) ? "New conversation" : request.Title.Trim() };
        context.Conversations.Add(conversation);
        await context.SaveChangesAsync(cancellationToken);

        return new ConversationDto(conversation.Id, conversation.Title, conversation.CreatedAt, conversation.UpdatedAt);
    }
}
