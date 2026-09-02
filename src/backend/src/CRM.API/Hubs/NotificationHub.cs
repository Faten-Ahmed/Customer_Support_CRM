using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using System.Security.Claims;

namespace CRM.API.Hubs;

[Authorize]
public class NotificationHub : Hub
{
    public override async Task OnConnectedAsync()
    {
        var userId = Context.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value
            ?? Context.User?.FindFirst("sub")?.Value;
        if (userId is not null)
            await Groups.AddToGroupAsync(Context.ConnectionId, $"user-{userId}");

        var role = Context.User?.FindFirst(ClaimTypes.Role)?.Value;
        if (role is "Agent" or "Manager")
            await Groups.AddToGroupAsync(Context.ConnectionId, "live-chat-agents");

        await base.OnConnectedAsync();
    }

    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        var userId = Context.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value
            ?? Context.User?.FindFirst("sub")?.Value;
        if (userId is not null)
            await Groups.RemoveFromGroupAsync(Context.ConnectionId, $"user-{userId}");

        var role = Context.User?.FindFirst(ClaimTypes.Role)?.Value;
        if (role is "Agent" or "Manager")
            await Groups.RemoveFromGroupAsync(Context.ConnectionId, "live-chat-agents");

        await base.OnDisconnectedAsync(exception);
    }
}
