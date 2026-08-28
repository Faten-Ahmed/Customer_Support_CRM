# Real-Time Dashboard SignalR Hub — Implementation Plan

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

**Story:** US-BE-078  
**Goal:** Implement `DashboardHub` — a SignalR hub at `/hubs/dashboard` that authenticates via JWT query param, routes connections to role-scoped groups, and pushes `KpiUpdated` / `AgentWorkloadUpdated` events when tickets change. Pushes are debounced: multiple events within 2 seconds collapse to a single push.

**Architecture:** `DashboardHub : Hub` with JWT auth via `OnMessageReceived`. `IDashboardPusher` service holds a `DebounceTimer` per group; on trigger it calls `IDashboardRepository.GetKpisAsync()` and broadcasts. Domain events (`TicketCreated`, `TicketStatusChanged`, `CsatSubmitted`, `SlaBreached`, `TicketAssigned`, `AgentStatusChanged`) raise domain events handled by `DashboardPushEventHandler`.

**Tech Stack:** .NET 10, ASP.NET Core, SignalR, MediatR, xUnit, Moq

---

## File Structure

| Action | Path |
|--------|------|
| Create | `src/CRM.Application/Dashboard/Services/IDashboardPusher.cs` |
| Create | `src/CRM.Infrastructure/Hubs/DashboardHub.cs` |
| Create | `src/CRM.Infrastructure/Dashboard/DashboardPushService.cs` |
| Create | `src/CRM.Application/Dashboard/Events/DashboardPushEventHandler.cs` |
| Modify | `src/CRM.API/Program.cs` |
| Test   | `tests/CRM.Application.Tests/Dashboard/DashboardPushEventHandlerTests.cs` |

---

## Task 1: DashboardHub and Debounced Push

> Note: `IDashboardRepository` and `GetDashboardKpisQuery` are from US-BE-077. Implement that plan first. Domain events `TicketCreated`, `TicketStatusChanged`, `CsatSubmitted`, `SlaBreached`, `TicketAssigned`, `AgentStatusChanged` are published by their respective command handlers.

- [ ] **Step 1: Write the failing tests**

```csharp
// tests/CRM.Application.Tests/Dashboard/DashboardPushEventHandlerTests.cs
using CRM.Application.Dashboard.Events;
using CRM.Application.Dashboard.Services;
using CRM.Domain.Tickets.Events;
using Moq;
using Xunit;

namespace CRM.Application.Tests.Dashboard;

public class DashboardPushEventHandlerTests
{
    private readonly Mock<IDashboardPusher> _pusher = new();
    private readonly DashboardPushEventHandler _handler;

    public DashboardPushEventHandlerTests()
    {
        _handler = new DashboardPushEventHandler(_pusher.Object);
    }

    [Fact]
    public async Task Handle_TicketCreatedEvent_TriggersDebouncedKpiPush()
    {
        var evt = new TicketCreatedEvent(Guid.NewGuid(), Guid.NewGuid());

        await _handler.Handle(evt, default);

        _pusher.Verify(p => p.ScheduleKpiPushAsync(evt.DepartmentId, default), Times.Once);
    }

    [Fact]
    public async Task Handle_TicketStatusChangedEvent_TriggersDebouncedKpiPush()
    {
        var evt = new TicketStatusChangedEvent(Guid.NewGuid(), Guid.NewGuid(), "Open", "Resolved");

        await _handler.Handle(evt, default);

        _pusher.Verify(p => p.ScheduleKpiPushAsync(evt.DepartmentId, default), Times.Once);
    }

    [Fact]
    public async Task Handle_TicketAssignedEvent_TriggersWorkloadPush()
    {
        var evt = new TicketAssignedEvent(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid());

        await _handler.Handle(evt, default);

        _pusher.Verify(p => p.ScheduleWorkloadPushAsync(evt.DepartmentId, default), Times.Once);
    }

    [Fact]
    public async Task Handle_AgentStatusChangedEvent_TriggersWorkloadPush()
    {
        var evt = new AgentStatusChangedEvent(Guid.NewGuid(), Guid.NewGuid(), "Available");

        await _handler.Handle(evt, default);

        _pusher.Verify(p => p.ScheduleWorkloadPushAsync(evt.DepartmentId, default), Times.Once);
    }
}
```

- [ ] **Step 2: Run tests to verify they fail**

