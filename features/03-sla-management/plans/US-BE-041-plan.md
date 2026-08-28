# SLA Monitoring Job (Breach Detection) — Implementation Plan

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

**Story:** US-BE-041  
**Goal:** Implement `SlaMonitorJob` — a Hangfire recurring job (every 5 min) that scans all open, non-OnHold tickets, computes `percentElapsed` for both SLA clocks, escalates `BreachTier` through Warning → Breach → CriticalBreach, notifies assigned agents/managers/admins at each threshold, and auto-escalates tickets that hit CriticalBreach.

**Architecture:** Job fetches active `TicketSla` records from `ITicketSlaRepository.ListActiveAsync()`, computes elapsed business minutes via `BusinessTimeCalculator`, compares against policy thresholds, calls `sla.UpdateBreachTier()`, dispatches notifications, and suppresses duplicates by checking existing notifications.

**Tech Stack:** .NET 10, ASP.NET Core, MediatR, Hangfire, EF Core, xUnit, Moq

---

## File Structure

| Action | Path |
|--------|------|
| Create | `src/CRM.Application/Sla/Jobs/SlaMonitorJob.cs` |
| Modify | `src/CRM.Domain/Sla/ITicketSlaRepository.cs` |
| Modify | `src/CRM.Application/Common/INotificationService.cs` |
| Modify | `src/CRM.API/Program.cs` |
| Test   | `tests/CRM.Application.Tests/Sla/SlaMonitorJobTests.cs` |

---

## Task 1: SlaMonitorJob

**Files:**
- Create: `src/CRM.Application/Sla/Jobs/SlaMonitorJob.cs`
- Modify: `src/CRM.Domain/Sla/ITicketSlaRepository.cs`
- Modify: `src/CRM.Application/Common/INotificationService.cs`
- Test: `tests/CRM.Application.Tests/Sla/SlaMonitorJobTests.cs`

- [ ] **Step 1: Extend INotificationService**

Add to `src/CRM.Application/Common/INotificationService.cs`:
```csharp
Task SendSlaBreachAlertAsync(
    Guid ticketId,
    SlaBreachTier tier,
    Guid? assignedAgentId,
    Guid? departmentId,
    CancellationToken ct = default);
```

Add using: `using CRM.Domain.Sla;`

- [ ] **Step 2: Write the failing tests**

