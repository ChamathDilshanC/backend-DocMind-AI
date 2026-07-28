using DocumentAssistant.Application.Common.Exceptions;
using DocumentAssistant.Application.Common.Interfaces;
using DocumentAssistant.Domain.Exceptions;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace DocumentAssistant.Application.Features.Documents.Queries;

public record DocumentPageDto(int PageNumber, IReadOnlyList<string> ChunkExcerpts);

public record GetDocumentPagesQuery(Guid DocumentId) : IRequest<IReadOnlyList<DocumentPageDto>>;

public class GetDocumentPagesQueryHandler(IApplicationDbContext context, ICurrentUserService currentUserService)
    : IRequestHandler<GetDocumentPagesQuery, IReadOnlyList<DocumentPageDto>>
{
    public async Task<IReadOnlyList<DocumentPageDto>> Handle(GetDocumentPagesQuery request, CancellationToken cancellationToken)
    {
        var userId = currentUserService.UserId ?? throw new UnauthorizedException("Not authenticated.");

        var document = await context.Documents.FirstOrDefaultAsync(d => d.Id == request.DocumentId, cancellationToken)
            ?? throw new NotFoundException(nameof(Domain.Entities.Document), request.DocumentId);

        if (document.UserId != userId)
        {
            throw new ForbiddenAccessException();
        }

        var chunks = await context.Chunks
            .Where(c => c.DocumentId == request.DocumentId)
            .OrderBy(c => c.PageNumber).ThenBy(c => c.ChunkIndex)
            .Select(c => new { c.PageNumber, c.Text })
            .ToListAsync(cancellationToken);

        return chunks
            .GroupBy(c => c.PageNumber)
            .OrderBy(g => g.Key)
            .Select(g => new DocumentPageDto(g.Key, g.Select(c => c.Text).ToList()))
            .ToList();
    }
}
