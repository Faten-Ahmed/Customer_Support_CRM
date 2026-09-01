using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using System.Security.Claims;

namespace CRM.Infrastructure.Hubs;

[Authorize]
public class DashboardHub : Hub
{
    public override async Task OnConnectedAsync()
    {
        var role = Context.User?.FindFirst(ClaimTypes.Role)?.Value ?? "Agent";

        // Role-scoped groups: "kpi-admin", "kpi-manager", "kpi-agent"
        await Groups.AddToGroupAsync(Context.ConnectionId, $"kpi-{role.ToLower()}");
        await base.OnConnectedAsync();
    }

    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        var role = Context.User?.FindFirst(ClaimTypes.Role)?.Value ?? "Agent";
        await Groups.RemoveFromGroupAsync(Context.ConnectionId, $"kpi-{role.ToLower()}");
        await base.OnDisconnectedAsync(exception);
    }
}
