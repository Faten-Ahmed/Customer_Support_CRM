using CRM.Application.Notifications.Queries;
using CRM.Domain.Notifications;
using Microsoft.Extensions.Caching.Distributed;
using Moq;
using System.Text;
using Xunit;

namespace CRM.Application.Tests.Notifications;

public class GetUnreadCountQueryHandlerTests
{
    private readonly Mock<INotificationRepository> _repo = new();
    private readonly Mock<IDistributedCache> _cache = new();
    private readonly GetUnreadCountQueryHandler _handler;

    public GetUnreadCountQueryHandlerTests()
    {
        _handler = new GetUnreadCountQueryHandler(_repo.Object, _cache.Object);
    }

    [Fact]
    public async Task Handle_CacheHit_ReturnsCachedCount()
    {
        var userId = Guid.NewGuid();
        var key = $"notification:unread:{userId}";
        var cached = Encoding.UTF8.GetBytes("7");

        _cache.Setup(c => c.GetAsync(key, default)).ReturnsAsync(cached);

        var result = await _handler.Handle(new GetUnreadCountQuery(userId), default);

        Assert.Equal(7, result);
        _repo.Verify(r => r.GetUnreadCountAsync(It.IsAny<Guid>(), default), Times.Never);
    }

    [Fact]
    public async Task Handle_CacheMiss_QueriesRepoAndCaches()
    {
        var userId = Guid.NewGuid();
        var key = $"notification:unread:{userId}";

        _cache.Setup(c => c.GetAsync(key, default)).ReturnsAsync((byte[]?)null);
        _repo.Setup(r => r.GetUnreadCountAsync(userId, default)).ReturnsAsync(3);

        var result = await _handler.Handle(new GetUnreadCountQuery(userId), default);

        Assert.Equal(3, result);
        _cache.Verify(c => c.SetAsync(
            key,
            It.Is<byte[]>(b => Encoding.UTF8.GetString(b) == "3"),
            It.Is<DistributedCacheEntryOptions>(o =>
                o.AbsoluteExpirationRelativeToNow == TimeSpan.FromSeconds(60)),
            default), Times.Once);
    }
}