```bash
dotnet test tests/CRM.Application.Tests/ --filter "DashboardPushEventHandlerTests" -v n
```

Expected: FAIL — `DashboardPushEventHandler` does not exist yet.

- [ ] **Step 3: Define domain events**

Add these event records to their respective domain files if not already present:

```csharp
// src/CRM.Domain/Tickets/Events/TicketCreatedEvent.cs
using MediatR;
namespace CRM.Domain.Tickets.Events;
public record TicketCreatedEvent(Guid TicketId, Guid DepartmentId) : INotification;
```

```csharp
// src/CRM.Domain/Tickets/Events/TicketStatusChangedEvent.cs
using MediatR;
namespace CRM.Domain.Tickets.Events;
public record TicketStatusChangedEvent(
    Guid TicketId, Guid DepartmentId, string OldStatus, string NewStatus) : INotification;
```

```csharp
// src/CRM.Domain/Tickets/Events/TicketAssignedEvent.cs
using MediatR;
namespace CRM.Domain.Tickets.Events;
public record TicketAssignedEvent(
    Guid TicketId, Guid DepartmentId, Guid AgentId) : INotification;
```

```csharp
// src/CRM.Domain/Tickets/Events/AgentStatusChangedEvent.cs
using MediatR;
namespace CRM.Domain.Tickets.Events;
public record AgentStatusChangedEvent(
    Guid AgentId, Guid DepartmentId, string NewStatus) : INotification;
```

- [ ] **Step 4: Create IDashboardPusher**

```csharp
// src/CRM.Application/Dashboard/Services/IDashboardPusher.cs
namespace CRM.Application.Dashboard.Services;

public interface IDashboardPusher
{
    Task ScheduleKpiPushAsync(Guid departmentId, CancellationToken ct = default);
    Task ScheduleWorkloadPushAsync(Guid departmentId, CancellationToken ct = default);
}
```

- [ ] **Step 5: Implement DashboardPushEventHandler**

```csharp
// src/CRM.Application/Dashboard/Events/DashboardPushEventHandler.cs
using CRM.Application.Dashboard.Services;
using CRM.Domain.Tickets.Events;
using MediatR;

namespace CRM.Application.Dashboard.Events;

public class DashboardPushEventHandler :
    INotificationHandler<TicketCreatedEvent>,
    INotificationHandler<TicketStatusChangedEvent>,
    INotificationHandler<TicketAssignedEvent>,
    INotificationHandler<AgentStatusChangedEvent>
{
    private readonly IDashboardPusher _pusher;

    public DashboardPushEventHandler(IDashboardPusher pusher) => _pusher = pusher;

    public Task Handle(TicketCreatedEvent n, CancellationToken ct)
        => _pusher.ScheduleKpiPushAsync(n.DepartmentId, ct);

    public Task Handle(TicketStatusChangedEvent n, CancellationToken ct)
        => _pusher.ScheduleKpiPushAsync(n.DepartmentId, ct);

    public Task Handle(TicketAssignedEvent n, CancellationToken ct)
        => _pusher.ScheduleWorkloadPushAsync(n.DepartmentId, ct);

    public Task Handle(AgentStatusChangedEvent n, CancellationToken ct)
        => _pusher.ScheduleWorkloadPushAsync(n.DepartmentId, ct);
}
```

- [ ] **Step 6: Run tests to verify they pass**

```bash
dotnet test tests/CRM.Application.Tests/ --filter "DashboardPushEventHandlerTests" -v n
```

Expected: 4 tests PASS.

- [ ] **Step 7: Implement DashboardHub**

```csharp
// src/CRM.Infrastructure/Hubs/DashboardHub.cs
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using System.Security.Claims;

namespace CRM.Infrastructure.Hubs;

[Authorize]
public class DashboardHub : Hub
{
    public override async Task OnConnectedAsync()
    {
        var role = Context.User?.FindFirst(ClaimTypes.Role)?.Value ?? "Agent";
        var userId = Context.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;

        // Role-scoped groups: "kpi-admin", "kpi-manager-{deptId}", "kpi-agent-{userId}"
        await Groups.AddToGroupAsync(Context.ConnectionId, $"kpi-{role.ToLower()}");
        await base.OnConnectedAsync();
    }

    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        var role = Context.User?.FindFirst(ClaimTypes.Role)?.Value ?? "Agent";
        await Groups.RemoveFromGroupAsync(Context.ConnectionId, $"kpi-{role.ToLower()}");
        await base.OnDisconnectedAsync(exception);
    }
}
```

