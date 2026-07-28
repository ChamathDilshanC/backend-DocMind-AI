using DocumentAssistant.Application.Common.Interfaces;
using DocumentAssistant.Domain.Exceptions;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace DocumentAssistant.Application.Features.Admin.Queries;

public record StatisticsDto(
    int TotalUsers, int TotalDocuments, int TotalConversations, int TotalMessages, long TotalStorageBytes,
    int DocumentsProcessing, int DocumentsFailed);

public record GetStatisticsQuery : IRequest<StatisticsDto>;

public class GetStatisticsQueryHandler(IApplicationDbContext context, ICurrentUserService currentUserService)
    : IRequestHandler<GetStatisticsQuery, StatisticsDto>
{
    public async Task<StatisticsDto> Handle(GetStatisticsQuery request, CancellationToken cancellationToken)
    {
        if (!currentUserService.IsAdmin)
        {
            throw new ForbiddenAccessException();
        }

        var totalUsers = await context.Users.CountAsync(cancellationToken);
        var totalDocuments = await context.Documents.CountAsync(cancellationToken);
        var totalConversations = await context.Conversations.CountAsync(cancellationToken);
        var totalMessages = await context.Messages.CountAsync(cancellationToken);
        var totalStorageBytes = await context.Documents.SumAsync(d => d.FileSizeBytes, cancellationToken);
        var documentsProcessing = await context.Documents.CountAsync(d => d.Status == Domain.Enums.DocumentStatus.Processing, cancellationToken);
        var documentsFailed = await context.Documents.CountAsync(d => d.Status == Domain.Enums.DocumentStatus.Failed, cancellationToken);

        return new StatisticsDto(totalUsers, totalDocuments, totalConversations, totalMessages, totalStorageBytes, documentsProcessing, documentsFailed);
    }
}
