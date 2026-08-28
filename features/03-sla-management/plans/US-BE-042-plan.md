# SLA Deadline Recalculation on Transfer — Implementation Plan

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

**Story:** US-BE-042  
**Goal:** When a ticket is transferred to a different department, recalculate SLA deadlines based on the new department's policy, factoring in already-elapsed business minutes so the remaining time is based on the new policy minus what has already been consumed.

**Architecture:** `RecalculateSlaOnTransferCommand(TicketId, NewDepartmentId)` → handler loads `TicketSla`, computes elapsed business minutes since clock start, loads new department's `SlaPolicy`, computes new deadlines as `addBusinessMinutes(now, newPolicy.Minutes - elapsed, newDeptHours)`, updates the `TicketSla` record, and writes a `TicketHistory` entry. Invoked by `TransferTicketCommandHandler` after the transfer.

**Tech Stack:** .NET 10, ASP.NET Core, MediatR, EF Core, xUnit, Moq

---

## File Structure

| Action | Path |
|--------|------|
| Create | `src/CRM.Application/Sla/Commands/RecalculateSlaOnTransferCommand.cs` |
| Modify | `src/CRM.Application/Tickets/Commands/TransferTicketCommand.cs` |
| Modify | `src/CRM.Domain/Sla/TicketSla.cs` |
| Test   | `tests/CRM.Application.Tests/Sla/RecalculateSlaOnTransferCommandHandlerTests.cs` |

---

## Task 1: RecalculateSlaOnTransfer Command + Handler

**Files:**
- Create: `src/CRM.Application/Sla/Commands/RecalculateSlaOnTransferCommand.cs`
- Modify: `src/CRM.Domain/Sla/TicketSla.cs`
- Test: `tests/CRM.Application.Tests/Sla/RecalculateSlaOnTransferCommandHandlerTests.cs`

- [ ] **Step 1: Add RecalculateDeadlines to TicketSla entity**

Add to `src/CRM.Domain/Sla/TicketSla.cs`:
```csharp
public void RecalculateDeadlines(
    Guid newSlaPolicyId,
    DateTime? newFirstResponseDue,
    DateTime? newResolutionDue)
{
    SlaPolicyId = newSlaPolicyId;
    FirstResponseDue = newFirstResponseDue;
    ResolutionDue = newResolutionDue;
    BreachTier = SlaBreachTier.None; // reset on transfer
    UpdatedAt = DateTime.UtcNow;
}
```

- [ ] **Step 2: Write the failing tests**