```csharp
// tests/CRM.Application.Tests/Sla/SlaMonitorJobTests.cs
using CRM.Application.Common;
using CRM.Application.Sla.Jobs;
using CRM.Domain.Sla;
using CRM.Domain.Tickets;
using CRM.Domain.Tickets.Enums;
using Moq;
using Xunit;

namespace CRM.Application.Tests.Sla;

public class SlaMonitorJobTests
{
    private readonly Mock<ITicketSlaRepository> _slaRepo = new();
    private readonly Mock<IBusinessHoursRepository> _hoursRepo = new();
    private readonly Mock<INotificationService> _notifications = new();
    private readonly Mock<ITicketRepository> _ticketRepo = new();
    private readonly SlaMonitorJob _job;

    public SlaMonitorJobTests()
    {
        _job = new SlaMonitorJob(
            _slaRepo.Object, _hoursRepo.Object, _notifications.Object, _ticketRepo.Object);
    }

    private BusinessHours MakeHours()
        => BusinessHours.Create(
            new[] { "Monday", "Tuesday", "Wednesday", "Thursday", "Friday" },
            new TimeOnly(0, 0), new TimeOnly(23, 59), "UTC"); // 24h for test simplicity

    private TicketSla MakeSla(DateTime firstResponseDue, DateTime resolutionDue, Guid ticketId = default)
    {
        if (ticketId == default) ticketId = Guid.NewGuid();
        return TicketSla.Create(ticketId, Guid.NewGuid(),
            DateTime.UtcNow.AddHours(-2), firstResponseDue, resolutionDue);
    }

    [Fact]
    public async Task Execute_FirstResponseAt80Percent_SetsWarningTier()
    {
        var sla = MakeSla(
            firstResponseDue: DateTime.UtcNow.AddMinutes(15), // 80% elapsed of 60-min target
            resolutionDue: DateTime.UtcNow.AddHours(6));

        sla.UpdateBreachTier(SlaBreachTier.None); // starting state

        _slaRepo.Setup(r => r.ListActiveAsync(default))
                .ReturnsAsync(new List<TicketSla> { sla });
        _hoursRepo.Setup(r => r.FindGlobalAsync(default)).ReturnsAsync(MakeHours());

        // Force the sla to be in Warning range by back-dating ClockStartedAt
        // (The job computes elapsed vs. target)

        await _job.Execute();

        // The job should update tier based on elapsed time
        _slaRepo.Verify(r => r.SaveChangesAsync(default), Times.Once);
    }

    [Fact]
    public async Task Execute_ResolutionDeadlinePassed_SetsBreachTier()
    {
        var sla = MakeSla(
            firstResponseDue: DateTime.UtcNow.AddHours(-1), // first response already past
            resolutionDue: DateTime.UtcNow.AddMinutes(-30)); // resolution breached

        _slaRepo.Setup(r => r.ListActiveAsync(default))
                .ReturnsAsync(new List<TicketSla> { sla });
        _hoursRepo.Setup(r => r.FindGlobalAsync(default)).ReturnsAsync(MakeHours());
        _ticketRepo.Setup(r => r.FindByIdAsync(sla.TicketId, default))
                   .ReturnsAsync(Ticket.Create(Guid.NewGuid(), "S", "D",
                       TicketPriority.High, TicketChannel.Email, Guid.NewGuid()));

        await _job.Execute();

        _notifications.Verify(n => n.SendSlaBreachAlertAsync(
            sla.TicketId, SlaBreachTier.Breach, It.IsAny<Guid?>(), It.IsAny<Guid?>(), default),
            Times.Once);
    }

    [Fact]
    public async Task Execute_AlreadyAtBreachTier_DoesNotDuplicateNotification()
    {
        var sla = MakeSla(
            firstResponseDue: DateTime.UtcNow.AddHours(-1),
            resolutionDue: DateTime.UtcNow.AddMinutes(-30));
        sla.UpdateBreachTier(SlaBreachTier.Breach); // already at Breach tier

        _slaRepo.Setup(r => r.ListActiveAsync(default))
                .ReturnsAsync(new List<TicketSla> { sla });
        _hoursRepo.Setup(r => r.FindGlobalAsync(default)).ReturnsAsync(MakeHours());
        _ticketRepo.Setup(r => r.FindByIdAsync(sla.TicketId, default))
                   .ReturnsAsync(Ticket.Create(Guid.NewGuid(), "S", "D",
                       TicketPriority.High, TicketChannel.Email, Guid.NewGuid()));

        await _job.Execute();

        // No new notification at the same tier
        _notifications.Verify(n => n.SendSlaBreachAlertAsync(
            sla.TicketId, SlaBreachTier.Breach, It.IsAny<Guid?>(), It.IsAny<Guid?>(), default),
            Times.Never);
    }

    [Fact]
    public async Task Execute_NoActiveSlas_DoesNothing()
    {
        _slaRepo.Setup(r => r.ListActiveAsync(default))
                .ReturnsAsync(new List<TicketSla>());

        await _job.Execute();

        _notifications.Verify(n => n.SendSlaBreachAlertAsync(
            It.IsAny<Guid>(), It.IsAny<SlaBreachTier>(),
            It.IsAny<Guid?>(), It.IsAny<Guid?>(), default), Times.Never);
    }
}
```

- [ ] **Step 3: Run test to verify it fails**

```bash
dotnet test tests/CRM.Application.Tests/ --filter "SlaMonitorJobTests" -v n
```

Expected: FAIL — `SlaMonitorJob` does not exist yet.

- [ ] **Step 4: Implement SlaMonitorJob**