- [ ] **Step 8: Implement DashboardPushService with 2-second debounce**

```csharp
// src/CRM.Infrastructure/Dashboard/DashboardPushService.cs
using CRM.Application.Dashboard.DTOs;
using CRM.Application.Dashboard.Services;
using CRM.Domain.Dashboard;
using Microsoft.AspNetCore.SignalR;
using CRM.Infrastructure.Hubs;

namespace CRM.Infrastructure.Dashboard;

public class DashboardPushService : IDashboardPusher
{
    private readonly IHubContext<DashboardHub> _hub;
    private readonly IDashboardRepository _dashboard;
    private readonly Dictionary<Guid, System.Timers.Timer> _kpiTimers = new();
    private readonly Dictionary<Guid, System.Timers.Timer> _workloadTimers = new();
    private readonly object _lock = new();

    public DashboardPushService(
        IHubContext<DashboardHub> hub,
        IDashboardRepository dashboard)
    {
        _hub = hub;
        _dashboard = dashboard;
    }

    public Task ScheduleKpiPushAsync(Guid departmentId, CancellationToken ct = default)
    {
        lock (_lock)
        {
            if (_kpiTimers.TryGetValue(departmentId, out var existing))
            {
                existing.Stop();
                existing.Start();
                return Task.CompletedTask;
            }

            var timer = new System.Timers.Timer(2000) { AutoReset = false };
            timer.Elapsed += async (_, _) => await PushKpiAsync(departmentId);
            _kpiTimers[departmentId] = timer;
            timer.Start();
        }
        return Task.CompletedTask;
    }

    public Task ScheduleWorkloadPushAsync(Guid departmentId, CancellationToken ct = default)
    {
        lock (_lock)
        {
            if (_workloadTimers.TryGetValue(departmentId, out var existing))
            {
                existing.Stop();
                existing.Start();
                return Task.CompletedTask;
            }

            var timer = new System.Timers.Timer(2000) { AutoReset = false };
            timer.Elapsed += async (_, _) => await PushWorkloadAsync(departmentId);
            _workloadTimers[departmentId] = timer;
            timer.Start();
        }
        return Task.CompletedTask;
    }

    private async Task PushKpiAsync(Guid departmentId)
    {
        lock (_lock) _kpiTimers.Remove(departmentId);
        var data = await _dashboard.GetKpisAsync(new[] { departmentId }, null);
        await _hub.Clients.Group("kpi-admin").SendAsync("KpiUpdated", data);
        await _hub.Clients.Group("kpi-manager").SendAsync("KpiUpdated", data);
    }

    private async Task PushWorkloadAsync(Guid departmentId)
    {
        lock (_lock) _workloadTimers.Remove(departmentId);
        var data = await _dashboard.GetKpisAsync(new[] { departmentId }, null);
        await _hub.Clients.Group("kpi-admin")
            .SendAsync("AgentWorkloadUpdated", data.AgentWorkload);
        await _hub.Clients.Group("kpi-manager")
            .SendAsync("AgentWorkloadUpdated", data.AgentWorkload);
    }
}
```

- [ ] **Step 9: Register hub and service in Program.cs**

Open `src/CRM.API/Program.cs` and add after existing SignalR registration:

```csharp
builder.Services.AddSingleton<IDashboardPusher, DashboardPushService>();

// In app.MapHubs section:
app.MapHub<DashboardHub>("/hubs/dashboard");
```

Ensure the JWT `OnMessageReceived` handler that reads `access_token` from query param (added in US-BE-054) covers this hub as well — it applies to all hubs globally.

- [ ] **Step 10: Commit**

```bash
git add src/CRM.Domain/Tickets/Events/ \
        src/CRM.Application/Dashboard/Services/IDashboardPusher.cs \
        src/CRM.Application/Dashboard/Events/DashboardPushEventHandler.cs \
        src/CRM.Infrastructure/Hubs/DashboardHub.cs \
        src/CRM.Infrastructure/Dashboard/DashboardPushService.cs \
        src/CRM.API/Program.cs \
        tests/CRM.Application.Tests/Dashboard/DashboardPushEventHandlerTests.cs
git commit -m "feat(dashboard): add DashboardHub with 2s debounced KpiUpdated/AgentWorkloadUpdated push"
```
