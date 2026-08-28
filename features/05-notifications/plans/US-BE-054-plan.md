# Push Notification via SignalR — Implementation Plan

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

**Story:** US-BE-054  
**Goal:** Implement the `NotificationHub` (SignalR) and `INotificationPushService` — a thin wrapper so the application layer can push real-time notifications without depending directly on SignalR. `CreateNotificationCommandHandler` calls `INotificationPushService` after each successful persist.

**Architecture:** `NotificationHub : Hub` authenticates via JWT Bearer (query-param or header), adds the connection to group `user-{userId}` on connect. `NotificationPushService` wraps `IHubContext<NotificationHub>` and sends `ReceiveNotification` + `UnreadCountUpdated` events. `CreateNotificationCommandHandler` is extended to inject and call `INotificationPushService`.

**Tech Stack:** .NET 10, ASP.NET Core SignalR, MediatR, xUnit, Moq

---

## File Structure

| Action | Path |
|--------|------|
| Create | `src/CRM.API/Hubs/NotificationHub.cs` |
| Create | `src/CRM.Application/Notifications/INotificationPushService.cs` |
| Create | `src/CRM.Infrastructure/Notifications/NotificationPushService.cs` |
| Modify | `src/CRM.Application/Notifications/Commands/CreateNotificationCommand.cs` |
| Modify | `src/CRM.API/Program.cs` |
| Test   | `tests/CRM.Application.Tests/Notifications/CreateNotificationCommandHandlerPushTests.cs` |

---

## Task 1: NotificationHub + Push Service

- [ ] **Step 1: Write the failing tests**

```csharp
// tests/CRM.Application.Tests/Notifications/CreateNotificationCommandHandlerPushTests.cs
using CRM.Application.Notifications.Commands;
using CRM.Application.Notifications.DTOs;
using CRM.Domain.Notifications;
using Moq;
using Xunit;

namespace CRM.Application.Tests.Notifications;

public class CreateNotificationCommandHandlerPushTests
{
    private readonly Mock<INotificationRepository> _repo = new();
    private readonly Mock<INotificationPushService> _push = new();
    private readonly CreateNotificationCommandHandler _handler;

    public CreateNotificationCommandHandlerPushTests()
    {
        _handler = new CreateNotificationCommandHandler(_repo.Object, _push.Object);
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
```

- [ ] **Step 2: Run tests to verify they fail**

```bash
dotnet test tests/CRM.Application.Tests/ --filter "CreateNotificationCommandHandlerPushTests" -v n
```

Expected: FAIL — `INotificationPushService` does not exist yet.

- [ ] **Step 3: Create INotificationPushService**

```csharp
// src/CRM.Application/Notifications/INotificationPushService.cs
using CRM.Application.Notifications.DTOs;

namespace CRM.Application.Notifications;

public interface INotificationPushService
{
    Task PushNotificationAsync(Guid userId, NotificationDto notification,
        CancellationToken ct = default);
    Task PushUnreadCountAsync(Guid userId, int count,
        CancellationToken ct = default);
}
```

- [ ] **Step 4: Update CreateNotificationCommandHandler to inject and call push service**

```csharp
// src/CRM.Application/Notifications/Commands/CreateNotificationCommand.cs
using CRM.Application.Notifications.DTOs;
using CRM.Domain.Notifications;
using MediatR;

namespace CRM.Application.Notifications.Commands;

public record CreateNotificationCommand(
    Guid UserId,
    NotificationType Type,
    string Title,
    string Body,
    string EntityType,
    Guid EntityId) : IRequest<Guid>;

public class CreateNotificationCommandHandler : IRequestHandler<CreateNotificationCommand, Guid>
{
    private static readonly HashSet<NotificationType> _slaTypes =
    [
        NotificationType.SlaWarning,
        NotificationType.SlaBreached,
        NotificationType.SlaCriticalBreach
    ];

    private readonly INotificationRepository _notifications;
    private readonly INotificationPushService _push;

    public CreateNotificationCommandHandler(
        INotificationRepository notifications,
        INotificationPushService push)
    {
        _notifications = notifications;
        _push = push;
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

        var dto = new NotificationDto(
            notification.Id, notification.Type.ToString(),
            notification.Title, notification.Body,
            notification.EntityType, notification.EntityId,
            notification.IsRead, notification.ReadAt, notification.CreatedAt);

        var count = await _notifications.GetUnreadCountAsync(cmd.UserId, ct);

        await _push.PushNotificationAsync(cmd.UserId, dto, ct);
        await _push.PushUnreadCountAsync(cmd.UserId, count, ct);

        return notification.Id;
    }
}
```

