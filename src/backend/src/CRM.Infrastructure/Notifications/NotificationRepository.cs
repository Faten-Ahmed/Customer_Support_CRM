using CRM.Domain.Common;
using CRM.Domain.Notifications;
using CRM.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace CRM.Infrastructure.Persistence.Repositories;

public class NotificationRepository : INotificationRepository
{
    private readonly AppDbContext _db;
    public NotificationRepository(AppDbContext db) => _db = db;

    public Task<Notification?> FindByIdAsync(Guid id, CancellationToken ct = default)
        => _db.Notifications.FirstOrDefaultAsync(n => n.Id == id, ct);

    public async Task<PagedResult<Notification>> ListAsync(
        Guid userId,
        bool? isRead,
        NotificationType? type,
        bool includeOlderThan90Days,
        int page,
        int pageSize,
        CancellationToken ct = default)
    {
        var query = _db.Notifications
            .Where(n => n.UserId == userId)
            .AsQueryable();

        if (!includeOlderThan90Days)
        {
            var cutoff = DateTime.UtcNow.AddDays(-90);
            query = query.Where(n => n.CreatedAt >= cutoff);
        }

        if (isRead.HasValue)
            query = query.Where(n => n.IsRead == isRead.Value);

        if (type.HasValue)
            query = query.Where(n => n.Type == type.Value);

        var total = await query.CountAsync(ct);
        var items = await query
            .OrderByDescending(n => n.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(ct);

        return new PagedResult<Notification>(items, total, page, pageSize);
    }

    public Task<int> GetUnreadCountAsync(Guid userId, CancellationToken ct = default)
        => _db.Notifications.CountAsync(n => n.UserId == userId && !n.IsRead, ct);

    public async Task<int> MarkAllReadAsync(Guid userId, CancellationToken ct = default)
    {
        var unread = await _db.Notifications
            .Where(n => n.UserId == userId && !n.IsRead)
            .ToListAsync(ct);

        foreach (var n in unread)
            n.MarkRead();

        return unread.Count;
    }

    public Task<bool> ExistsAsync(
        Guid userId, NotificationType type, Guid entityId,
        CancellationToken ct = default)
        => _db.Notifications.AnyAsync(
            n => n.UserId == userId && n.Type == type && n.EntityId == entityId && !n.IsRead, ct);

    public async Task AddAsync(Notification notification, CancellationToken ct = default)
        => await _db.Notifications.AddAsync(notification, ct);

    public Task SaveChangesAsync(CancellationToken ct = default)
        => _db.SaveChangesAsync(ct);
}
