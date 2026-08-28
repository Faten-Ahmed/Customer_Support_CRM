# Mark Notifications Read — Implementation Plan

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

**Story:** US-BE-056  
**Goal:** Implement `PUT /api/notifications/{id}/read` (mark single notification read) and `PUT /api/notifications/read-all` (mark all of the caller's notifications read). Both enforce ownership — a user cannot mark another's notifications read.

**Architecture:** `MarkNotificationReadCommand(NotificationId, RequestingUserId)` → checks ownership, calls `notification.MarkRead()`, saves. `MarkAllNotificationsReadCommand(RequestingUserId)` → calls `INotificationRepository.MarkAllReadAsync()`, returns count. Both push updated `UnreadCountUpdated` via `INotificationPushService`.

**Tech Stack:** .NET 10, ASP.NET Core, MediatR, EF Core, xUnit, Moq

---

## File Structure

| Action | Path |
|--------|------|
| Create | `src/CRM.Application/Notifications/Commands/MarkNotificationReadCommand.cs` |
| Create | `src/CRM.Application/Notifications/Commands/MarkAllNotificationsReadCommand.cs` |
| Modify | `src/CRM.API/Controllers/NotificationsController.cs` |
| Test   | `tests/CRM.Application.Tests/Notifications/MarkNotificationReadCommandHandlerTests.cs` |
| Test   | `tests/CRM.API.Tests/Notifications/NotificationsControllerMarkReadTests.cs` |

---

## Task 1: MarkNotificationRead Commands

- [ ] **Step 1: Write the failing tests**

```csharp
// tests/CRM.Application.Tests/Notifications/MarkNotificationReadCommandHandlerTests.cs
using CRM.Application.Notifications.Commands;
using CRM.Domain.Notifications;
using Moq;
using Xunit;

namespace CRM.Application.Tests.Notifications;

public class MarkNotificationReadCommandHandlerTests
{
    private readonly Mock<INotificationRepository> _repo = new();
    private readonly Mock<INotificationPushService> _push = new();
    private readonly MarkNotificationReadCommandHandler _handler;
    private readonly MarkAllNotificationsReadCommandHandler _markAllHandler;

    public MarkNotificationReadCommandHandlerTests()
    {
        _handler = new MarkNotificationReadCommandHandler(_repo.Object, _push.Object);
        _markAllHandler = new MarkAllNotificationsReadCommandHandler(_repo.Object, _push.Object);
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
```

- [ ] **Step 2: Run tests to verify they fail**

```bash
dotnet test tests/CRM.Application.Tests/ --filter "MarkNotificationReadCommandHandlerTests" -v n
```

Expected: FAIL — commands do not exist yet.

- [ ] **Step 3: Create MarkNotificationReadCommand**

```csharp
// src/CRM.Application/Notifications/Commands/MarkNotificationReadCommand.cs
using CRM.Application.Notifications.DTOs;
using CRM.Domain.Notifications;
using MediatR;

namespace CRM.Application.Notifications.Commands;

public record MarkNotificationReadCommand(
    Guid NotificationId,
    Guid RequestingUserId) : IRequest<MarkNotificationReadResult>;

public record MarkNotificationReadResult(Guid Id, bool IsRead, DateTime? ReadAt);

public class MarkNotificationReadCommandHandler
    : IRequestHandler<MarkNotificationReadCommand, MarkNotificationReadResult>
{
    private readonly INotificationRepository _notifications;
    private readonly INotificationPushService _push;

    public MarkNotificationReadCommandHandler(
        INotificationRepository notifications,
        INotificationPushService push)
    {
        _notifications = notifications;
        _push = push;
    }

    public async Task<MarkNotificationReadResult> Handle(
        MarkNotificationReadCommand cmd, CancellationToken ct)
    {
        var notification = await _notifications.FindByIdAsync(cmd.NotificationId, ct)
            ?? throw new KeyNotFoundException(
                $"Notification {cmd.NotificationId} not found.");

        if (notification.UserId != cmd.RequestingUserId)
            throw new UnauthorizedAccessException(
                "You can only mark your own notifications as read.");

        notification.MarkRead();
        await _notifications.SaveChangesAsync(ct);

        var newCount = await _notifications.GetUnreadCountAsync(cmd.RequestingUserId, ct);
        await _push.PushUnreadCountAsync(cmd.RequestingUserId, newCount, ct);

        return new MarkNotificationReadResult(
            notification.Id, notification.IsRead, notification.ReadAt);
    }
}
```

- [ ] **Step 4: Create MarkAllNotificationsReadCommand**

```csharp
// src/CRM.Application/Notifications/Commands/MarkAllNotificationsReadCommand.cs
using CRM.Domain.Notifications;
using MediatR;

namespace CRM.Application.Notifications.Commands;

public record MarkAllNotificationsReadCommand(Guid RequestingUserId) : IRequest<int>;

public class MarkAllNotificationsReadCommandHandler
    : IRequestHandler<MarkAllNotificationsReadCommand, int>
{
    private readonly INotificationRepository _notifications;
    private readonly INotificationPushService _push;

    public MarkAllNotificationsReadCommandHandler(
        INotificationRepository notifications,
        INotificationPushService push)
    {
        _notifications = notifications;
        _push = push;
    }

    public async Task<int> Handle(MarkAllNotificationsReadCommand cmd, CancellationToken ct)
    {
        int count = await _notifications.MarkAllReadAsync(cmd.RequestingUserId, ct);

        var newCount = await _notifications.GetUnreadCountAsync(cmd.RequestingUserId, ct);
        await _push.PushUnreadCountAsync(cmd.RequestingUserId, newCount, ct);

        return count;
    }
}
```

- [ ] **Step 5: Run tests to verify they pass**

```bash
dotnet test tests/CRM.Application.Tests/ --filter "MarkNotificationReadCommandHandlerTests" -v n
```

Expected: 4 tests PASS.

- [ ] **Step 6: Add endpoints to NotificationsController**

```csharp
// Add to src/CRM.API/Controllers/NotificationsController.cs:

[HttpPut("{id:guid}/read")]
public async Task<IActionResult> MarkRead(Guid id, CancellationToken ct)
{
    try
    {
        var result = await _mediator.Send(
            new MarkNotificationReadCommand(id, CurrentUserId), ct);
        return Ok(new { data = result });
    }
    catch (KeyNotFoundException ex) { return NotFound(new { error = ex.Message }); }
    catch (UnauthorizedAccessException ex) { return StatusCode(403, new { error = ex.Message }); }
}

[HttpPut("read-all")]
public async Task<IActionResult> MarkAllRead(CancellationToken ct)
{
    int markedRead = await _mediator.Send(
        new MarkAllNotificationsReadCommand(CurrentUserId), ct);
    return Ok(new { data = new { markedRead } });
}
```

- [ ] **Step 7: Write controller tests**

```csharp
// tests/CRM.API.Tests/Notifications/NotificationsControllerMarkReadTests.cs
using System.Net;
using CRM.Application.Notifications.Commands;
using MediatR;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using Xunit;

namespace CRM.API.Tests.Notifications;

public class NotificationsControllerMarkReadTests
{
    private readonly Mock<IMediator> _mediator = new();

    private HttpClient BuildClient()
    {
        var factory = new WebApplicationFactory<Program>()
            .WithWebHostBuilder(b => b.ConfigureServices(services =>
            {
                services.RemoveAll<IMediator>();
                services.AddSingleton(_mediator.Object);
            }));
        var client = factory.CreateClient();
        client.DefaultRequestHeaders.Authorization =
            new System.Net.Http.Headers.AuthenticationHeaderValue(
                "Bearer", TestJwtHelper.CreateTestToken(role: "Agent"));
        return client;
    }

    [Fact]
    public async Task MarkRead_Returns200()
    {
        _mediator.Setup(m => m.Send(It.IsAny<MarkNotificationReadCommand>(), default))
                 .ReturnsAsync(new MarkNotificationReadResult(
                     Guid.NewGuid(), true, DateTime.UtcNow));

        var response = await BuildClient()
            .PutAsync($"/api/notifications/{Guid.NewGuid()}/read", null);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task MarkRead_WrongUser_Returns403()
    {
        _mediator.Setup(m => m.Send(It.IsAny<MarkNotificationReadCommand>(), default))
                 .ThrowsAsync(new UnauthorizedAccessException("Not your notification."));

        var response = await BuildClient()
            .PutAsync($"/api/notifications/{Guid.NewGuid()}/read", null);

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task MarkAllRead_Returns200WithCount()
    {
        _mediator.Setup(m => m.Send(It.IsAny<MarkAllNotificationsReadCommand>(), default))
                 .ReturnsAsync(5);

        var response = await BuildClient()
            .PutAsync("/api/notifications/read-all", null);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }
}
```

- [ ] **Step 8: Run controller tests to verify they pass**

```bash
dotnet test tests/CRM.API.Tests/ --filter "NotificationsControllerMarkReadTests" -v n
```

Expected: 3 tests PASS.

- [ ] **Step 9: Commit**

```bash
git add src/CRM.Application/Notifications/Commands/MarkNotificationReadCommand.cs \
        src/CRM.Application/Notifications/Commands/MarkAllNotificationsReadCommand.cs \
        src/CRM.API/Controllers/NotificationsController.cs \
        tests/CRM.Application.Tests/Notifications/MarkNotificationReadCommandHandlerTests.cs \
        tests/CRM.API.Tests/Notifications/NotificationsControllerMarkReadTests.cs
git commit -m "feat(notifications): add PUT /api/notifications/{id}/read and PUT /api/notifications/read-all with ownership check"
```
