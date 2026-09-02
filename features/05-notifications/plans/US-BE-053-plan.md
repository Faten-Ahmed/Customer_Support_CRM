# Create & Persist Notification — Implementation Plan

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

**Story:** US-BE-053  
**Goal:** Implement the `Notification` domain entity, `INotificationRepository`, and `CreateNotificationCommand` — the internal foundation that all other modules call to create persistent notifications.

**Architecture:** `Notification` entity (pure domain) holds type, recipient, entity reference, and read state. `INotificationRepository` provides persistence. `CreateNotificationCommandHandler` enforces duplicate suppression for SLA notification types (BR-NOT-006) before persisting. No HTTP endpoint — this is an internal command only.

**Tech Stack:** .NET 10, MediatR, EF Core, xUnit, Moq

---

## File Structure

| Action | Path |
|--------|------|
| Create | `src/CRM.Domain/Notifications/Notification.cs` |
| Create | `src/CRM.Domain/Notifications/NotificationType.cs` |
| Create | `src/CRM.Domain/Notifications/INotificationRepository.cs` |
| Create | `src/CRM.Application/Notifications/Commands/CreateNotificationCommand.cs` |
| Create | `src/CRM.Application/Notifications/DTOs/NotificationDto.cs` |
| Test   | `tests/CRM.Application.Tests/Notifications/CreateNotificationCommandHandlerTests.cs` |

---

## Task 1: Notification Entity + CreateNotificationCommand

- [ ] **Step 1: Write the failing tests**

```csharp
// tests/CRM.Application.Tests/Notifications/CreateNotificationCommandHandlerTests.cs
using CRM.Application.Notifications.Commands;
using CRM.Domain.Notifications;
using Moq;
using Xunit;

namespace CRM.Application.Tests.Notifications;

public class CreateNotificationCommandHandlerTests
{
    private readonly Mock<INotificationRepository> _repo = new();
    private readonly CreateNotificationCommandHandler _handler;

    public CreateNotificationCommandHandlerTests()
    {
        _handler = new CreateNotificationCommandHandler(_repo.Object);
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
```

- [ ] **Step 2: Run tests to verify they fail**

```bash
dotnet test tests/CRM.Application.Tests/ --filter "CreateNotificationCommandHandlerTests" -v n
```

Expected: FAIL — `Notification`, `NotificationType`, and `CreateNotificationCommand` do not exist yet.

- [ ] **Step 3: Create NotificationType enum**

```csharp
// src/CRM.Domain/Notifications/NotificationType.cs
namespace CRM.Domain.Notifications;

public enum NotificationType
{
    TicketAssigned,
    TicketReopened,
    NewMessage,
    NewInternalNote,
    SlaWarning,
    SlaBreached,
    SlaCriticalBreach,
    TicketEscalated,
    UnassignedTicketAlert,
    KbArticleSubmittedForReview,
    KbArticleRejected,
    KbArticlePublished,
    TicketReplyReceived,
    TicketStatusChanged,
    TicketClosed,
    SurveyAvailable
}
```

- [ ] **Step 4: Create Notification entity**

```csharp
// src/CRM.Domain/Notifications/Notification.cs
namespace CRM.Domain.Notifications;

public class Notification
{
    public Guid Id { get; private set; }
    public Guid UserId { get; private set; }
    public NotificationType Type { get; private set; }
    public string Title { get; private set; } = string.Empty;
    public string Body { get; private set; } = string.Empty;
    public string EntityType { get; private set; } = string.Empty;
    public Guid EntityId { get; private set; }
    public bool IsRead { get; private set; }
    public DateTime? ReadAt { get; private set; }
    public DateTime CreatedAt { get; private set; }

    private Notification() { }

    public static Notification Create(
        Guid userId,
        NotificationType type,
        string title,
        string body,
        string entityType,
        Guid entityId)
    {
        return new Notification
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            Type = type,
            Title = title,
            Body = body,
            EntityType = entityType,
            EntityId = entityId,
            IsRead = false,
            CreatedAt = DateTime.UtcNow
        };
    }

    public void MarkRead()
    {
        IsRead = true;
        ReadAt = DateTime.UtcNow;
    }
}
```

- [ ] **Step 5: Create INotificationRepository**

```csharp
// src/CRM.Domain/Notifications/INotificationRepository.cs
using CRM.Application.Common;

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
```

- [ ] **Step 6: Create NotificationDto**

```csharp
// src/CRM.Application/Notifications/DTOs/NotificationDto.cs
namespace CRM.Application.Notifications.DTOs;

public record NotificationDto(
    Guid Id,
    string Type,
    string Title,
    string Body,
    string EntityType,
    Guid EntityId,
    bool IsRead,
    DateTime? ReadAt,
    DateTime CreatedAt);
```

- [ ] **Step 7: Implement CreateNotificationCommand**

```csharp
// src/CRM.Application/Notifications/Commands/CreateNotificationCommand.cs
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

    public CreateNotificationCommandHandler(INotificationRepository notifications)
        => _notifications = notifications;

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

        return notification.Id;
    }
}
```

- [ ] **Step 8: Run tests to verify they pass**

```bash
dotnet test tests/CRM.Application.Tests/ --filter "CreateNotificationCommandHandlerTests" -v n
```

Expected: 5 tests PASS.

- [ ] **Step 9: Commit**

```bash
git add src/CRM.Domain/Notifications/ \
        src/CRM.Application/Notifications/Commands/CreateNotificationCommand.cs \
        src/CRM.Application/Notifications/DTOs/NotificationDto.cs \
        tests/CRM.Application.Tests/Notifications/CreateNotificationCommandHandlerTests.cs
git commit -m "feat(notifications): add Notification entity, INotificationRepository, and CreateNotificationCommand with SLA duplicate suppression"
```
