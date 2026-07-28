using DocumentAssistant.Application.Common.Interfaces;
using DocumentAssistant.Application.Common.Models;
using DocumentAssistant.Domain.Exceptions;
using DocumentAssistant.Shared;
using MediatR;

namespace DocumentAssistant.Application.Features.Users.Queries;

public record UserListItemDto(Guid Id, string Name, string Email, string Role, DateTime CreatedAt);

public record GetAllUsersQuery(int PageNumber = 1, int PageSize = 20) : IRequest<PaginatedList<UserListItemDto>>;

public class GetAllUsersQueryHandler(IApplicationDbContext context, ICurrentUserService currentUserService)
    : IRequestHandler<GetAllUsersQuery, PaginatedList<UserListItemDto>>
{
    public async Task<PaginatedList<UserListItemDto>> Handle(GetAllUsersQuery request, CancellationToken cancellationToken)
    {
        if (!currentUserService.IsAdmin)
        {
            throw new ForbiddenAccessException();
        }

        return await context.Users
            .OrderBy(u => u.CreatedAt)
            .Select(u => new UserListItemDto(u.Id, u.Name, u.Email, u.Role.ToString(), u.CreatedAt))
            .ToPaginatedListAsync(request.PageNumber, request.PageSize, cancellationToken);
    }
}
