using DocumentAssistant.Application.Common.Exceptions;
using DocumentAssistant.Application.Common.Interfaces;
using DocumentAssistant.Domain.Enums;
using DocumentAssistant.Domain.Exceptions;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace DocumentAssistant.Application.Features.Embeddings.Commands;

public record DeleteEmbeddingsCommand(Guid DocumentId) : IRequest;

public class DeleteEmbeddingsCommandHandler(
    IApplicationDbContext context, IVectorStoreService vectorStoreService, ICurrentUserService currentUserService)
    : IRequestHandler<DeleteEmbeddingsCommand>
{
    public async Task Handle(DeleteEmbeddingsCommand request, CancellationToken cancellationToken)
    {
        var userId = currentUserService.UserId ?? throw new UnauthorizedException("Not authenticated.");

        var document = await context.Documents.FirstOrDefaultAsync(d => d.Id == request.DocumentId, cancellationToken)
            ?? throw new NotFoundException(nameof(Domain.Entities.Document), request.DocumentId);

        if (document.UserId != userId)
        {
            throw new ForbiddenAccessException();
        }

        await vectorStoreService.DeleteByDocumentIdAsync(document.Id, cancellationToken);

        var chunks = await context.Chunks.Where(c => c.DocumentId == document.Id).ToListAsync(cancellationToken);
        context.Chunks.RemoveRange(chunks);

        document.Status = DocumentStatus.Uploaded;
        document.PageCount = null;
        document.ProcessingError = null;

        await context.SaveChangesAsync(cancellationToken);
    }
}
