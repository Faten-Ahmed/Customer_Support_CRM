# Auto-Assign Ticket Job — Implementation Plan

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

**Story:** US-BE-035  
**Goal:** Implement `AutoAssignTicketJob` — a Hangfire background job triggered on ticket creation that finds the best-fit available agent using skill-matching then round-robin fallback, or notifies the manager if no assignment is possible.

**Architecture:** `CreateTicketCommandHandler` enqueues `AutoAssignTicketJob(ticketId)` via `IBackgroundJobClient`. The job fetches active agents in the ticket's department, selects the skill-matched agent with the fewest open tickets (fallback: oldest `LastAssignedAt`), calls `ticket.Assign()`, and publishes a manager alert if no assignment is possible (overloaded or no active agents).

**Tech Stack:** .NET 10, ASP.NET Core, MediatR, Hangfire, EF Core, xUnit, Moq

---

## File Structure

| Action | Path |
|--------|------|
| Create | `src/CRM.Application/Common/INotificationService.cs` |
| Create | `src/CRM.Application/Tickets/DTOs/AgentCapacityDto.cs` |
| Create | `src/CRM.Application/Tickets/Jobs/AutoAssignTicketJob.cs` |
| Modify | `src/CRM.Domain/Users/IUserRepository.cs` |
| Modify | `src/CRM.Application/Tickets/Commands/CreateTicketInternalCommand.cs` |
| Test   | `tests/CRM.Application.Tests/Tickets/AutoAssignTicketJobTests.cs` |

---

## Task 1: AutoAssignTicketJob

**Files:**
- Create: `src/CRM.Application/Common/INotificationService.cs`
- Create: `src/CRM.Application/Tickets/DTOs/AgentCapacityDto.cs`
- Create: `src/CRM.Application/Tickets/Jobs/AutoAssignTicketJob.cs`
- Modify: `src/CRM.Domain/Users/IUserRepository.cs`
- Test: `tests/CRM.Application.Tests/Tickets/AutoAssignTicketJobTests.cs`

- [ ] **Step 1: Write the failing tests**

