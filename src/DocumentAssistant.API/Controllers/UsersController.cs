using DocumentAssistant.Application.Features.Users.Commands;
using DocumentAssistant.Application.Features.Users.Queries;
using DocumentAssistant.Shared;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace DocumentAssistant.API.Controllers;

public record UpdateProfileRequest(string Name);
public record ChangePasswordRequest(string? CurrentPassword, string NewPassword);

[ApiController]
[Route("api/users")]
[Authorize]
public class UsersController(ISender sender) : ControllerBase
{
    [HttpGet("me")]
    public async Task<ActionResult<UserProfileDto>> GetProfile(CancellationToken cancellationToken)
    {
        return Ok(await sender.Send(new GetUserProfileQuery(), cancellationToken));
    }

    [HttpPut("me")]
    public async Task<ActionResult<UserProfileDto>> UpdateProfile(UpdateProfileRequest request, CancellationToken cancellationToken)
    {
        return Ok(await sender.Send(new UpdateUserProfileCommand(request.Name), cancellationToken));
    }

    [HttpPost("me/change-password")]
    public async Task<IActionResult> ChangePassword(ChangePasswordRequest request, CancellationToken cancellationToken)
    {
        await sender.Send(new ChangePasswordCommand(request.CurrentPassword, request.NewPassword), cancellationToken);
        return NoContent();
    }

    [HttpDelete("me")]
    public async Task<IActionResult> DeleteAccount(CancellationToken cancellationToken)
    {
        await sender.Send(new DeleteUserCommand(), cancellationToken);
        return NoContent();
    }

    [HttpGet]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<PaginatedList<UserListItemDto>>> GetAllUsers(
        [FromQuery] int pageNumber = 1, [FromQuery] int pageSize = 20, CancellationToken cancellationToken = default)
    {
        return Ok(await sender.Send(new GetAllUsersQuery(pageNumber, pageSize), cancellationToken));
    }
}