```csharp
// tests/CRM.Application.Tests/Sla/RecalculateSlaOnTransferCommandHandlerTests.cs
using CRM.Application.Sla;
using CRM.Application.Sla.Commands;
using CRM.Domain.Sla;
using CRM.Domain.Tickets;
using CRM.Domain.Tickets.Enums;
using Moq;
using Xunit;

namespace CRM.Application.Tests.Sla;

public class RecalculateSlaOnTransferCommandHandlerTests
{
    private readonly Mock<ITicketSlaRepository> _slaRepo = new();
    private readonly Mock<ISlaPolicyRepository> _policyRepo = new();
    private readonly Mock<IBusinessHoursRepository> _hoursRepo = new();
    private readonly Mock<ITicketRepository> _ticketRepo = new();
    private readonly RecalculateSlaOnTransferCommandHandler _handler;

    public RecalculateSlaOnTransferCommandHandlerTests()
    {
        _handler = new RecalculateSlaOnTransferCommandHandler(
            _slaRepo.Object, _policyRepo.Object, _hoursRepo.Object, _ticketRepo.Object);
    }

    private BusinessHours MakeHours()
        => BusinessHours.Create(
            new[] { "Monday", "Tuesday", "Wednesday", "Thursday", "Friday" },
            new TimeOnly(0, 0), new TimeOnly(23, 59), "UTC");

    [Fact]
    public async Task Handle_NewDeptHasPolicy_RecalculatesDeadlines()
    {
        var ticketId = Guid.NewGuid();
        var newDeptId = Guid.NewGuid();
        var ticket = Ticket.Create(Guid.NewGuid(), "S", "D",
            TicketPriority.High, TicketChannel.Email, Guid.NewGuid());

        var sla = TicketSla.Create(ticketId, Guid.NewGuid(),
            DateTime.UtcNow.AddHours(-1), // started 1 hour ago
            DateTime.UtcNow.AddHours(3),
            DateTime.UtcNow.AddHours(7));

        var newPolicy = SlaPolicy.Create(TicketPriority.High, 120, 480, newDeptId);
        var hours = MakeHours();

        _ticketRepo.Setup(r => r.FindByIdAsync(ticketId, default)).ReturnsAsync(ticket);
        _slaRepo.Setup(r => r.FindByTicketIdAsync(ticketId, default)).ReturnsAsync(sla);
        _policyRepo.Setup(r => r.FindByDepartmentAndPriorityAsync(
            newDeptId, TicketPriority.High, default)).ReturnsAsync(newPolicy);
        _hoursRepo.Setup(r => r.FindByDepartmentAsync(newDeptId, default)).ReturnsAsync(hours);

        await _handler.Handle(
            new RecalculateSlaOnTransferCommand(ticketId, newDeptId), default);

        _slaRepo.Verify(r => r.SaveChangesAsync(default), Times.Once);
        // Verify new deadlines are in the future (remaining = 120 - elapsed minutes)
        Assert.NotNull(sla.FirstResponseDue);
        Assert.NotNull(sla.ResolutionDue);
    }

    [Fact]
    public async Task Handle_ElapsedExceedsNewPolicy_SetsDeadlineToNow()
    {
        var ticketId = Guid.NewGuid();
        var newDeptId = Guid.NewGuid();
        var ticket = Ticket.Create(Guid.NewGuid(), "S", "D",
            TicketPriority.Low, TicketChannel.Email, Guid.NewGuid());

        var sla = TicketSla.Create(ticketId, Guid.NewGuid(),
            DateTime.UtcNow.AddHours(-5), // started 5 hours ago (300 min)
            DateTime.UtcNow.AddHours(1),
            DateTime.UtcNow.AddHours(3));

        // New policy has only 60-min first response and 120-min resolution
        var newPolicy = SlaPolicy.Create(TicketPriority.Low, 60, 120, newDeptId);
        var hours = MakeHours();

        _ticketRepo.Setup(r => r.FindByIdAsync(ticketId, default)).ReturnsAsync(ticket);
        _slaRepo.Setup(r => r.FindByTicketIdAsync(ticketId, default)).ReturnsAsync(sla);
        _policyRepo.Setup(r => r.FindByDepartmentAndPriorityAsync(
            newDeptId, TicketPriority.Low, default)).ReturnsAsync(newPolicy);
        _hoursRepo.Setup(r => r.FindByDepartmentAsync(newDeptId, default)).ReturnsAsync(hours);

        await _handler.Handle(
            new RecalculateSlaOnTransferCommand(ticketId, newDeptId), default);

        // Deadline should be set to now (already breached on arrival)
        Assert.True(sla.ResolutionDue <= DateTime.UtcNow.AddMinutes(1));
    }

    [Fact]
    public async Task Handle_NoSlaRecord_DoesNothing()
    {
        var ticketId = Guid.NewGuid();
        var ticket = Ticket.Create(Guid.NewGuid(), "S", "D",
            TicketPriority.Low, TicketChannel.Email, Guid.NewGuid());

        _ticketRepo.Setup(r => r.FindByIdAsync(ticketId, default)).ReturnsAsync(ticket);
        _slaRepo.Setup(r => r.FindByTicketIdAsync(ticketId, default))
                .ReturnsAsync((TicketSla?)null);

        await _handler.Handle(
            new RecalculateSlaOnTransferCommand(ticketId, Guid.NewGuid()), default);

        _slaRepo.Verify(r => r.SaveChangesAsync(default), Times.Never);
    }
}
```

- [ ] **Step 3: Run test to verify it fails**

```bash
dotnet test tests/CRM.Application.Tests/ --filter "RecalculateSlaOnTransferCommandHandlerTests" -v n
```

Expected: FAIL — `RecalculateSlaOnTransferCommand` does not exist yet.

- [ ] **Step 4: Implement command and handler**

