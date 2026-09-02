using CRM.Application.Notifications.DTOs;

namespace CRM.Application.Notifications;

public interface INotificationPushService
{
    Task PushNotificationAsync(Guid userId, NotificationDto notification,
        CancellationToken ct = default);
    Task PushUnreadCountAsync(Guid userId, int count,
        CancellationToken ct = default);
    Task NotifyLiveChatHandoffAsync(Guid sessionId, string customerName,
        CancellationToken ct = default);
}
