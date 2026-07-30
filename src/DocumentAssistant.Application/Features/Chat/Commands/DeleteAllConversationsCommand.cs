using DocumentAssistant.Application.Common.Exceptions;
using DocumentAssistant.Application.Common.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace DocumentAssistant.Application.Features.Chat.Commands;

public record DeleteAllConversationsCommand : IRequest;

public class DeleteAllConversationsCommandHandler(IApplicationDbContext context, ICurrentUserService currentUserService)
    : IRequestHandler<DeleteAllConversationsCommand>
{
    public async Task Handle(DeleteAllConversationsCommand request, CancellationToken cancellationToken)
    {
        var userId = currentUserService.UserId ?? throw new UnauthorizedException("Not authenticated.");

        var conversations = await context.Conversations.Where(c => c.UserId == userId).ToListAsync(cancellationToken);
        context.Conversations.RemoveRange(conversations);
        await context.SaveChangesAsync(cancellationToken);
    }
}
