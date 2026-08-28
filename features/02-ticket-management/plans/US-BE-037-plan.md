# Reopen Ticket on Customer Reply — Implementation Plan

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

**Story:** US-BE-037  
**Goal:** When a portal customer posts a message to a Resolved ticket, automatically reopen it to Reopened status, write a history entry, and re-trigger the auto-assign job if the previous agent is no longer available.

**Architecture:** `ReopenTicketCommand(TicketId, ReopenedByUserId)` → handler validates ticket is Resolved (not Closed), calls `ticket.ChangeStatus(Reopened)` via `TicketStateMachine`, writes history, enqueues `AutoAssignTicketJob` if the current assigned agent is inactive. The portal message controller invokes this command before persisting the customer's message when `ticket.Status == Resolved`.

**Tech Stack:** .NET 10, ASP.NET Core, MediatR, Hangfire, EF Core, xUnit, Moq

---

## File Structure

| Action | Path |
|--------|------|
| Create | `src/CRM.Application/Tickets/Commands/ReopenTicketCommand.cs` |
| Modify | `src/CRM.Domain/Users/IUserRepository.cs` |
| Test   | `tests/CRM.Application.Tests/Tickets/ReopenTicketCommandHandlerTests.cs` |

---

## Task 1: ReopenTicket Command + Handler

**Files:**
- Create: `src/CRM.Application/Tickets/Commands/ReopenTicketCommand.cs`
- Modify: `src/CRM.Domain/Users/IUserRepository.cs`
- Test: `tests/CRM.Application.Tests/Tickets/ReopenTicketCommandHandlerTests.cs`

- [ ] **Step 1: Write the failing tests**

```csharp
// tests/CRM.Application.Tests/Tickets/ReopenTicketCommandHandlerTests.cs
using CRM.Application.Tickets.Commands;
using CRM.Domain.Tickets;
using CRM.Domain.Tickets.Enums;
using CRM.Domain.Users;
using Hangfire;
using Moq;
using Xunit;

namespace CRM.Application.Tests.Tickets;

public class ReopenTicketCommandHandlerTests
{
    private readonly Mock<ITicketRepository> _ticketRepo = new();
    private readonly Mock<IUserRepository> _userRepo = new();
    private readonly Mock<IBackgroundJobClient> _jobs = new();
    private readonly ReopenTicketCommandHandler _handler;

    public ReopenTicketCommandHandlerTests()
    {
        _handler = new ReopenTicketCommandHandler(
            _ticketRepo.Object, _userRepo.Object, _jobs.Object);
    }

    private Ticket MakeResolvedTicket(Guid? assignedTo = null)
    {
        var ticket = Ticket.Create(Guid.NewGuid(), "Subject", "Desc",
            TicketPriority.Medium, TicketChannel.Portal, Guid.NewGuid());
        // Simulate resolved state by transitioning through valid states
        ticket.ChangeStatus(TicketStatus.Assigned, Guid.NewGuid());
        ticket.ChangeStatus(TicketStatus.InProgress, Guid.NewGuid());
        ticket.ChangeStatus(TicketStatus.Resolved, Guid.NewGuid());
        if (assignedTo.HasValue)
            ticket.Assign(assignedTo.Value, Guid.NewGuid());
        return ticket;
    }

    [Fact]
    public async Task Handle_ResolvedTicket_TransitionsToReopened()
    {
        var customerId = Guid.NewGuid();
        var ticket = MakeResolvedTicket();
        _ticketRepo.Setup(r => r.FindByIdAsync(ticket.Id, default)).ReturnsAsync(ticket);

        await _handler.Handle(new ReopenTicketCommand(ticket.Id, customerId), default);

        Assert.Equal(TicketStatus.Reopened, ticket.Status);
        _ticketRepo.Verify(r => r.SaveChangesAsync(default), Times.Once);
    }

    [Fact]
    public async Task Handle_ClosedTicket_ThrowsInvalidOperationException()
    {
        var ticket = Ticket.Create(Guid.NewGuid(), "S", "D",
            TicketPriority.Low, TicketChannel.Portal, Guid.NewGuid());
        ticket.ChangeStatus(TicketStatus.Assigned, Guid.NewGuid());
        ticket.ChangeStatus(TicketStatus.InProgress, Guid.NewGuid());
        ticket.ChangeStatus(TicketStatus.Resolved, Guid.NewGuid());
        ticket.ChangeStatus(TicketStatus.Closed, Guid.NewGuid());

        _ticketRepo.Setup(r => r.FindByIdAsync(ticket.Id, default)).ReturnsAsync(ticket);

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            _handler.Handle(new ReopenTicketCommand(ticket.Id, Guid.NewGuid()), default));
    }

    [Fact]
    public async Task Handle_AssignedAgentInactive_EnqueuesAutoAssignJob()
    {
        var agentId = Guid.NewGuid();
        var ticket = MakeResolvedTicket(assignedTo: agentId);
        _ticketRepo.Setup(r => r.FindByIdAsync(ticket.Id, default)).ReturnsAsync(ticket);
        _userRepo.Setup(r => r.IsActiveAgentAsync(agentId, default)).ReturnsAsync(false);

        await _handler.Handle(new ReopenTicketCommand(ticket.Id, Guid.NewGuid()), default);

        _jobs.Verify(j => j.Enqueue(It.IsAny<System.Linq.Expressions.Expression<Func<AutoAssignTicketJob, Task>>>()), Times.Once);
    }

    [Fact]
    public async Task Handle_AssignedAgentStillActive_DoesNotReenqueue()
    {
        var agentId = Guid.NewGuid();
        var ticket = MakeResolvedTicket(assignedTo: agentId);
        _ticketRepo.Setup(r => r.FindByIdAsync(ticket.Id, default)).ReturnsAsync(ticket);
        _userRepo.Setup(r => r.IsActiveAgentAsync(agentId, default)).ReturnsAsync(true);

        await _handler.Handle(new ReopenTicketCommand(ticket.Id, Guid.NewGuid()), default);

        _jobs.Verify(j => j.Enqueue(It.IsAny<System.Linq.Expressions.Expression<Func<AutoAssignTicketJob, Task>>>()), Times.Never);
    }
}
```

