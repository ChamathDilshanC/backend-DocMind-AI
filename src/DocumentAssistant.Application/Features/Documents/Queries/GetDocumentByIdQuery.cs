using DocumentAssistant.Application.Common.Exceptions;
using DocumentAssistant.Application.Common.Interfaces;
using DocumentAssistant.Application.Features.Documents.Common;
using DocumentAssistant.Domain.Exceptions;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace DocumentAssistant.Application.Features.Documents.Queries;

public record GetDocumentByIdQuery(Guid DocumentId) : IRequest<DocumentDto>;

public class GetDocumentByIdQueryHandler(IApplicationDbContext context, ICurrentUserService currentUserService)
    : IRequestHandler<GetDocumentByIdQuery, DocumentDto>
{
    public async Task<DocumentDto> Handle(GetDocumentByIdQuery request, CancellationToken cancellationToken)
    {
        var userId = currentUserService.UserId ?? throw new UnauthorizedException("Not authenticated.");

        var document = await context.Documents.FirstOrDefaultAsync(d => d.Id == request.DocumentId, cancellationToken)
            ?? throw new NotFoundException(nameof(Domain.Entities.Document), request.DocumentId);

        if (document.UserId != userId)
        {
            throw new ForbiddenAccessException();
        }

        return DocumentMapper.ToDto(document);
    }
}
