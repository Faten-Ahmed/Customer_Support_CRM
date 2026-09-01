using CRM.Domain.Notifications;
using MediatR;
using Microsoft.Extensions.Caching.Distributed;
using System.Text;

namespace CRM.Application.Notifications.Queries;

public record GetUnreadCountQuery(Guid RequestingUserId) : IRequest<int>;

public class GetUnreadCountQueryHandler : IRequestHandler<GetUnreadCountQuery, int>
{
    private readonly INotificationRepository _notifications;
    private readonly IDistributedCache _cache;

    private static string CacheKey(Guid userId) => $"notification:unread:{userId}";
    private static readonly TimeSpan Ttl = TimeSpan.FromSeconds(60);

    public GetUnreadCountQueryHandler(
        INotificationRepository notifications,
        IDistributedCache cache)
    {
        _notifications = notifications;
        _cache = cache;
    }

    public async Task<int> Handle(GetUnreadCountQuery query, CancellationToken ct)
    {
        var key = CacheKey(query.RequestingUserId);
        var cached = await _cache.GetAsync(key, ct);

        if (cached is not null)
            return int.Parse(Encoding.UTF8.GetString(cached));

        var count = await _notifications.GetUnreadCountAsync(query.RequestingUserId, ct);

        await _cache.SetAsync(
            key,
            Encoding.UTF8.GetBytes(count.ToString()),
            new DistributedCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = Ttl
            },
            ct);

        return count;
    }

    public static Task InvalidateAsync(IDistributedCache cache, Guid userId,
        CancellationToken ct = default)
        => cache.RemoveAsync(CacheKey(userId), ct);
}
