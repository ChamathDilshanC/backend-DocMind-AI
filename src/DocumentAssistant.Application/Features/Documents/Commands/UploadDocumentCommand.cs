using DocumentAssistant.Application.Common.Exceptions;
using DocumentAssistant.Application.Common.Interfaces;
using DocumentAssistant.Application.Common.Models;
using DocumentAssistant.Application.Features.Documents.Common;
using DocumentAssistant.Domain.Entities;
using DocumentAssistant.Domain.Enums;
using FluentValidation;
using MediatR;

namespace DocumentAssistant.Application.Features.Documents.Commands;

public record UploadDocumentCommand(string FileName, long FileSizeBytes, Stream Content) : IRequest<DocumentDto>;

public class UploadDocumentCommandValidator : AbstractValidator<UploadDocumentCommand>
{
    public UploadDocumentCommandValidator()
    {
        RuleFor(x => x.FileName).NotEmpty().MaximumLength(300);
        RuleFor(x => x.FileSizeBytes).GreaterThan(0).LessThanOrEqualTo(FileValidator.MaxFileSizeBytes);
    }
}

public class UploadDocumentCommandHandler(
    IApplicationDbContext context, IStorageService storageService, IBackgroundJobEnqueuer backgroundJobEnqueuer,
    ICurrentUserService currentUserService)
    : IRequestHandler<UploadDocumentCommand, DocumentDto>
{
    public async Task<DocumentDto> Handle(UploadDocumentCommand request, CancellationToken cancellationToken)
    {
        var userId = currentUserService.UserId ?? throw new UnauthorizedException("Not authenticated.");

        var validation = await FileValidator.ValidateAsync(request.FileName, request.FileSizeBytes, request.Content, cancellationToken);
        if (!validation.IsValid)
        {
            throw new ValidationException([new FluentValidation.Results.ValidationFailure(nameof(request.FileName), validation.Error)]);
        }

        var document = new Document
        {
            UserId = userId,
            Name = request.FileName,
            OriginalFileName = request.FileName,
            FileType = validation.FileType!.Value,
            FileSizeBytes = request.FileSizeBytes,
            Status = DocumentStatus.Uploaded
        };

        document.StoragePath = await storageService.SaveAsync(userId, document.Id, request.FileName, request.Content, cancellationToken);

        context.Documents.Add(document);
        await context.SaveChangesAsync(cancellationToken);

        backgroundJobEnqueuer.EnqueueDocumentProcessing(document.Id);

        return DocumentMapper.ToDto(document);
    }
}