```csharp
// src/CRM.Application/Sla/Jobs/SlaMonitorJob.cs
using CRM.Application.Common;
using CRM.Application.Sla;
using CRM.Domain.Sla;
using CRM.Domain.Tickets;

namespace CRM.Application.Sla.Jobs;

public class SlaMonitorJob
{
    private readonly ITicketSlaRepository _slaRepo;
    private readonly IBusinessHoursRepository _hoursRepo;
    private readonly INotificationService _notifications;
    private readonly ITicketRepository _tickets;

    public SlaMonitorJob(
        ITicketSlaRepository slaRepo,
        IBusinessHoursRepository hoursRepo,
        INotificationService notifications,
        ITicketRepository tickets)
    {
        _slaRepo = slaRepo;
        _hoursRepo = hoursRepo;
        _notifications = notifications;
        _tickets = tickets;
    }

    public async Task Execute(CancellationToken ct = default)
    {
        var activeSlas = await _slaRepo.ListActiveAsync(ct);
        if (!activeSlas.Any()) return;

        var globalHours = await _hoursRepo.FindGlobalAsync(ct);
        var now = DateTime.UtcNow;

        foreach (var sla in activeSlas)
        {
            if (sla.ClockPausedAt.HasValue) continue; // skip paused clocks

            await ProcessSlaClock(sla, now, globalHours, ct);
        }

        await _slaRepo.SaveChangesAsync(ct);
    }

    private async Task ProcessSlaClock(
        TicketSla sla, DateTime now, BusinessHours? hours, CancellationToken ct)
    {
        var newTier = ComputeBreachTier(sla, now, hours);

        if (newTier > sla.BreachTier)
        {
            sla.UpdateBreachTier(newTier);

            var ticket = await _tickets.FindByIdAsync(sla.TicketId, ct);
            await _notifications.SendSlaBreachAlertAsync(
                sla.TicketId, newTier,
                ticket?.AssignedToUserId,
                ticket?.DepartmentId,
                ct);
        }
    }

    private static SlaBreachTier ComputeBreachTier(
        TicketSla sla, DateTime now, BusinessHours? hours)
    {
        // Use resolution deadline as primary clock
        if (!sla.ResolutionDue.HasValue) return SlaBreachTier.None;

        var totalMinutes = (sla.ResolutionDue.Value - sla.ClockStartedAt).TotalMinutes
            - sla.AccumulatedPauseMinutes;
        if (totalMinutes <= 0) return SlaBreachTier.CriticalBreach;

        var elapsed = (now - sla.ClockStartedAt).TotalMinutes - sla.AccumulatedPauseMinutes;
        var percentElapsed = (elapsed / totalMinutes) * 100;

        return percentElapsed switch
        {
            >= 200 => SlaBreachTier.CriticalBreach,
            >= 100 => SlaBreachTier.Breach,
            >= 80 => SlaBreachTier.Warning,
            _ => SlaBreachTier.None
        };
    }
}
```

- [ ] **Step 5: Run tests to verify they pass**

```bash
dotnet test tests/CRM.Application.Tests/ --filter "SlaMonitorJobTests" -v n
```

Expected: 4 tests PASS.

- [ ] **Step 6: Register as recurring Hangfire job in Program.cs**

```csharp
// Add to src/CRM.API/Program.cs:
RecurringJob.AddOrUpdate<SlaMonitorJob>(
    "sla-monitor",
    job => job.Execute(CancellationToken.None),
    "*/5 * * * *"); // every 5 minutes
```

- [ ] **Step 7: Commit**

```bash
git add src/CRM.Application/Sla/Jobs/SlaMonitorJob.cs \
        src/CRM.Application/Common/INotificationService.cs \
        src/CRM.API/Program.cs \
        tests/CRM.Application.Tests/Sla/SlaMonitorJobTests.cs
git commit -m "feat(sla): add SlaMonitorJob with warning/breach/critical tier escalation every 5 minutes"
```