```csharp
// src/CRM.Application/Sla/Commands/RecalculateSlaOnTransferCommand.cs
using CRM.Application.Sla;
using CRM.Domain.Sla;
using CRM.Domain.Tickets;
using MediatR;

namespace CRM.Application.Sla.Commands;

public record RecalculateSlaOnTransferCommand(
    Guid TicketId, Guid NewDepartmentId) : IRequest;

public class RecalculateSlaOnTransferCommandHandler
    : IRequestHandler<RecalculateSlaOnTransferCommand>
{
    private readonly ITicketSlaRepository _slaRepo;
    private readonly ISlaPolicyRepository _policies;
    private readonly IBusinessHoursRepository _businessHours;
    private readonly ITicketRepository _tickets;

    public RecalculateSlaOnTransferCommandHandler(
        ITicketSlaRepository slaRepo,
        ISlaPolicyRepository policies,
        IBusinessHoursRepository businessHours,
        ITicketRepository tickets)
    {
        _slaRepo = slaRepo;
        _policies = policies;
        _businessHours = businessHours;
        _tickets = tickets;
    }

    public async Task Handle(RecalculateSlaOnTransferCommand cmd, CancellationToken ct)
    {
        var ticket = await _tickets.FindByIdAsync(cmd.TicketId, ct)
            ?? throw new KeyNotFoundException($"Ticket {cmd.TicketId} not found.");

        var sla = await _slaRepo.FindByTicketIdAsync(cmd.TicketId, ct);
        if (sla is null) return;

        var policy = await _policies.FindByDepartmentAndPriorityAsync(
            cmd.NewDepartmentId, ticket.Priority, ct)
            ?? await _policies.FindGlobalByPriorityAsync(ticket.Priority, ct);

        if (policy is null) return;

        var hours = await _businessHours.FindByDepartmentAsync(cmd.NewDepartmentId, ct)
            ?? await _businessHours.FindGlobalAsync(ct);

        var now = DateTime.UtcNow;
        var elapsedMinutes = hours is not null
            ? BusinessTimeCalculator.ElapsedBusinessMinutes(sla.ClockStartedAt, now, hours)
            : (int)(now - sla.ClockStartedAt).TotalMinutes;

        elapsedMinutes -= sla.AccumulatedPauseMinutes;

        var remainingFirstResponse = Math.Max(0, policy.FirstResponseMinutes - elapsedMinutes);
        var remainingResolution = Math.Max(0, policy.ResolutionMinutes - elapsedMinutes);

        DateTime? newFirstResponseDue = hours is not null
            ? BusinessTimeCalculator.AddBusinessMinutes(now, remainingFirstResponse, hours)
            : now.AddMinutes(remainingFirstResponse);

        DateTime? newResolutionDue = hours is not null
            ? BusinessTimeCalculator.AddBusinessMinutes(now, remainingResolution, hours)
            : now.AddMinutes(remainingResolution);

        // If elapsed already exceeds target, deadline is now (already breached on arrival)
        if (remainingFirstResponse == 0) newFirstResponseDue = now;
        if (remainingResolution == 0) newResolutionDue = now;

        sla.RecalculateDeadlines(policy.Id, newFirstResponseDue, newResolutionDue);

        await _slaRepo.SaveChangesAsync(ct);
    }
}
```

- [ ] **Step 5: Invoke from TransferTicketCommandHandler**

In `src/CRM.Application/Tickets/Commands/TransferTicketCommand.cs`, after saving:
```csharp
await _mediator.Send(
    new RecalculateSlaOnTransferCommand(cmd.TicketId, cmd.TargetDepartmentId), ct);
```

- [ ] **Step 6: Run tests to verify they pass**

```bash
dotnet test tests/CRM.Application.Tests/ --filter "RecalculateSlaOnTransferCommandHandlerTests" -v n
```

Expected: 3 tests PASS.

- [ ] **Step 7: Commit**

```bash
git add src/CRM.Application/Sla/Commands/RecalculateSlaOnTransferCommand.cs \
        src/CRM.Domain/Sla/TicketSla.cs \
        src/CRM.Application/Tickets/Commands/TransferTicketCommand.cs \
        tests/CRM.Application.Tests/Sla/RecalculateSlaOnTransferCommandHandlerTests.cs
git commit -m "feat(sla): recalculate SLA deadlines on ticket transfer to new department"
```