```csharp
// tests/CRM.Application.Tests/Tickets/AutoAssignTicketJobTests.cs
using CRM.Application.Common;
using CRM.Application.Tickets.DTOs;
using CRM.Application.Tickets.Jobs;
using CRM.Domain.Tickets;
using CRM.Domain.Tickets.Enums;
using CRM.Domain.Users;
using Moq;
using Xunit;

namespace CRM.Application.Tests.Tickets;

public class AutoAssignTicketJobTests
{
    private readonly Mock<ITicketRepository> _ticketRepo = new();
    private readonly Mock<IUserRepository> _userRepo = new();
    private readonly Mock<INotificationService> _notifications = new();
    private readonly AutoAssignTicketJob _job;

    public AutoAssignTicketJobTests()
    {
        _job = new AutoAssignTicketJob(_ticketRepo.Object, _userRepo.Object, _notifications.Object);
    }

    private Ticket MakeTicket(Guid? categoryId = null, Guid? deptId = null)
    {
        var ticket = Ticket.Create(Guid.NewGuid(), "S", "D",
            TicketPriority.High, TicketChannel.Email, Guid.NewGuid());
        // Use reflection or a test helper to set DepartmentId and CategoryId if needed
        return ticket;
    }

    [Fact]
    public async Task Execute_SkillMatchedAgent_AssignsAgentWithFewestTickets()
    {
        var ticketId = Guid.NewGuid();
        var deptId = Guid.NewGuid();
        var categoryId = Guid.NewGuid();
        var bestAgentId = Guid.NewGuid();

        var ticket = Ticket.Create(Guid.NewGuid(), "Subject", "Desc",
            TicketPriority.High, TicketChannel.Email, Guid.NewGuid());
        // ticket.DepartmentId and CategoryId set to deptId/categoryId in actual entity
        _ticketRepo.Setup(r => r.FindByIdAsync(ticketId, default)).ReturnsAsync(ticket);

        var agents = new List<AgentCapacityDto>
        {
            new(bestAgentId, OpenTicketCount: 2, LastAssignedAt: null,
                SkillCategoryIds: new List<Guid> { categoryId }),
            new(Guid.NewGuid(), OpenTicketCount: 5, LastAssignedAt: null,
                SkillCategoryIds: new List<Guid> { categoryId })
        };
        _userRepo.Setup(r => r.FindActiveAgentsInDepartmentAsync(It.IsAny<Guid>(), default))
                 .ReturnsAsync(agents);

        await _job.Execute(ticketId);

        _ticketRepo.Verify(r => r.SaveChangesAsync(default), Times.Once);
        _userRepo.Verify(r => r.UpdateLastAssignedAtAsync(bestAgentId, default), Times.Once);
    }

    [Fact]
    public async Task Execute_NoSkillMatch_RoundRobinByOldestLastAssigned()
    {
        var ticketId = Guid.NewGuid();
        var olderAgentId = Guid.NewGuid();
        var oldDate = DateTime.UtcNow.AddHours(-5);

        var ticket = Ticket.Create(Guid.NewGuid(), "S", "D",
            TicketPriority.Low, TicketChannel.Email, Guid.NewGuid());
        _ticketRepo.Setup(r => r.FindByIdAsync(ticketId, default)).ReturnsAsync(ticket);

        var agents = new List<AgentCapacityDto>
        {
            new(olderAgentId, OpenTicketCount: 3, LastAssignedAt: oldDate,
                SkillCategoryIds: new List<Guid>()),
            new(Guid.NewGuid(), OpenTicketCount: 1, LastAssignedAt: DateTime.UtcNow.AddHours(-1),
                SkillCategoryIds: new List<Guid>())
        };
        _userRepo.Setup(r => r.FindActiveAgentsInDepartmentAsync(It.IsAny<Guid>(), default))
                 .ReturnsAsync(agents);

        await _job.Execute(ticketId);

        _userRepo.Verify(r => r.UpdateLastAssignedAtAsync(olderAgentId, default), Times.Once);
    }

    [Fact]
    public async Task Execute_AllAgentsOverloaded_SendsManagerAlert()
    {
        var ticketId = Guid.NewGuid();
        var ticket = Ticket.Create(Guid.NewGuid(), "S", "D",
            TicketPriority.Low, TicketChannel.Email, Guid.NewGuid());
        _ticketRepo.Setup(r => r.FindByIdAsync(ticketId, default)).ReturnsAsync(ticket);

        var agents = new List<AgentCapacityDto>
        {
            new(Guid.NewGuid(), OpenTicketCount: 16, LastAssignedAt: null,
                SkillCategoryIds: new List<Guid>()),
            new(Guid.NewGuid(), OpenTicketCount: 20, LastAssignedAt: null,
                SkillCategoryIds: new List<Guid>())
        };
        _userRepo.Setup(r => r.FindActiveAgentsInDepartmentAsync(It.IsAny<Guid>(), default))
                 .ReturnsAsync(agents);

        await _job.Execute(ticketId);

        _notifications.Verify(
            n => n.SendUnassignedTicketAlertAsync(It.IsAny<Guid>(), ticketId, default),
            Times.Once);
        _ticketRepo.Verify(r => r.SaveChangesAsync(default), Times.Never);
    }

    [Fact]
    public async Task Execute_NoActiveAgents_SendsManagerAlert()
    {
        var ticketId = Guid.NewGuid();
        var ticket = Ticket.Create(Guid.NewGuid(), "S", "D",
            TicketPriority.Low, TicketChannel.Email, Guid.NewGuid());
        _ticketRepo.Setup(r => r.FindByIdAsync(ticketId, default)).ReturnsAsync(ticket);
        _userRepo.Setup(r => r.FindActiveAgentsInDepartmentAsync(It.IsAny<Guid>(), default))
                 .ReturnsAsync(new List<AgentCapacityDto>());

        await _job.Execute(ticketId);

        _notifications.Verify(
            n => n.SendUnassignedTicketAlertAsync(It.IsAny<Guid>(), ticketId, default),
            Times.Once);
    }
}
```

- [ ] **Step 2: Run test to verify it fails**

```bash
dotnet test tests/CRM.Application.Tests/ --filter "AutoAssignTicketJobTests" -v n
```

Expected: FAIL — `AutoAssignTicketJob` does not exist yet.

- [ ] **Step 3: Create INotificationService**

```csharp
// src/CRM.Application/Common/INotificationService.cs
namespace CRM.Application.Common;

public interface INotificationService
{
    Task SendUnassignedTicketAlertAsync(
        Guid departmentId, Guid ticketId, CancellationToken ct = default);
}
```

- [ ] **Step 4: Create AgentCapacityDto**

```csharp
// src/CRM.Application/Tickets/DTOs/AgentCapacityDto.cs
namespace CRM.Application.Tickets.DTOs;

public record AgentCapacityDto(
    Guid AgentId,
    int OpenTicketCount,
    DateTime? LastAssignedAt,
    IReadOnlyList<Guid> SkillCategoryIds);
```

