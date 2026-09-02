using CRM.API.Hubs;
using CRM.Application.Notifications;
using CRM.Application.Notifications.DTOs;
using Microsoft.AspNetCore.SignalR;

namespace CRM.API.Services;

public class NotificationPushService : INotificationPushService
{
    private readonly IHubContext<NotificationHub> _hub;

    public NotificationPushService(IHubContext<NotificationHub> hub) => _hub = hub;

    public Task PushNotificationAsync(Guid userId, NotificationDto notification,
        CancellationToken ct = default)
        => _hub.Clients.Group($"user-{userId}")
               .SendAsync("ReceiveNotification", notification, ct);

    public Task PushUnreadCountAsync(Guid userId, int count,
        CancellationToken ct = default)
        => _hub.Clients.Group($"user-{userId}")
               .SendAsync("UnreadCountUpdated", count, ct);

    public Task NotifyLiveChatHandoffAsync(Guid sessionId, string customerName,
        CancellationToken ct = default)
        => _hub.Clients.Group("live-chat-agents")
               .SendAsync("LiveChatHandoffRequested",
                   new { SessionId = sessionId, CustomerName = customerName }, ct);
}
