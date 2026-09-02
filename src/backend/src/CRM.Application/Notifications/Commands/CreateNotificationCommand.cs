using CRM.Application.Notifications.DTOs;
using CRM.Application.Notifications.Queries;
using CRM.Domain.Notifications;
using MediatR;
using Microsoft.Extensions.Caching.Distributed;

namespace CRM.Application.Notifications.Commands;

public record CreateNotificationCommand(
    Guid UserId,
    NotificationType Type,
    string Title,
    string Body,
    string EntityType,
    Guid EntityId) : IRequest<Guid>;

public class CreateNotificationCommandHandler : IRequestHandler<CreateNotificationCommand, Guid>
{
    private static readonly HashSet<NotificationType> _slaTypes =
    [
        NotificationType.SlaWarning,
        NotificationType.SlaBreached,
        NotificationType.SlaCriticalBreach
    ];

    private readonly INotificationRepository _notifications;
    private readonly INotificationPushService _push;
    private readonly IDistributedCache _cache;

    public CreateNotificationCommandHandler(
        INotificationRepository notifications,
        INotificationPushService push,
        IDistributedCache cache)
    {
        _notifications = notifications;
        _push = push;
        _cache = cache;
    }

    public async Task<Guid> Handle(CreateNotificationCommand cmd, CancellationToken ct)
    {
        if (_slaTypes.Contains(cmd.Type))
        {
            bool exists = await _notifications.ExistsAsync(
                cmd.UserId, cmd.Type, cmd.EntityId, ct);
            if (exists) return Guid.Empty;
        }

        var notification = Notification.Create(
            cmd.UserId, cmd.Type, cmd.Title, cmd.Body, cmd.EntityType, cmd.EntityId);

        await _notifications.AddAsync(notification, ct);
        await _notifications.SaveChangesAsync(ct);

        await GetUnreadCountQueryHandler.InvalidateAsync(_cache, cmd.UserId, ct);

        var count = await _notifications.GetUnreadCountAsync(cmd.UserId, ct);

        var dto = new NotificationDto(
            notification.Id, notification.Type.ToString(),
            notification.Title, notification.Body,
            notification.EntityType, notification.EntityId,
            notification.IsRead, notification.ReadAt, notification.CreatedAt);

        await _push.PushNotificationAsync(cmd.UserId, dto, ct);
        await _push.PushUnreadCountAsync(cmd.UserId, count, ct);

        return notification.Id;
    }
}
