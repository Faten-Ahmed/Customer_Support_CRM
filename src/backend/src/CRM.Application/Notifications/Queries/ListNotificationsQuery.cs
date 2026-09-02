using CRM.Application.Notifications.DTOs;
using CRM.Domain.Common;
using CRM.Domain.Notifications;
using CRM.Domain.Users;
using MediatR;

namespace CRM.Application.Notifications.Queries;

public record ListNotificationsQuery(
    Guid RequestingUserId,
    UserRole RequestingUserRole,
    bool? IsRead,
    NotificationType? Type,
    int Page,
    int PageSize,
    bool All) : IRequest<PagedResult<NotificationDto>>;

public class ListNotificationsQueryHandler
    : IRequestHandler<ListNotificationsQuery, PagedResult<NotificationDto>>
{
    private readonly INotificationRepository _notifications;

    public ListNotificationsQueryHandler(INotificationRepository notifications)
        => _notifications = notifications;

    public async Task<PagedResult<NotificationDto>> Handle(
        ListNotificationsQuery query, CancellationToken ct)
    {
        bool includeAll = query.All &&
            query.RequestingUserRole is UserRole.Admin;

        var paged = await _notifications.ListAsync(
            query.RequestingUserId,
            query.IsRead,
            query.Type,
            includeAll,
            query.Page,
            query.PageSize,
            ct);

        var dtos = paged.Items
            .Select(n => new NotificationDto(
                n.Id, n.Type.ToString(), n.Title, n.Body,
                n.EntityType, n.EntityId, n.IsRead, n.ReadAt, n.CreatedAt))
            .ToList();

        return new PagedResult<NotificationDto>(
            dtos, paged.TotalCount, paged.Page, paged.PageSize);
    }
}
