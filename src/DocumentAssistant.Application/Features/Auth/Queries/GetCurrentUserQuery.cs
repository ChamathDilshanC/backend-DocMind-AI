using DocumentAssistant.Application.Common.Exceptions;
using DocumentAssistant.Application.Common.Interfaces;
using DocumentAssistant.Application.Features.Auth.Common;
using DocumentAssistant.Domain.Exceptions;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace DocumentAssistant.Application.Features.Auth.Queries;

public record GetCurrentUserQuery : IRequest<UserDto>;

public class GetCurrentUserQueryHandler(IApplicationDbContext context, ICurrentUserService currentUserService)
    : IRequestHandler<GetCurrentUserQuery, UserDto>
{
    public async Task<UserDto> Handle(GetCurrentUserQuery request, CancellationToken cancellationToken)
    {
        var userId = currentUserService.UserId ?? throw new UnauthorizedException("Not authenticated.");

        var user = await context.Users.FirstOrDefaultAsync(u => u.Id == userId, cancellationToken)
            ?? throw new NotFoundException(nameof(Domain.Entities.User), userId);

        return new UserDto(user.Id, user.Name, user.Email, user.Role.ToString(), user.AvatarUrl);
    }
}
