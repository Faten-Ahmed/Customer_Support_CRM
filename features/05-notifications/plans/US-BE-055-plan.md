# List Notifications — Implementation Plan

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

**Story:** US-BE-055  
**Goal:** Implement `GET /api/notifications` — returns the authenticated user's notifications, newest first, filtered by `isRead` and `type`, paginated. By default returns only the last 90 days; `?all=true` (Admin only) includes all.

**Architecture:** `ListNotificationsQuery(RequestingUserId, RequestingUserRole, IsRead?, Type?, Page, PageSize, All)` → delegates to `INotificationRepository.ListAsync()`. Portal returns only the caller's own notifications. `NotificationsController` maps query params to the query.

**Tech Stack:** .NET 10, ASP.NET Core, MediatR, EF Core, xUnit, Moq

---

## File Structure

| Action | Path |
|--------|------|
| Create | `src/CRM.Application/Notifications/Queries/ListNotificationsQuery.cs` |
| Create | `src/CRM.API/Controllers/NotificationsController.cs` |
| Test   | `tests/CRM.Application.Tests/Notifications/ListNotificationsQueryHandlerTests.cs` |
| Test   | `tests/CRM.API.Tests/Notifications/NotificationsControllerListTests.cs` |

---

## Task 1: ListNotifications Query + Controller

- [ ] **Step 1: Write the failing tests**

```csharp
// tests/CRM.Application.Tests/Notifications/ListNotificationsQueryHandlerTests.cs
using CRM.Application.Common;
using CRM.Application.Notifications.Queries;
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

        // Agent passes all=true but it should be treated as false
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
```

- [ ] **Step 2: Run tests to verify they fail**

```bash
dotnet test tests/CRM.Application.Tests/ --filter "ListNotificationsQueryHandlerTests" -v n
```

Expected: FAIL — `ListNotificationsQuery` does not exist yet.

- [ ] **Step 3: Implement ListNotificationsQuery**

```csharp
// src/CRM.Application/Notifications/Queries/ListNotificationsQuery.cs
using CRM.Application.Common;
using CRM.Application.Notifications.DTOs;
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
```

- [ ] **Step 4: Run tests to verify they pass**

```bash
dotnet test tests/CRM.Application.Tests/ --filter "ListNotificationsQueryHandlerTests" -v n
```

Expected: 4 tests PASS.

- [ ] **Step 5: Create NotificationsController**

```csharp
// src/CRM.API/Controllers/NotificationsController.cs
using CRM.Application.Notifications.Queries;
using CRM.Domain.Notifications;
using CRM.Domain.Users;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace CRM.API.Controllers;

[ApiController]
[Route("api/notifications")]
[Authorize]
public class NotificationsController : ControllerBase
{
    private readonly IMediator _mediator;

    private Guid CurrentUserId =>
        Guid.Parse(User.FindFirst(ClaimTypes.NameIdentifier)!.Value);

    private UserRole CurrentUserRole
    {
        get
        {
            var roleClaim = User.FindFirst(ClaimTypes.Role)?.Value ?? "Agent";
            Enum.TryParse<UserRole>(roleClaim, out var role);
            return role;
        }
    }

    public NotificationsController(IMediator mediator) => _mediator = mediator;

    [HttpGet]
    public async Task<IActionResult> List(
        [FromQuery] bool? isRead,
        [FromQuery] string? type,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        [FromQuery] bool all = false,
        CancellationToken ct = default)
    {
        NotificationType? parsedType = Enum.TryParse<NotificationType>(type, out var t) ? t : null;
        var capped = Math.Min(pageSize, 50);

        var result = await _mediator.Send(
            new ListNotificationsQuery(
                CurrentUserId, CurrentUserRole,
                isRead, parsedType, page, capped, all), ct);

        return Ok(result);
    }
}
```

- [ ] **Step 6: Write controller test**

```csharp
// tests/CRM.API.Tests/Notifications/NotificationsControllerListTests.cs
using System.Net;
using System.Net.Http.Json;
using CRM.Application.Common;
using CRM.Application.Notifications.DTOs;
using CRM.Application.Notifications.Queries;
using MediatR;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using Xunit;

namespace CRM.API.Tests.Notifications;

public class NotificationsControllerListTests
{
    private readonly Mock<IMediator> _mediator = new();

    private HttpClient BuildClient(string role = "Agent")
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
                "Bearer", TestJwtHelper.CreateTestToken(role: role));
        return client;
    }

    [Fact]
    public async Task List_Returns200WithPagedResult()
    {
        _mediator.Setup(m => m.Send(It.IsAny<ListNotificationsQuery>(), default))
                 .ReturnsAsync(new PagedResult<NotificationDto>(
                     new List<NotificationDto>(), 0, 1, 20));

        var response = await BuildClient().GetAsync("/api/notifications");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task List_CapsPageSizeAt50()
    {
        _mediator.Setup(m => m.Send(
            It.Is<ListNotificationsQuery>(q => q.PageSize == 50), default))
                 .ReturnsAsync(new PagedResult<NotificationDto>(
                     new List<NotificationDto>(), 0, 1, 50));

        var response = await BuildClient().GetAsync("/api/notifications?pageSize=200");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        _mediator.Verify(m => m.Send(
            It.Is<ListNotificationsQuery>(q => q.PageSize == 50), default), Times.Once);
    }
}
```

- [ ] **Step 7: Run controller tests to verify they pass**

```bash
dotnet test tests/CRM.API.Tests/ --filter "NotificationsControllerListTests" -v n
```

Expected: 2 tests PASS.

- [ ] **Step 8: Commit**

```bash
git add src/CRM.Application/Notifications/Queries/ListNotificationsQuery.cs \
        src/CRM.API/Controllers/NotificationsController.cs \
        tests/CRM.Application.Tests/Notifications/ListNotificationsQueryHandlerTests.cs \
        tests/CRM.API.Tests/Notifications/NotificationsControllerListTests.cs
git commit -m "feat(notifications): add GET /api/notifications with pagination and 90-day default filter"
```
