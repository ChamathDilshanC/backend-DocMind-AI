using DocumentAssistant.Application.Common.Exceptions;
using DocumentAssistant.Application.Common.Interfaces;
using DocumentAssistant.Domain.Exceptions;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace DocumentAssistant.Application.Features.Documents.Commands;

public record DeleteDocumentCommand(Guid DocumentId) : IRequest;

public class DeleteDocumentCommandHandler(
    IApplicationDbContext context, IStorageService storageService, IVectorStoreService vectorStoreService,
    ICurrentUserService currentUserService)
    : IRequestHandler<DeleteDocumentCommand>
{
    public async Task Handle(DeleteDocumentCommand request, CancellationToken cancellationToken)
    {
        var userId = currentUserService.UserId ?? throw new UnauthorizedException("Not authenticated.");

        var document = await context.Documents.FirstOrDefaultAsync(d => d.Id == request.DocumentId, cancellationToken)
            ?? throw new NotFoundException(nameof(Domain.Entities.Document), request.DocumentId);

        if (document.UserId != userId)
        {
            throw new ForbiddenAccessException();
        }

        await vectorStoreService.DeleteByDocumentIdAsync(document.Id, cancellationToken);
        await storageService.DeleteAsync(document.StoragePath, cancellationToken);

        context.Documents.Remove(document);
        await context.SaveChangesAsync(cancellationToken);
    }
}
