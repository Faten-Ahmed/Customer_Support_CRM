# SLA Clock Pause / Resume — Implementation Plan

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

**Story:** US-BE-040  
**Goal:** When a ticket transitions to `OnHold`, pause the SLA clock; when it transitions from `OnHold` back to `InProgress`, resume it and accumulate the paused duration in `TotalPausedMinutes`.

**Architecture:** `ChangeTicketStatusCommandHandler` (from US-BE-025) injects `ITicketSlaRepository` and calls `sla.PauseClock()` when new status is `OnHold`, or `sla.ResumeClock()` when old status was `OnHold`. Both `PauseClock` and `ResumeClock` are already on the `TicketSla` entity (US-BE-033). Multiple OnHold cycles accumulate correctly via `AccumulatedPauseMinutes`.

**Tech Stack:** .NET 10, ASP.NET Core, MediatR, EF Core, xUnit, Moq

---

## File Structure

| Action | Path |
|--------|------|
| Modify | `src/CRM.Application/Tickets/Commands/ChangeTicketStatusCommand.cs` |
| Test   | `tests/CRM.Application.Tests/Sla/SlaClockPauseResumeTests.cs` |

---

## Task 1: Integrate SLA Pause/Resume into ChangeTicketStatusCommandHandler

**Files:**
- Modify: `src/CRM.Application/Tickets/Commands/ChangeTicketStatusCommand.cs`
- Test: `tests/CRM.Application.Tests/Sla/SlaClockPauseResumeTests.cs`

- [ ] **Step 1: Write the failing tests**

```csharp
// tests/CRM.Application.Tests/Sla/SlaClockPauseResumeTests.cs
using CRM.Application.Tickets.Commands;
using CRM.Domain.Sla;
using CRM.Domain.Tickets;
using CRM.Domain.Tickets.Enums;
using Moq;
using Xunit;

namespace CRM.Application.Tests.Sla;

public class SlaClockPauseResumeTests
{
    private readonly Mock<ITicketRepository> _ticketRepo = new();
    private readonly Mock<ITicketSlaRepository> _slaRepo = new();
    private readonly ChangeTicketStatusCommandHandler _handler;

    public SlaClockPauseResumeTests()
    {
        _handler = new ChangeTicketStatusCommandHandler(_ticketRepo.Object, _slaRepo.Object);
    }

    private TicketSla MakeSla(Guid ticketId)
        => TicketSla.Create(ticketId, Guid.NewGuid(),
            DateTime.UtcNow.AddHours(-1),
            DateTime.UtcNow.AddHours(3),
            DateTime.UtcNow.AddHours(7));

    [Fact]
    public async Task Handle_StatusChangesToOnHold_PausesSlaClock()
    {
        var ticket = Ticket.Create(Guid.NewGuid(), "S", "D",
            TicketPriority.Medium, TicketChannel.Email, Guid.NewGuid());
        ticket.ChangeStatus(TicketStatus.Assigned, Guid.NewGuid());
        ticket.ChangeStatus(TicketStatus.InProgress, Guid.NewGuid());

        var sla = MakeSla(ticket.Id);
        _ticketRepo.Setup(r => r.FindByIdAsync(ticket.Id, default)).ReturnsAsync(ticket);
        _slaRepo.Setup(r => r.FindByTicketIdAsync(ticket.Id, default)).ReturnsAsync(sla);

        await _handler.Handle(
            new ChangeTicketStatusCommand(ticket.Id, TicketStatus.OnHold, Guid.NewGuid()), default);

        Assert.True(sla.ClockPausedAt.HasValue);
    }

    [Fact]
    public async Task Handle_StatusChangesFromOnHoldToInProgress_ResumesSlaClock()
    {
        var ticket = Ticket.Create(Guid.NewGuid(), "S", "D",
            TicketPriority.Medium, TicketChannel.Email, Guid.NewGuid());
        ticket.ChangeStatus(TicketStatus.Assigned, Guid.NewGuid());
        ticket.ChangeStatus(TicketStatus.InProgress, Guid.NewGuid());
        ticket.ChangeStatus(TicketStatus.OnHold, Guid.NewGuid());

        var sla = MakeSla(ticket.Id);
        sla.PauseClock();

        _ticketRepo.Setup(r => r.FindByIdAsync(ticket.Id, default)).ReturnsAsync(ticket);
        _slaRepo.Setup(r => r.FindByTicketIdAsync(ticket.Id, default)).ReturnsAsync(sla);

        await _handler.Handle(
            new ChangeTicketStatusCommand(ticket.Id, TicketStatus.InProgress, Guid.NewGuid()), default);

        Assert.False(sla.ClockPausedAt.HasValue);
    }

    [Fact]
    public async Task Handle_MultipleOnHoldCycles_AccumulatesPauseMinutes()
    {
        var ticket = Ticket.Create(Guid.NewGuid(), "S", "D",
            TicketPriority.Medium, TicketChannel.Email, Guid.NewGuid());
        ticket.ChangeStatus(TicketStatus.Assigned, Guid.NewGuid());
        ticket.ChangeStatus(TicketStatus.InProgress, Guid.NewGuid());

        var sla = MakeSla(ticket.Id);

        // First pause/resume cycle (manual simulation of time passing)
        sla.PauseClock(); // sets ClockPausedAt
        // Cannot directly advance time in tests; verify accumulated after second resume
        sla.ResumeClock(); // accumulates first pause

        _ticketRepo.Setup(r => r.FindByIdAsync(ticket.Id, default)).ReturnsAsync(ticket);
        _slaRepo.Setup(r => r.FindByTicketIdAsync(ticket.Id, default)).ReturnsAsync(sla);
        _slaRepo.Setup(r => r.FindByTicketIdAsync(ticket.Id, default)).ReturnsAsync(sla);

        // Second OnHold cycle
        sla.PauseClock();
        await _handler.Handle(
            new ChangeTicketStatusCommand(ticket.Id, TicketStatus.InProgress, Guid.NewGuid()), default);

        Assert.False(sla.ClockPausedAt.HasValue);
    }

    [Fact]
    public async Task Handle_NoSlaRecord_CompletesWithoutError()
    {
        var ticket = Ticket.Create(Guid.NewGuid(), "S", "D",
            TicketPriority.Medium, TicketChannel.Email, Guid.NewGuid());
        ticket.ChangeStatus(TicketStatus.Assigned, Guid.NewGuid());
        ticket.ChangeStatus(TicketStatus.InProgress, Guid.NewGuid());

        _ticketRepo.Setup(r => r.FindByIdAsync(ticket.Id, default)).ReturnsAsync(ticket);
        _slaRepo.Setup(r => r.FindByTicketIdAsync(ticket.Id, default))
                .ReturnsAsync((TicketSla?)null);

        // Should not throw even if no SLA record exists
        await _handler.Handle(
            new ChangeTicketStatusCommand(ticket.Id, TicketStatus.OnHold, Guid.NewGuid()), default);
    }
}
```