- [ ] **Step 2: Run test to verify it fails**

```bash
dotnet test tests/CRM.Application.Tests/ --filter "ReopenTicketCommandHandlerTests" -v n
```

Expected: FAIL — `ReopenTicketCommand` does not exist yet.

- [ ] **Step 3: Add IsActiveAgentAsync to IUserRepository**

Add to `src/CRM.Domain/Users/IUserRepository.cs`:
```csharp
Task<bool> IsActiveAgentAsync(Guid agentId, CancellationToken ct = default);
```

- [ ] **Step 4: Implement command and handler**

```csharp
// src/CRM.Application/Tickets/Commands/ReopenTicketCommand.cs
using CRM.Application.Tickets.Jobs;
using CRM.Domain.Tickets;
using CRM.Domain.Tickets.Enums;
using CRM.Domain.Users;
using Hangfire;
using MediatR;

namespace CRM.Application.Tickets.Commands;

public record ReopenTicketCommand(Guid TicketId, Guid ReopenedByUserId) : IRequest;

public class ReopenTicketCommandHandler : IRequestHandler<ReopenTicketCommand>
{
    private readonly ITicketRepository _tickets;
    private readonly IUserRepository _users;
    private readonly IBackgroundJobClient _jobs;

    public ReopenTicketCommandHandler(
        ITicketRepository tickets,
        IUserRepository users,
        IBackgroundJobClient jobs)
    {
        _tickets = tickets;
        _users = users;
        _jobs = jobs;
    }

    public async Task Handle(ReopenTicketCommand cmd, CancellationToken ct)
    {
        var ticket = await _tickets.FindByIdAsync(cmd.TicketId, ct)
            ?? throw new KeyNotFoundException($"Ticket {cmd.TicketId} not found.");

        if (ticket.Status == TicketStatus.Closed)
            throw new InvalidOperationException("Cannot reopen a Closed ticket. Customers may not reply to closed tickets.");

        if (ticket.Status != TicketStatus.Resolved)
            throw new InvalidOperationException($"Ticket is not in Resolved status; current status: {ticket.Status}.");

        ticket.ChangeStatus(TicketStatus.Reopened, cmd.ReopenedByUserId);

        bool needsReassignment = ticket.AssignedToUserId.HasValue
            && !await _users.IsActiveAgentAsync(ticket.AssignedToUserId.Value, ct);

        if (needsReassignment)
            _jobs.Enqueue<AutoAssignTicketJob>(j => j.Execute(ticket.Id, CancellationToken.None));

        await _tickets.SaveChangesAsync(ct);
    }
}
```

- [ ] **Step 5: Invoke ReopenTicketCommand from portal message endpoint**

In the portal tickets controller (or `AddPortalTicketMessageCommandHandler`), before adding the message check the ticket status:

```csharp
// In PortalTicketsController.AddMessage or AddPortalTicketMessageCommandHandler:
if (ticket.Status == TicketStatus.Resolved)
    await _mediator.Send(new ReopenTicketCommand(ticketId, currentCustomerId), ct);
```

- [ ] **Step 6: Run tests to verify they pass**

```bash
dotnet test tests/CRM.Application.Tests/ --filter "ReopenTicketCommandHandlerTests" -v n
```

Expected: 4 tests PASS.

- [ ] **Step 7: Commit**

```bash
git add src/CRM.Application/Tickets/Commands/ReopenTicketCommand.cs \
        src/CRM.Domain/Users/IUserRepository.cs \
        tests/CRM.Application.Tests/Tickets/ReopenTicketCommandHandlerTests.cs
git commit -m "feat(tickets): add ReopenTicketCommand triggered by customer reply on Resolved ticket"
```
