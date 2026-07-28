using DocumentAssistant.Application.Common.Exceptions;
using DocumentAssistant.Application.Common.Interfaces;
using DocumentAssistant.Application.Common.Models;
using DocumentAssistant.Application.Features.Chat.Common;
using DocumentAssistant.Shared;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace DocumentAssistant.Application.Features.Chat.Queries;

public record GetChatHistoryQuery(int PageNumber = 1, int PageSize = 20) : IRequest<PaginatedList<ConversationSummaryDto>>;

public class GetChatHistoryQueryHandler(IApplicationDbContext context, ICurrentUserService currentUserService)
    : IRequestHandler<GetChatHistoryQuery, PaginatedList<ConversationSummaryDto>>
{
    public async Task<PaginatedList<ConversationSummaryDto>> Handle(GetChatHistoryQuery request, CancellationToken cancellationToken)
    {
        var userId = currentUserService.UserId ?? throw new UnauthorizedException("Not authenticated.");

        return await context.Conversations
            .Where(c => c.UserId == userId)
            .OrderByDescending(c => c.UpdatedAt)
            .Select(c => new ConversationSummaryDto(
                c.Id,
                c.Title,
                c.Messages.OrderByDescending(m => m.CreatedAt).Select(m => m.Content).FirstOrDefault(),
                c.UpdatedAt))
            .ToPaginatedListAsync(request.PageNumber, request.PageSize, cancellationToken);
    }
}
