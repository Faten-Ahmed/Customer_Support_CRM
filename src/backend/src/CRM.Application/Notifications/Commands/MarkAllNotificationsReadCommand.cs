using CRM.Application.Notifications.Queries;
using CRM.Domain.Notifications;
using MediatR;
using Microsoft.Extensions.Caching.Distributed;

namespace CRM.Application.Notifications.Commands;

public record MarkAllNotificationsReadCommand(Guid RequestingUserId) : IRequest<int>;

public class MarkAllNotificationsReadCommandHandler
    : IRequestHandler<MarkAllNotificationsReadCommand, int>
{
    private readonly INotificationRepository _notifications;
    private readonly INotificationPushService _push;
    private readonly IDistributedCache _cache;

    public MarkAllNotificationsReadCommandHandler(
        INotificationRepository notifications,
        INotificationPushService push,
        IDistributedCache cache)
    {
        _notifications = notifications;
        _push = push;
        _cache = cache;
    }

    public async Task<int> Handle(MarkAllNotificationsReadCommand cmd, CancellationToken ct)
    {
        int count = await _notifications.MarkAllReadAsync(cmd.RequestingUserId, ct);
        await _notifications.SaveChangesAsync(ct);

        await GetUnreadCountQueryHandler.InvalidateAsync(_cache, cmd.RequestingUserId, ct);

        var newCount = await _notifications.GetUnreadCountAsync(cmd.RequestingUserId, ct);
        await _push.PushUnreadCountAsync(cmd.RequestingUserId, newCount, ct);

        return count;
    }
}
