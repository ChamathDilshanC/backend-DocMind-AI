using DocumentAssistant.Application.Common.Exceptions;
using DocumentAssistant.Application.Common.Interfaces;
using DocumentAssistant.Domain.Exceptions;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace DocumentAssistant.Application.Features.Users.Queries;

public record UserProfileDto(Guid Id, string Name, string Email, string Role, bool HasPassword, bool HasGoogleLinked, bool EmailVerified, DateTime CreatedAt);

public record GetUserProfileQuery : IRequest<UserProfileDto>;

public class GetUserProfileQueryHandler(IApplicationDbContext context, ICurrentUserService currentUserService)
    : IRequestHandler<GetUserProfileQuery, UserProfileDto>
{
    public async Task<UserProfileDto> Handle(GetUserProfileQuery request, CancellationToken cancellationToken)
    {
        var userId = currentUserService.UserId ?? throw new UnauthorizedException("Not authenticated.");

        var user = await context.Users.FirstOrDefaultAsync(u => u.Id == userId, cancellationToken)
            ?? throw new NotFoundException(nameof(Domain.Entities.User), userId);

        return new UserProfileDto(
            user.Id, user.Name, user.Email, user.Role.ToString(),
            user.PasswordHash is not null, user.GoogleId is not null, user.EmailVerified, user.CreatedAt);
    }
}
