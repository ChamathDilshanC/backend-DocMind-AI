using DocumentAssistant.Application.Common.Exceptions;
using DocumentAssistant.Application.Common.Interfaces;
using DocumentAssistant.Application.Features.Documents.Common;
using DocumentAssistant.Domain.Exceptions;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace DocumentAssistant.Application.Features.Documents.Commands;

public record RenameDocumentCommand(Guid DocumentId, string NewName) : IRequest<DocumentDto>;

public class RenameDocumentCommandValidator : AbstractValidator<RenameDocumentCommand>
{
    public RenameDocumentCommandValidator()
    {
        RuleFor(x => x.NewName).NotEmpty().MaximumLength(300);
    }
}

public class RenameDocumentCommandHandler(IApplicationDbContext context, ICurrentUserService currentUserService)
    : IRequestHandler<RenameDocumentCommand, DocumentDto>
{
    public async Task<DocumentDto> Handle(RenameDocumentCommand request, CancellationToken cancellationToken)
    {
        var userId = currentUserService.UserId ?? throw new UnauthorizedException("Not authenticated.");

        var document = await context.Documents.FirstOrDefaultAsync(d => d.Id == request.DocumentId, cancellationToken)
            ?? throw new NotFoundException(nameof(Domain.Entities.Document), request.DocumentId);

        if (document.UserId != userId)
        {
            throw new ForbiddenAccessException();
        }

        document.Name = request.NewName.Trim();
        await context.SaveChangesAsync(cancellationToken);

        return DocumentMapper.ToDto(document);
    }
}
