using DocumentAssistant.Application.Common.Exceptions;
using DocumentAssistant.Application.Common.Interfaces;
using DocumentAssistant.Domain.Exceptions;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace DocumentAssistant.Application.Features.Embeddings.Commands;

/// <summary>Re-enqueues document processing. Covers both "retry a failed document" and "regenerate embeddings".</summary>
public record CreateEmbeddingsCommand(Guid DocumentId) : IRequest;

public class CreateEmbeddingsCommandHandler(IApplicationDbContext context, IBackgroundJobEnqueuer backgroundJobEnqueuer, ICurrentUserService currentUserService)
    : IRequestHandler<CreateEmbeddingsCommand>
{
    public async Task Handle(CreateEmbeddingsCommand request, CancellationToken cancellationToken)
    {
        var userId = currentUserService.UserId ?? throw new UnauthorizedException("Not authenticated.");

        var document = await context.Documents.FirstOrDefaultAsync(d => d.Id == request.DocumentId, cancellationToken)
            ?? throw new NotFoundException(nameof(Domain.Entities.Document), request.DocumentId);

        if (document.UserId != userId)
        {
            throw new ForbiddenAccessException();
        }

        backgroundJobEnqueuer.EnqueueDocumentProcessing(document.Id);
    }
}
