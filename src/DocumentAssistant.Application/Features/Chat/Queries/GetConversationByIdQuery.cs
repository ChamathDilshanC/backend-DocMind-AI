using System.Text.Json;
using DocumentAssistant.Application.Common.Exceptions;
using DocumentAssistant.Application.Common.Interfaces;
using DocumentAssistant.Application.Common.Models;
using DocumentAssistant.Application.Features.Chat.Common;
using DocumentAssistant.Domain.Exceptions;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace DocumentAssistant.Application.Features.Chat.Queries;

public record GetConversationByIdQuery(Guid ConversationId) : IRequest<ConversationDetailDto>;

public class GetConversationByIdQueryHandler(IApplicationDbContext context, ICurrentUserService currentUserService)
    : IRequestHandler<GetConversationByIdQuery, ConversationDetailDto>
{
    public async Task<ConversationDetailDto> Handle(GetConversationByIdQuery request, CancellationToken cancellationToken)
    {
        var userId = currentUserService.UserId ?? throw new UnauthorizedException("Not authenticated.");

        var conversation = await context.Conversations
            .Include(c => c.Messages.OrderBy(m => m.CreatedAt))
            .FirstOrDefaultAsync(c => c.Id == request.ConversationId, cancellationToken)
            ?? throw new NotFoundException(nameof(Domain.Entities.Conversation), request.ConversationId);

        if (conversation.UserId != userId)
        {
            throw new ForbiddenAccessException();
        }

        var messages = conversation.Messages.Select(m => new MessageDto(
            m.Id,
            m.Role.ToString(),
            m.Content,
            m.CitationsJson is null ? null : JsonSerializer.Deserialize<List<CitationDto>>(m.CitationsJson),
            m.CreatedAt)).ToList();

        return new ConversationDetailDto(conversation.Id, conversation.Title, messages);
    }
}