- [ ] **Step 5: Run tests to verify they pass**

```bash
dotnet test tests/CRM.Application.Tests/ --filter "CreateNotificationCommandHandlerPushTests" -v n
```

Expected: 2 tests PASS.

- [ ] **Step 6: Create NotificationHub**

```csharp
// src/CRM.API/Hubs/NotificationHub.cs
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using System.Security.Claims;

namespace CRM.API.Hubs;

[Authorize]
public class NotificationHub : Hub
{
    public override async Task OnConnectedAsync()
    {
        var userId = Context.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (!string.IsNullOrEmpty(userId))
            await Groups.AddToGroupAsync(Context.ConnectionId, $"user-{userId}");
        await base.OnConnectedAsync();
    }

    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        var userId = Context.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (!string.IsNullOrEmpty(userId))
            await Groups.RemoveFromGroupAsync(Context.ConnectionId, $"user-{userId}");
        await base.OnDisconnectedAsync(exception);
    }
}
```

- [ ] **Step 7: Implement NotificationPushService**

```csharp
// src/CRM.Infrastructure/Notifications/NotificationPushService.cs
using CRM.API.Hubs;
using CRM.Application.Notifications;
using CRM.Application.Notifications.DTOs;
using Microsoft.AspNetCore.SignalR;

namespace CRM.Infrastructure.Notifications;

public class NotificationPushService : INotificationPushService
{
    private readonly IHubContext<NotificationHub> _hub;

    public NotificationPushService(IHubContext<NotificationHub> hub) => _hub = hub;

    public Task PushNotificationAsync(Guid userId, NotificationDto notification,
        CancellationToken ct = default)
        => _hub.Clients.Group($"user-{userId}")
               .SendAsync("ReceiveNotification", notification, ct);

    public Task PushUnreadCountAsync(Guid userId, int count,
        CancellationToken ct = default)
        => _hub.Clients.Group($"user-{userId}")
               .SendAsync("UnreadCountUpdated", new { count }, ct);
}
```

- [ ] **Step 8: Register hub and service in Program.cs**

```csharp
// Add to src/CRM.API/Program.cs in the services section:
builder.Services.AddSignalR();
builder.Services.AddScoped<INotificationPushService, NotificationPushService>();

// Add to the middleware/routing section (after app.UseAuthorization()):
app.MapHub<NotificationHub>("/hubs/notifications");
```

SignalR JWT configuration — add to the existing JWT setup in Program.cs:

```csharp
builder.Services.AddAuthentication(...)
    .AddJwtBearer(options =>
    {
        // existing JWT options ...

        // Allow token from query string for SignalR WebSocket connections
        options.Events = new JwtBearerEvents
        {
            OnMessageReceived = context =>
            {
                var accessToken = context.Request.Query["access_token"];
                var path = context.HttpContext.Request.Path;
                if (!string.IsNullOrEmpty(accessToken) &&
                    path.StartsWithSegments("/hubs/notifications"))
                {
                    context.Token = accessToken;
                }
                return Task.CompletedTask;
            }
        };
    });
```

- [ ] **Step 9: Commit**

```bash
git add src/CRM.API/Hubs/NotificationHub.cs \
        src/CRM.Application/Notifications/INotificationPushService.cs \
        src/CRM.Infrastructure/Notifications/NotificationPushService.cs \
        src/CRM.Application/Notifications/Commands/CreateNotificationCommand.cs \
        src/CRM.API/Program.cs \
        tests/CRM.Application.Tests/Notifications/CreateNotificationCommandHandlerPushTests.cs
git commit -m "feat(notifications): add SignalR NotificationHub and real-time push after notification persist"
```
