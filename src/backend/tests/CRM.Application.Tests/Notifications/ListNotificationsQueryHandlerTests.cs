using CRM.Application.Notifications.Queries;
using CRM.Domain.Common;
using CRM.Domain.Notifications;
using CRM.Domain.Users;
using Moq;
using Xunit;

namespace CRM.Application.Tests.Notifications;

public class ListNotificationsQueryHandlerTests
{
    private readonly Mock<INotificationRepository> _repo = new();
    private readonly ListNotificationsQueryHandler _handler;

    public ListNotificationsQueryHandlerTests()
    {
        _handler = new ListNotificationsQueryHandler(_repo.Object);
    }

    [Fact]
    public async Task Handle_DefaultQuery_PassesLast90DaysFilter()
    {
        var userId = Guid.NewGuid();
        _repo.Setup(r => r.ListAsync(
            userId, null, null, false, 1, 20, default))
             .ReturnsAsync(new PagedResult<Notification>(
                 new List<Notification>(), 0, 1, 20));

        var result = await _handler.Handle(
            new ListNotificationsQuery(userId, UserRole.Agent,
                null, null, 1, 20, All: false), default);

        Assert.Equal(0, result.TotalCount);
        _repo.Verify(r => r.ListAsync(userId, null, null, false, 1, 20, default), Times.Once);
    }

    [Fact]
    public async Task Handle_AllTrueByAdmin_PassesIncludeOlderThan90Days()
    {
        var userId = Guid.NewGuid();
        _repo.Setup(r => r.ListAsync(userId, null, null, true, 1, 20, default))
             .ReturnsAsync(new PagedResult<Notification>(
                 new List<Notification>(), 0, 1, 20));

        var result = await _handler.Handle(
            new ListNotificationsQuery(userId, UserRole.Admin,
                null, null, 1, 20, All: true), default);

        _repo.Verify(r => r.ListAsync(userId, null, null, true, 1, 20, default), Times.Once);
    }

    [Fact]
    public async Task Handle_AllTrueByNonAdmin_IgnoresAllFlag()
    {
        var userId = Guid.NewGuid();
        _repo.Setup(r => r.ListAsync(userId, null, null, false, 1, 20, default))
             .ReturnsAsync(new PagedResult<Notification>(
                 new List<Notification>(), 0, 1, 20));

        var result = await _handler.Handle(
            new ListNotificationsQuery(userId, UserRole.Agent,
                null, null, 1, 20, All: true), default);

        _repo.Verify(r => r.ListAsync(userId, null, null, false, 1, 20, default), Times.Once);
    }

    [Fact]
    public async Task Handle_IsReadFalse_PassesUnreadFilter()
    {
        var userId = Guid.NewGuid();
        _repo.Setup(r => r.ListAsync(userId, false, null, false, 1, 20, default))
             .ReturnsAsync(new PagedResult<Notification>(
                 new List<Notification>(), 0, 1, 20));

        await _handler.Handle(
            new ListNotificationsQuery(userId, UserRole.Agent,
                IsRead: false, null, 1, 20, All: false), default);

        _repo.Verify(r => r.ListAsync(userId, false, null, false, 1, 20, default), Times.Once);
    }
}
