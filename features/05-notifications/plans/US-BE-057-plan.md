# Unread Notification Count — Implementation Plan

## Internationalization (i18n) Requirements

- ✅ All UI text must use `$localize` or Angular i18n
- ✅ Arabic text must support RTL layout (Angular Material handles this with `dir="rtl"`)
- ✅ English text must support LTR layout
- ✅ No hardcoded text in templates or components
- ✅ All labels, buttons, messages, and notifications must be translatable

## Angular Material Requirements

- ✅ All UI components must use Angular Material (MatButton, MatCard, MatFormField, MatInput, MatTable, etc.)
- ✅ NO Tailwind CSS
- ✅ NO custom CSS frameworks
- ✅ Use Angular Material theming for branding (colors, typography)

---


> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Story:** US-BE-057  
**Goal:** Implement `GET /api/notifications/unread-count` — returns the caller's unread notification count served from a Redis distributed cache (TTL 60s). Cache is invalidated (deleted) on new notification creation and on `MarkAllNotificationsRead`.

**Architecture:** `GetUnreadCountQuery(RequestingUserId)` → checks `IDistributedCache` by key `notification:unread:{userId}`. On cache miss, queries `INotificationRepository.GetUnreadCountAsync`, stores result for 60s. Cache invalidation is added to `CreateNotificationCommandHandler` and `MarkAllNotificationsReadCommandHandler`.

**Tech Stack:** .NET 10, ASP.NET Core, MediatR, `Microsoft.Extensions.Caching.StackExchangeRedis`, xUnit, Moq

---

## File Structure

| Action | Path |
|--------|------|
| Create | `src/CRM.Application/Notifications/Queries/GetUnreadCountQuery.cs` |
| Modify | `src/CRM.Application/Notifications/Commands/CreateNotificationCommand.cs` |
| Modify | `src/CRM.Application/Notifications/Commands/MarkAllNotificationsReadCommand.cs` |
| Modify | `src/CRM.API/Controllers/NotificationsController.cs` |
| Modify | `src/CRM.API/Program.cs` |
| Test   | `tests/CRM.Application.Tests/Notifications/GetUnreadCountQueryHandlerTests.cs` |

---

## Task 1: Unread Count Query with Cache

- [ ] **Step 1: Write the failing tests**

```csharp
// tests/CRM.Application.Tests/Notifications/GetUnreadCountQueryHandlerTests.cs
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
```

- [ ] **Step 2: Run tests to verify they fail**

```bash
dotnet test tests/CRM.Application.Tests/ --filter "GetUnreadCountQueryHandlerTests" -v n
```

Expected: FAIL — `GetUnreadCountQuery` does not exist yet.

- [ ] **Step 3: Implement GetUnreadCountQuery**

```csharp
// src/CRM.Application/Notifications/Queries/GetUnreadCountQuery.cs
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
```

- [ ] **Step 4: Run tests to verify they pass**

```bash
dotnet test tests/CRM.Application.Tests/ --filter "GetUnreadCountQueryHandlerTests" -v n
```

Expected: 2 tests PASS.

- [ ] **Step 5: Add cache invalidation to CreateNotificationCommandHandler**

In `src/CRM.Application/Notifications/Commands/CreateNotificationCommand.cs`, inject `IDistributedCache` and call `GetUnreadCountQueryHandler.InvalidateAsync` after `SaveChangesAsync`:

```csharp
// Updated constructor and Handle method in CreateNotificationCommandHandler:
private readonly INotificationRepository _notifications;
private readonly INotificationPushService _push;
private readonly IDistributedCache _cache;

public CreateNotificationCommandHandler(
    INotificationRepository notifications,
    INotificationPushService push,
    IDistributedCache cache)
{
    _notifications = notifications;
    _push = push;
    _cache = cache;
}

public async Task<Guid> Handle(CreateNotificationCommand cmd, CancellationToken ct)
{
    if (_slaTypes.Contains(cmd.Type))
    {
        bool exists = await _notifications.ExistsAsync(
            cmd.UserId, cmd.Type, cmd.EntityId, ct);
        if (exists) return Guid.Empty;
    }

    var notification = Notification.Create(
        cmd.UserId, cmd.Type, cmd.Title, cmd.Body, cmd.EntityType, cmd.EntityId);

    await _notifications.AddAsync(notification, ct);
    await _notifications.SaveChangesAsync(ct);

    await GetUnreadCountQueryHandler.InvalidateAsync(_cache, cmd.UserId, ct);

    var count = await _notifications.GetUnreadCountAsync(cmd.UserId, ct);

    var dto = new NotificationDto(
        notification.Id, notification.Type.ToString(),
        notification.Title, notification.Body,
        notification.EntityType, notification.EntityId,
        notification.IsRead, notification.ReadAt, notification.CreatedAt);

    await _push.PushNotificationAsync(cmd.UserId, dto, ct);
    await _push.PushUnreadCountAsync(cmd.UserId, count, ct);

    return notification.Id;
}
```

- [ ] **Step 6: Add cache invalidation to MarkAllNotificationsReadCommandHandler**

In `src/CRM.Application/Notifications/Commands/MarkAllNotificationsReadCommand.cs`, inject `IDistributedCache` and invalidate after `MarkAllReadAsync`:

```csharp
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

    await GetUnreadCountQueryHandler.InvalidateAsync(_cache, cmd.RequestingUserId, ct);

    var newCount = await _notifications.GetUnreadCountAsync(cmd.RequestingUserId, ct);
    await _push.PushUnreadCountAsync(cmd.RequestingUserId, newCount, ct);

    return count;
}
```

- [ ] **Step 7: Add unread-count endpoint to NotificationsController**

```csharp
// Add to src/CRM.API/Controllers/NotificationsController.cs:

[HttpGet("unread-count")]
public async Task<IActionResult> UnreadCount(CancellationToken ct)
{
    var count = await _mediator.Send(new GetUnreadCountQuery(CurrentUserId), ct);
    return Ok(new { data = new { count } });
}
```

- [ ] **Step 8: Register Redis cache in Program.cs**

```csharp
// Add to src/CRM.API/Program.cs services section:
builder.Services.AddStackExchangeRedisCache(options =>
{
    options.Configuration = builder.Configuration.GetConnectionString("Redis");
    options.InstanceName = "CRM:";
});
```

Add to `appsettings.json`:
```json
"ConnectionStrings": {
  "Redis": "localhost:6379"
}
```

- [ ] **Step 9: Run all notification tests to verify no regressions**

```bash
dotnet test tests/CRM.Application.Tests/ --filter "Notifications" -v n
```

Expected: All passing.

- [ ] **Step 10: Commit**

```bash
git add src/CRM.Application/Notifications/Queries/GetUnreadCountQuery.cs \
        src/CRM.Application/Notifications/Commands/CreateNotificationCommand.cs \
        src/CRM.Application/Notifications/Commands/MarkAllNotificationsReadCommand.cs \
        src/CRM.API/Controllers/NotificationsController.cs \
        src/CRM.API/Program.cs \
        tests/CRM.Application.Tests/Notifications/GetUnreadCountQueryHandlerTests.cs
git commit -m "feat(notifications): add GET /api/notifications/unread-count with Redis cache (TTL 60s) and cache invalidation"
```
