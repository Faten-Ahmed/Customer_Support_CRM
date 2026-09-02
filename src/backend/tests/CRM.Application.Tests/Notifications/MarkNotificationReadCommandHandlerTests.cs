using CRM.Application.Notifications;
using CRM.Application.Notifications.Commands;
using CRM.Domain.Notifications;
using Microsoft.Extensions.Caching.Distributed;
using Moq;
using Xunit;

namespace CRM.Application.Tests.Notifications;

public class MarkNotificationReadCommandHandlerTests
{
    private readonly Mock<INotificationRepository> _repo = new();
    private readonly Mock<INotificationPushService> _push = new();
    private readonly Mock<IDistributedCache> _cache = new();
    private readonly MarkNotificationReadCommandHandler _handler;
    private readonly MarkAllNotificationsReadCommandHandler _markAllHandler;

    public MarkNotificationReadCommandHandlerTests()
    {
        _handler = new MarkNotificationReadCommandHandler(_repo.Object, _push.Object);
        _markAllHandler = new MarkAllNotificationsReadCommandHandler(_repo.Object, _push.Object, _cache.Object);
    }

    [Fact]
    public async Task Handle_OwnerMarksRead_MarksAndPushesCount()
    {
        var userId = Guid.NewGuid();
        var notification = Notification.Create(
            userId, NotificationType.NewMessage, "New Message", "Body", "Ticket", Guid.NewGuid());
        _repo.Setup(r => r.FindByIdAsync(notification.Id, default)).ReturnsAsync(notification);
        _repo.Setup(r => r.GetUnreadCountAsync(userId, default)).ReturnsAsync(0);

        var result = await _handler.Handle(
            new MarkNotificationReadCommand(notification.Id, userId), default);

        Assert.True(result.IsRead);
        Assert.NotNull(result.ReadAt);
        _repo.Verify(r => r.SaveChangesAsync(default), Times.Once);
        _push.Verify(p => p.PushUnreadCountAsync(userId, 0, default), Times.Once);
    }

    [Fact]
    public async Task Handle_WrongUser_ThrowsUnauthorizedAccessException()
    {
        var ownerId = Guid.NewGuid();
        var otherId = Guid.NewGuid();
        var notification = Notification.Create(
            ownerId, NotificationType.NewMessage, "New Message", "Body", "Ticket", Guid.NewGuid());
        _repo.Setup(r => r.FindByIdAsync(notification.Id, default)).ReturnsAsync(notification);

        await Assert.ThrowsAsync<UnauthorizedAccessException>(() =>
            _handler.Handle(
                new MarkNotificationReadCommand(notification.Id, otherId), default));
    }

    [Fact]
    public async Task Handle_NotFound_ThrowsKeyNotFoundException()
    {
        _repo.Setup(r => r.FindByIdAsync(It.IsAny<Guid>(), default))
             .ReturnsAsync((Notification?)null);

        await Assert.ThrowsAsync<KeyNotFoundException>(() =>
            _handler.Handle(
                new MarkNotificationReadCommand(Guid.NewGuid(), Guid.NewGuid()), default));
    }

    [Fact]
    public async Task MarkAll_ReturnsCountAndPushesUpdatedCount()
    {
        var userId = Guid.NewGuid();
        _repo.Setup(r => r.MarkAllReadAsync(userId, default)).ReturnsAsync(5);
        _repo.Setup(r => r.GetUnreadCountAsync(userId, default)).ReturnsAsync(0);

        var markedRead = await _markAllHandler.Handle(
            new MarkAllNotificationsReadCommand(userId), default);

        Assert.Equal(5, markedRead);
        _push.Verify(p => p.PushUnreadCountAsync(userId, 0, default), Times.Once);
    }
}
