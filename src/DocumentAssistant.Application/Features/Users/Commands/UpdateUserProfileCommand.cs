using DocumentAssistant.Application.Common.Exceptions;
using DocumentAssistant.Application.Common.Interfaces;
using DocumentAssistant.Application.Features.Users.Queries;
using DocumentAssistant.Domain.Exceptions;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace DocumentAssistant.Application.Features.Users.Commands;

public record UpdateUserProfileCommand(string Name) : IRequest<UserProfileDto>;

public class UpdateUserProfileCommandValidator : AbstractValidator<UpdateUserProfileCommand>
{
    public UpdateUserProfileCommandValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(200);
    }
}

public class UpdateUserProfileCommandHandler(IApplicationDbContext context, ICurrentUserService currentUserService)
    : IRequestHandler<UpdateUserProfileCommand, UserProfileDto>
{
    public async Task<UserProfileDto> Handle(UpdateUserProfileCommand request, CancellationToken cancellationToken)
    {
        var userId = currentUserService.UserId ?? throw new UnauthorizedException("Not authenticated.");

        var user = await context.Users.FirstOrDefaultAsync(u => u.Id == userId, cancellationToken)
            ?? throw new NotFoundException(nameof(Domain.Entities.User), userId);

        user.Name = request.Name.Trim();
        await context.SaveChangesAsync(cancellationToken);

        return new UserProfileDto(
            user.Id, user.Name, user.Email, user.Role.ToString(),
            user.PasswordHash is not null, user.GoogleId is not null, user.EmailVerified, user.CreatedAt);
    }
}