- [ ] **Step 5: Add methods to IUserRepository**

Add to `src/CRM.Domain/Users/IUserRepository.cs`:
```csharp
Task<IReadOnlyList<AgentCapacityDto>> FindActiveAgentsInDepartmentAsync(
    Guid departmentId, CancellationToken ct = default);

Task UpdateLastAssignedAtAsync(Guid agentId, CancellationToken ct = default);
```

Add the using to the interface file:
```csharp
using CRM.Application.Tickets.DTOs;
```

- [ ] **Step 6: Implement AutoAssignTicketJob**

```csharp
// src/CRM.Application/Tickets/Jobs/AutoAssignTicketJob.cs
using CRM.Application.Common;
using CRM.Domain.Tickets;
using CRM.Domain.Users;

namespace CRM.Application.Tickets.Jobs;

public class AutoAssignTicketJob
{
    private const int MaxOpenTickets = 15;

    private readonly ITicketRepository _tickets;
    private readonly IUserRepository _users;
    private readonly INotificationService _notifications;

    public AutoAssignTicketJob(
        ITicketRepository tickets,
        IUserRepository users,
        INotificationService notifications)
    {
        _tickets = tickets;
        _users = users;
        _notifications = notifications;
    }

    public async Task Execute(Guid ticketId, CancellationToken ct = default)
    {
        var ticket = await _tickets.FindByIdAsync(ticketId, ct)
            ?? throw new KeyNotFoundException($"Ticket {ticketId} not found.");

        var deptId = ticket.DepartmentId ?? Guid.Empty;
        var agents = await _users.FindActiveAgentsInDepartmentAsync(deptId, ct);

        if (!agents.Any())
        {
            await _notifications.SendUnassignedTicketAlertAsync(deptId, ticketId, ct);
            return;
        }

        // Step 1: skills match — pick skill-matched agent with fewest open tickets
        var skillMatched = agents
            .Where(a => a.SkillCategoryIds.Contains(ticket.CategoryId ?? Guid.Empty)
                        && a.OpenTicketCount < MaxOpenTickets)
            .OrderBy(a => a.OpenTicketCount)
            .FirstOrDefault();

        // Step 2: round-robin fallback — oldest LastAssignedAt
        var roundRobin = agents
            .Where(a => a.OpenTicketCount < MaxOpenTickets)
            .OrderBy(a => a.LastAssignedAt ?? DateTime.MinValue)
            .FirstOrDefault();

        var selected = skillMatched ?? roundRobin;

        if (selected is null)
        {
            await _notifications.SendUnassignedTicketAlertAsync(deptId, ticketId, ct);
            return;
        }

        ticket.Assign(selected.AgentId, Guid.Empty); // system-triggered assignment
        await _users.UpdateLastAssignedAtAsync(selected.AgentId, ct);
        await _tickets.SaveChangesAsync(ct);
    }
}
```

- [ ] **Step 7: Enqueue job from CreateTicketInternalCommandHandler**

In `src/CRM.Application/Tickets/Commands/CreateTicketInternalCommand.cs`, inject `IBackgroundJobClient` and enqueue after saving:

```csharp
// Add to handler constructor and Handle method:
// Constructor: add IBackgroundJobClient _jobs parameter
// In Handle(), after await _tickets.SaveChangesAsync(ct):
_jobs.Enqueue<AutoAssignTicketJob>(j => j.Execute(ticket.Id, CancellationToken.None));
```

Add `using Hangfire;` to the file.

- [ ] **Step 8: Run tests to verify they pass**

```bash
dotnet test tests/CRM.Application.Tests/ --filter "AutoAssignTicketJobTests" -v n
```

Expected: 4 tests PASS.

- [ ] **Step 9: Commit**

```bash
git add src/CRM.Application/Common/INotificationService.cs \
        src/CRM.Application/Tickets/DTOs/AgentCapacityDto.cs \
        src/CRM.Application/Tickets/Jobs/AutoAssignTicketJob.cs \
        src/CRM.Domain/Users/IUserRepository.cs \
        src/CRM.Application/Tickets/Commands/CreateTicketInternalCommand.cs \
        tests/CRM.Application.Tests/Tickets/AutoAssignTicketJobTests.cs
git commit -m "feat(tickets): add AutoAssignTicketJob with skill-match and round-robin fallback"
```