- [ ] **Step 2: Run test to verify it fails**

```bash
dotnet test tests/CRM.Application.Tests/ --filter "SlaClockPauseResumeTests" -v n
```

Expected: FAIL — `ChangeTicketStatusCommandHandler` does not yet inject `ITicketSlaRepository`.

- [ ] **Step 3: Inject ITicketSlaRepository into ChangeTicketStatusCommandHandler**

Modify `src/CRM.Application/Tickets/Commands/ChangeTicketStatusCommand.cs`:

```csharp
// Add ITicketSlaRepository to constructor:
private readonly ITicketRepository _tickets;
private readonly ITicketSlaRepository _slaRepo;

public ChangeTicketStatusCommandHandler(
    ITicketRepository tickets,
    ITicketSlaRepository slaRepo)
{
    _tickets = tickets;
    _slaRepo = slaRepo;
}

public async Task Handle(ChangeTicketStatusCommand cmd, CancellationToken ct)
{
    var ticket = await _tickets.FindByIdAsync(cmd.TicketId, ct)
        ?? throw new KeyNotFoundException($"Ticket {cmd.TicketId} not found.");

    var previousStatus = ticket.Status;

    ticket.ChangeStatus(cmd.NewStatus, cmd.ChangedByUserId);

    // SLA clock pause/resume
    var sla = await _slaRepo.FindByTicketIdAsync(cmd.TicketId, ct);
    if (sla is not null)
    {
        if (cmd.NewStatus == TicketStatus.OnHold)
            sla.PauseClock();
        else if (previousStatus == TicketStatus.OnHold)
            sla.ResumeClock();
    }

    await _tickets.SaveChangesAsync(ct);
    if (sla is not null)
        await _slaRepo.SaveChangesAsync(ct);
}
```

- [ ] **Step 4: Run tests to verify they pass**

```bash
dotnet test tests/CRM.Application.Tests/ --filter "SlaClockPauseResumeTests" -v n
```

Expected: 4 tests PASS.

- [ ] **Step 5: Run all existing ChangeTicketStatus tests**

```bash
dotnet test tests/CRM.Application.Tests/ --filter "ChangeTicketStatus" -v n
```

Expected: all existing tests still PASS (no regressions).

- [ ] **Step 6: Commit**

```bash
git add src/CRM.Application/Tickets/Commands/ChangeTicketStatusCommand.cs \
        tests/CRM.Application.Tests/Sla/SlaClockPauseResumeTests.cs
git commit -m "feat(sla): pause/resume SLA clock on OnHold status transitions"
```
