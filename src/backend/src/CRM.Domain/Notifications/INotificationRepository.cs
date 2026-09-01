using CRM.Domain.Common;

namespace CRM.Domain.Notifications;

public interface INotificationRepository
{
    Task<Notification?> FindByIdAsync(Guid id, CancellationToken ct = default);
    Task<PagedResult<Notification>> ListAsync(
        Guid userId,
        bool? isRead,
        NotificationType? type,
        bool includeOlderThan90Days,
        int page,
        int pageSize,
        CancellationToken ct = default);
    Task<int> GetUnreadCountAsync(Guid userId, CancellationToken ct = default);
    Task<int> MarkAllReadAsync(Guid userId, CancellationToken ct = default);
    Task<bool> ExistsAsync(
        Guid userId, NotificationType type, Guid entityId,
        CancellationToken ct = default);
    Task AddAsync(Notification notification, CancellationToken ct = default);
    Task SaveChangesAsync(CancellationToken ct = default);
}
