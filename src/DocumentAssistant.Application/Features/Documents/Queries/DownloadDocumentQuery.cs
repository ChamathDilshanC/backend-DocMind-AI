using DocumentAssistant.Application.Common.Exceptions;
using DocumentAssistant.Application.Common.Interfaces;
using DocumentAssistant.Domain.Exceptions;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace DocumentAssistant.Application.Features.Documents.Queries;

public record DownloadDocumentResult(Stream Content, string FileName, string ContentType);

public record DownloadDocumentQuery(Guid DocumentId) : IRequest<DownloadDocumentResult>;

public class DownloadDocumentQueryHandler(IApplicationDbContext context, IStorageService storageService, ICurrentUserService currentUserService)
    : IRequestHandler<DownloadDocumentQuery, DownloadDocumentResult>
{
    public async Task<DownloadDocumentResult> Handle(DownloadDocumentQuery request, CancellationToken cancellationToken)
    {
        var userId = currentUserService.UserId ?? throw new UnauthorizedException("Not authenticated.");

        var document = await context.Documents.FirstOrDefaultAsync(d => d.Id == request.DocumentId, cancellationToken)
            ?? throw new NotFoundException(nameof(Domain.Entities.Document), request.DocumentId);

        if (document.UserId != userId)
        {
            throw new ForbiddenAccessException();
        }

        var stream = await storageService.OpenReadAsync(document.StoragePath, cancellationToken);
        var contentType = document.FileType == Domain.Enums.DocumentFileType.Pdf
            ? "application/pdf"
            : "application/vnd.openxmlformats-officedocument.wordprocessingml.document";

        return new DownloadDocumentResult(stream, document.OriginalFileName, contentType);
    }
}
