using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;

namespace DocumentAssistant.API.Hubs;

/// <summary>Single hub for both document-processing progress and chat-token streaming, per user group.</summary>
[Authorize]
public class AppHub : Hub
{
    public static string GroupNameFor(Guid userId) => $"user-{userId}";

    public override async Task OnConnectedAsync()
    {
        var userId = Context.User?.FindFirstValue(ClaimTypes.NameIdentifier) ?? Context.User?.FindFirstValue("sub");
        if (userId is not null)
        {
            await Groups.AddToGroupAsync(Context.ConnectionId, GroupNameFor(Guid.Parse(userId)));
        }

        await base.OnConnectedAsync();
    }
}
