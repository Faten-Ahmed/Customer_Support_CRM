# Auto-Close Resolved Tickets Job — Implementation Plan

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

**Story:** US-BE-036  
**Goal:** Implement `AutoCloseResolvedTicketsJob` — a Hangfire recurring job (every 30 min) that automatically closes tickets that have been Resolved for 48+ hours with no customer reply since resolution.

**Architecture:** Job queries `ITicketRepository.FindResolvedWithNoCustomerReplyAsync(cutoffUtc)`, transitions each ticket to Closed, writes a `TicketHistory` entry (`AutoClosed`), and saves. Registered as a Hangfire recurring job in `Program.cs`.

**Tech Stack:** .NET 10, ASP.NET Core, MediatR, Hangfire, EF Core, xUnit, Moq

---

## File Structure

| Action | Path |
|--------|------|
| Create | `src/CRM.Application/Tickets/Jobs/AutoCloseResolvedTicketsJob.cs` |
| Modify | `src/CRM.Domain/Tickets/ITicketRepository.cs` |
| Modify | `src/CRM.Domain/Tickets/Ticket.cs` |
| Modify | `src/CRM.API/Program.cs` |
| Test   | `tests/CRM.Application.Tests/Tickets/AutoCloseResolvedTicketsJobTests.cs` |

---

## Task 1: AutoCloseResolvedTicketsJob

**Files:**
- Create: `src/CRM.Application/Tickets/Jobs/AutoCloseResolvedTicketsJob.cs`
- Modify: `src/CRM.Domain/Tickets/ITicketRepository.cs`
- Modify: `src/CRM.Domain/Tickets/Ticket.cs`
- Test: `tests/CRM.Application.Tests/Tickets/AutoCloseResolvedTicketsJobTests.cs`

- [ ] **Step 1: Write the failing tests**

```csharp
// tests/CRM.Application.Tests/Tickets/AutoCloseResolvedTicketsJobTests.cs
using CRM.Application.Tickets.Jobs;
using CRM.Domain.Tickets;
using CRM.Domain.Tickets.Enums;
using Moq;
using Xunit;

namespace CRM.Application.Tests.Tickets;

public class AutoCloseResolvedTicketsJobTests
{
    private readonly Mock<ITicketRepository> _repo = new();
    private readonly AutoCloseResolvedTicketsJob _job;

    public AutoCloseResolvedTicketsJobTests()
    {
        _job = new AutoCloseResolvedTicketsJob(_repo.Object);
    }

    [Fact]
    public async Task Execute_ResolvedTicketsOlderThan48h_ClosesThemAll()
    {
        var creatorId = Guid.NewGuid();
        var ticket1 = Ticket.Create(Guid.NewGuid(), "Sub1", "Desc",
            TicketPriority.Low, TicketChannel.Email, creatorId);
        var ticket2 = Ticket.Create(Guid.NewGuid(), "Sub2", "Desc",
            TicketPriority.High, TicketChannel.Portal, creatorId);

        _repo.Setup(r => r.FindResolvedWithNoCustomerReplyAsync(
            It.IsAny<DateTime>(), default))
            .ReturnsAsync(new List<Ticket> { ticket1, ticket2 });

        await _job.Execute();

        Assert.Equal(TicketStatus.Closed, ticket1.Status);
        Assert.Equal(TicketStatus.Closed, ticket2.Status);
        _repo.Verify(r => r.SaveChangesAsync(default), Times.Once);
    }

    [Fact]
    public async Task Execute_NoEligibleTickets_DoesNotCallSave()
    {
        _repo.Setup(r => r.FindResolvedWithNoCustomerReplyAsync(
            It.IsAny<DateTime>(), default))
            .ReturnsAsync(new List<Ticket>());

        await _job.Execute();

        _repo.Verify(r => r.SaveChangesAsync(default), Times.Never);
    }

    [Fact]
    public async Task Execute_ClosedTicket_HasAutoClosedHistoryEntry()
    {
        var creatorId = Guid.NewGuid();
        var ticket = Ticket.Create(Guid.NewGuid(), "Sub", "Desc",
            TicketPriority.Medium, TicketChannel.Email, creatorId);

        _repo.Setup(r => r.FindResolvedWithNoCustomerReplyAsync(
            It.IsAny<DateTime>(), default))
            .ReturnsAsync(new List<Ticket> { ticket });

        await _job.Execute();

        Assert.Contains(ticket.History, h => h.FieldChanged == "AutoClosed");
    }
}
```

