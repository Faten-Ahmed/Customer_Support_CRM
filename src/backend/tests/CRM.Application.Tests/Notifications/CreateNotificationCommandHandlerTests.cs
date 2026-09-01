using CRM.Application.Notifications;
using CRM.Application.Notifications.Commands;
using CRM.Application.Notifications.Queries;
using CRM.Domain.Notifications;
using Microsoft.Extensions.Caching.Distributed;
using Moq;
using Xunit;

namespace CRM.Application.Tests.Notifications;

public class CreateNotificationCommandHandlerTests
{
    private readonly Mock<INotificationRepository> _repo = new();
    private readonly Mock<INotificationPushService> _push = new();
    private readonly Mock<IDistributedCache> _cache = new();
    private readonly CreateNotificationCommandHandler _handler;

    public CreateNotificationCommandHandlerTests()
    {
        _handler = new CreateNotificationCommandHandler(_repo.Object, _push.Object, _cache.Object);
    }

    [Fact]
    public async Task Handle_NewNotification_PersistsIt()
    {
        var userId = Guid.NewGuid();
        var entityId = Guid.NewGuid();

        _repo.Setup(r => r.ExistsAsync(
            userId, NotificationType.TicketAssigned, entityId, default))
             .ReturnsAsync(false);

        var cmd = new CreateNotificationCommand(
            userId, NotificationType.TicketAssigned,
            "Ticket Assigned", "TKT-001 was assigned to you.",
            "Ticket", entityId);

        var id = await _handler.Handle(cmd, default);

        _repo.Verify(r => r.AddAsync(It.IsAny<Notification>(), default), Times.Once);
        _repo.Verify(r => r.SaveChangesAsync(default), Times.Once);
        Assert.NotEqual(Guid.Empty, id);
    }

    [Theory]
    [InlineData(NotificationType.SlaWarning)]
    [InlineData(NotificationType.SlaBreached)]
    [InlineData(NotificationType.SlaCriticalBreach)]
    public async Task Handle_SlaNotificationAlreadyExists_SkipsPersist(
        NotificationType type)
    {
        var userId = Guid.NewGuid();
        var entityId = Guid.NewGuid();

        _repo.Setup(r => r.ExistsAsync(userId, type, entityId, default))
             .ReturnsAsync(true);

        var cmd = new CreateNotificationCommand(
            userId, type, "SLA Warning", "Body.", "Ticket", entityId);

        var id = await _handler.Handle(cmd, default);

        _repo.Verify(r => r.AddAsync(It.IsAny<Notification>(), default), Times.Never);
        Assert.Equal(Guid.Empty, id);
    }

    [Theory]
    [InlineData(NotificationType.TicketAssigned)]
    [InlineData(NotificationType.NewMessage)]
    [InlineData(NotificationType.KbArticlePublished)]
    public async Task Handle_NonSlaNotification_DoesNotCheckDuplicate(
        NotificationType type)
    {
        var userId = Guid.NewGuid();
        var entityId = Guid.NewGuid();

        var cmd = new CreateNotificationCommand(
            userId, type, "Title", "Body.", "Ticket", entityId);

        await _handler.Handle(cmd, default);

        _repo.Verify(r => r.ExistsAsync(
            It.IsAny<Guid>(), It.IsAny<NotificationType>(), It.IsAny<Guid>(), default),
            Times.Never);
        _repo.Verify(r => r.AddAsync(It.IsAny<Notification>(), default), Times.Once);
    }
}
