using CRM.Application.Notifications;
using CRM.Application.Notifications.Commands;
using CRM.Application.Notifications.DTOs;
using CRM.Application.Notifications.Queries;
using CRM.Domain.Notifications;
using Microsoft.Extensions.Caching.Distributed;
using Moq;
using Xunit;

namespace CRM.Application.Tests.Notifications;

public class CreateNotificationCommandHandlerPushTests
{
    private readonly Mock<INotificationRepository> _repo = new();
    private readonly Mock<INotificationPushService> _push = new();
    private readonly Mock<IDistributedCache> _cache = new();
    private readonly CreateNotificationCommandHandler _handler;

    public CreateNotificationCommandHandlerPushTests()
    {
        _handler = new CreateNotificationCommandHandler(_repo.Object, _push.Object, _cache.Object);
    }

    [Fact]
    public async Task Handle_NewNotification_PushesRealTimeNotification()
    {
        var userId = Guid.NewGuid();
        var entityId = Guid.NewGuid();
        _repo.Setup(r => r.GetUnreadCountAsync(userId, default)).ReturnsAsync(3);

        var cmd = new CreateNotificationCommand(
            userId, NotificationType.TicketAssigned,
            "Ticket Assigned", "TKT-001 was assigned to you.",
            "Ticket", entityId);

        await _handler.Handle(cmd, default);

        _push.Verify(p => p.PushNotificationAsync(
            userId,
            It.Is<NotificationDto>(d => d.Type == "TicketAssigned"),
            default), Times.Once);
        _push.Verify(p => p.PushUnreadCountAsync(userId, 3, default), Times.Once);
    }

    [Fact]
    public async Task Handle_SlaNotificationAlreadyExists_DoesNotPush()
    {
        var userId = Guid.NewGuid();
        var entityId = Guid.NewGuid();
        _repo.Setup(r => r.ExistsAsync(userId, NotificationType.SlaWarning, entityId, default))
             .ReturnsAsync(true);

        var cmd = new CreateNotificationCommand(
            userId, NotificationType.SlaWarning,
            "SLA Warning", "Body.", "Ticket", entityId);

        await _handler.Handle(cmd, default);

        _push.Verify(p => p.PushNotificationAsync(
            It.IsAny<Guid>(), It.IsAny<NotificationDto>(), default), Times.Never);
    }
}
