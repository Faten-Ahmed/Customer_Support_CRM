using CRM.Domain.Notifications;
using MediatR;

namespace CRM.Application.Notifications.Commands;

public record MarkNotificationReadCommand(
    Guid NotificationId,
    Guid RequestingUserId) : IRequest<MarkNotificationReadResult>;

public record MarkNotificationReadResult(Guid Id, bool IsRead, DateTime? ReadAt);

public class MarkNotificationReadCommandHandler
    : IRequestHandler<MarkNotificationReadCommand, MarkNotificationReadResult>
{
    private readonly INotificationRepository _notifications;
    private readonly INotificationPushService _push;

    public MarkNotificationReadCommandHandler(
        INotificationRepository notifications,
        INotificationPushService push)
    {
        _notifications = notifications;
        _push = push;
    }

    public async Task<MarkNotificationReadResult> Handle(
        MarkNotificationReadCommand cmd, CancellationToken ct)
    {
        var notification = await _notifications.FindByIdAsync(cmd.NotificationId, ct)
            ?? throw new KeyNotFoundException(
                $"Notification {cmd.NotificationId} not found.");

        if (notification.UserId != cmd.RequestingUserId)
            throw new UnauthorizedAccessException(
                "You can only mark your own notifications as read.");

        notification.MarkRead();
        await _notifications.SaveChangesAsync(ct);

        var newCount = await _notifications.GetUnreadCountAsync(cmd.RequestingUserId, ct);
        await _push.PushUnreadCountAsync(cmd.RequestingUserId, newCount, ct);

        return new MarkNotificationReadResult(
            notification.Id, notification.IsRead, notification.ReadAt);
    }
}