- [ ] **Step 2: Run test to verify it fails**

```bash
dotnet test tests/CRM.Application.Tests/ --filter "AutoCloseResolvedTicketsJobTests" -v n
```

Expected: FAIL — `AutoCloseResolvedTicketsJob` does not exist yet.

- [ ] **Step 3: Add AutoClose to Ticket entity**

Add to `src/CRM.Domain/Tickets/Ticket.cs`:
```csharp
public void AutoClose()
{
    Status = TicketStatus.Closed;
    _history.Add(new TicketHistory(
        Id, "AutoClosed", Status.ToString(), TicketStatus.Closed.ToString(),
        Guid.Empty, DateTime.UtcNow));
    UpdatedAt = DateTime.UtcNow;
}
```

- [ ] **Step 4: Add repository method**

Add to `src/CRM.Domain/Tickets/ITicketRepository.cs`:
```csharp
Task<IReadOnlyList<Ticket>> FindResolvedWithNoCustomerReplyAsync(
    DateTime resolvedBefore, CancellationToken ct = default);
```

The infrastructure implementation should query:
`Status = Resolved AND ResolvedAt < resolvedBefore AND NOT EXISTS (TicketMessage where AuthorType = Customer AND CreatedAt > ResolvedAt)`

- [ ] **Step 5: Implement AutoCloseResolvedTicketsJob**

```csharp
// src/CRM.Application/Tickets/Jobs/AutoCloseResolvedTicketsJob.cs
using CRM.Domain.Tickets;

namespace CRM.Application.Tickets.Jobs;

public class AutoCloseResolvedTicketsJob
{
    private static readonly TimeSpan AutoCloseAfter = TimeSpan.FromHours(48);

    private readonly ITicketRepository _tickets;

    public AutoCloseResolvedTicketsJob(ITicketRepository tickets) => _tickets = tickets;

    public async Task Execute(CancellationToken ct = default)
    {
        var cutoff = DateTime.UtcNow - AutoCloseAfter;
        var eligible = await _tickets.FindResolvedWithNoCustomerReplyAsync(cutoff, ct);

        if (!eligible.Any()) return;

        foreach (var ticket in eligible)
            ticket.AutoClose();

        await _tickets.SaveChangesAsync(ct);
    }
}
```

- [ ] **Step 6: Run tests to verify they pass**

```bash
dotnet test tests/CRM.Application.Tests/ --filter "AutoCloseResolvedTicketsJobTests" -v n
```

Expected: 3 tests PASS.

- [ ] **Step 7: Register recurring job in Program.cs**

Add to `src/CRM.API/Program.cs` after `app.UseHangfireDashboard()` (or wherever Hangfire is configured):
```csharp
RecurringJob.AddOrUpdate<AutoCloseResolvedTicketsJob>(
    "auto-close-resolved-tickets",
    job => job.Execute(CancellationToken.None),
    "*/30 * * * *"); // every 30 minutes
```

Add `using CRM.Application.Tickets.Jobs;` and `using Hangfire;`.

- [ ] **Step 8: Commit**

```bash
git add src/CRM.Application/Tickets/Jobs/AutoCloseResolvedTicketsJob.cs \
        src/CRM.Domain/Tickets/ITicketRepository.cs \
        src/CRM.Domain/Tickets/Ticket.cs \
        src/CRM.API/Program.cs \
        tests/CRM.Application.Tests/Tickets/AutoCloseResolvedTicketsJobTests.cs
git commit -m "feat(tickets): add AutoCloseResolvedTicketsJob with 48-hour auto-close policy"
```
