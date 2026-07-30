using DocumentAssistant.Application.Common.Exceptions;
using DocumentAssistant.Application.Common.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace DocumentAssistant.Application.Features.Documents.Commands;

public record DeleteAllDocumentsCommand : IRequest;

public class DeleteAllDocumentsCommandHandler(
    IApplicationDbContext context, IStorageService storageService, IVectorStoreService vectorStoreService,
    ICurrentUserService currentUserService)
    : IRequestHandler<DeleteAllDocumentsCommand>
{
    public async Task Handle(DeleteAllDocumentsCommand request, CancellationToken cancellationToken)
    {
        var userId = currentUserService.UserId ?? throw new UnauthorizedException("Not authenticated.");

        var documents = await context.Documents.Where(d => d.UserId == userId).ToListAsync(cancellationToken);

        foreach (var document in documents)
        {
            await vectorStoreService.DeleteByDocumentIdAsync(document.Id, cancellationToken);
            await storageService.DeleteAsync(document.StoragePath, cancellationToken);
        }

        context.Documents.RemoveRange(documents);
        await context.SaveChangesAsync(cancellationToken);
    }
}
