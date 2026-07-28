using DocumentAssistant.Application.Common.Exceptions;
using DocumentAssistant.Application.Common.Interfaces;
using DocumentAssistant.Application.Common.Models;
using DocumentAssistant.Application.Features.Documents.Common;
using DocumentAssistant.Shared;
using MediatR;

namespace DocumentAssistant.Application.Features.Documents.Queries;

public record GetDocumentsQuery(int PageNumber = 1, int PageSize = 20) : IRequest<PaginatedList<DocumentDto>>;

public class GetDocumentsQueryHandler(IApplicationDbContext context, ICurrentUserService currentUserService)
    : IRequestHandler<GetDocumentsQuery, PaginatedList<DocumentDto>>
{
    public async Task<PaginatedList<DocumentDto>> Handle(GetDocumentsQuery request, CancellationToken cancellationToken)
    {
        var userId = currentUserService.UserId ?? throw new UnauthorizedException("Not authenticated.");

        return await context.Documents
            .Where(d => d.UserId == userId)
            .OrderByDescending(d => d.CreatedAt)
            .Select(d => new DocumentDto(d.Id, d.Name, d.FileType.ToString(), d.FileSizeBytes, d.Status.ToString(), d.ProcessingError, d.PageCount, d.CreatedAt, d.UpdatedAt))
            .ToPaginatedListAsync(request.PageNumber, request.PageSize, cancellationToken);
    }
}
