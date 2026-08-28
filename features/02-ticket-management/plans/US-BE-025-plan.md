# Change Ticket Status — Implementation Plan

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

**Story:** US-BE-025  
**Goal:** Implement `PATCH /api/tickets/{id}/status` — transitions a ticket through the state machine (New→Assigned→InProgress→OnHold/Escalated→Resolved→Closed). Enforces valid transitions; records history.

**Architecture:** `ChangeTicketStatusCommand(ticketId, newStatus, changedByUserId)` → handler validates the transition is allowed per the state machine table, calls `ticket.ChangeStatus(newStatus, changedBy)`, saves. Invalid transitions return 400.

**State machine allowed transitions:**
- New → Assigned (auto on assignment, or manual)
- Assigned → InProgress
- InProgress → OnHold, Escalated, Resolved
- OnHold → InProgress
- Escalated → InProgress, Resolved
- Resolved → Reopened, Closed
- Reopened → Assigned, InProgress
- Closed → (no transitions)

**Tech Stack:** .NET 10, ASP.NET Core, MediatR, EF Core, xUnit, Moq

---

## File Structure

| Action | Path |
|--------|------|
| Create | `src/CRM.Application/Tickets/Commands/ChangeTicketStatusCommand.cs` |
| Create | `src/CRM.Application/Tickets/Services/TicketStateMachine.cs` |
| Modify | `src/CRM.API/Controllers/TicketsController.cs` |
| Test   | `tests/CRM.Application.Tests/Tickets/ChangeTicketStatusCommandHandlerTests.cs` |
| Test   | `tests/CRM.Application.Tests/Tickets/TicketStateMachineTests.cs` |
| Test   | `tests/CRM.API.Tests/Tickets/TicketsControllerStatusTests.cs` |

---

## Task 1: TicketStateMachine

**Files:**
- Create: `src/CRM.Application/Tickets/Services/TicketStateMachine.cs`
- Test: `tests/CRM.Application.Tests/Tickets/TicketStateMachineTests.cs`

- [ ] **Step 1: Write the failing tests**

```csharp
// tests/CRM.Application.Tests/Tickets/TicketStateMachineTests.cs
using CRM.Application.Tickets.Services;
using CRM.Domain.Tickets.Enums;
using Xunit;

namespace CRM.Application.Tests.Tickets;

public class TicketStateMachineTests
{
    [Theory]
    [InlineData(TicketStatus.New, TicketStatus.Assigned, true)]
    [InlineData(TicketStatus.Assigned, TicketStatus.InProgress, true)]
    [InlineData(TicketStatus.InProgress, TicketStatus.OnHold, true)]
    [InlineData(TicketStatus.InProgress, TicketStatus.Resolved, true)]
    [InlineData(TicketStatus.InProgress, TicketStatus.Escalated, true)]
    [InlineData(TicketStatus.OnHold, TicketStatus.InProgress, true)]
    [InlineData(TicketStatus.Escalated, TicketStatus.InProgress, true)]
    [InlineData(TicketStatus.Escalated, TicketStatus.Resolved, true)]
    [InlineData(TicketStatus.Resolved, TicketStatus.Reopened, true)]
    [InlineData(TicketStatus.Resolved, TicketStatus.Closed, true)]
    [InlineData(TicketStatus.Reopened, TicketStatus.InProgress, true)]
    [InlineData(TicketStatus.Closed, TicketStatus.Reopened, false)]
    [InlineData(TicketStatus.New, TicketStatus.Resolved, false)]
    [InlineData(TicketStatus.Assigned, TicketStatus.Closed, false)]
    public void IsValidTransition_ReturnsExpectedResult(
        TicketStatus from, TicketStatus to, bool expected)
    {
        Assert.Equal(expected, TicketStateMachine.IsValidTransition(from, to));
    }
}
```

- [ ] **Step 2: Run test to verify it fails**

```bash
dotnet test tests/CRM.Application.Tests/ --filter "TicketStateMachineTests" -v n
```

Expected: FAIL — `TicketStateMachine` does not exist yet.

- [ ] **Step 3: Implement TicketStateMachine**

```csharp
// src/CRM.Application/Tickets/Services/TicketStateMachine.cs
using CRM.Domain.Tickets.Enums;

namespace CRM.Application.Tickets.Services;

public static class TicketStateMachine
{
    private static readonly Dictionary<TicketStatus, HashSet<TicketStatus>> _allowedTransitions
        = new()
    {
        [TicketStatus.New]       = new() { TicketStatus.Assigned },
        [TicketStatus.Assigned]  = new() { TicketStatus.InProgress },
        [TicketStatus.InProgress]= new() { TicketStatus.OnHold, TicketStatus.Escalated, TicketStatus.Resolved },
        [TicketStatus.OnHold]    = new() { TicketStatus.InProgress },
        [TicketStatus.Escalated] = new() { TicketStatus.InProgress, TicketStatus.Resolved },
        [TicketStatus.Resolved]  = new() { TicketStatus.Reopened, TicketStatus.Closed },
        [TicketStatus.Reopened]  = new() { TicketStatus.Assigned, TicketStatus.InProgress },
        [TicketStatus.Closed]    = new()
    };

    public static bool IsValidTransition(TicketStatus from, TicketStatus to)
        => _allowedTransitions.TryGetValue(from, out var allowed) && allowed.Contains(to);
}
```

- [ ] **Step 4: Run tests to verify they pass**

```bash
dotnet test tests/CRM.Application.Tests/ --filter "TicketStateMachineTests" -v n
```

Expected: 14 tests PASS.

- [ ] **Step 5: Commit**

```bash
git add src/CRM.Application/Tickets/Services/TicketStateMachine.cs \
        tests/CRM.Application.Tests/Tickets/TicketStateMachineTests.cs
git commit -m "feat(tickets): add TicketStateMachine with allowed transition table"
```

---

## Task 2: ChangeTicketStatus Command + Handler

**Files:**
- Create: `src/CRM.Application/Tickets/Commands/ChangeTicketStatusCommand.cs`
- Test: `tests/CRM.Application.Tests/Tickets/ChangeTicketStatusCommandHandlerTests.cs`

- [ ] **Step 1: Write the failing tests**

```csharp
// tests/CRM.Application.Tests/Tickets/ChangeTicketStatusCommandHandlerTests.cs
using CRM.Application.Tickets.Commands;
using CRM.Domain.Tickets;
using CRM.Domain.Tickets.Enums;
using Moq;
using Xunit;

namespace CRM.Application.Tests.Tickets;

public class ChangeTicketStatusCommandHandlerTests
{
    private readonly Mock<ITicketRepository> _repo = new();
    private readonly ChangeTicketStatusCommandHandler _handler;

    public ChangeTicketStatusCommandHandlerTests()
    {
        _handler = new ChangeTicketStatusCommandHandler(_repo.Object);
    }

    [Fact]
    public async Task Handle_ValidTransition_ChangesStatus()
    {
        var id = Guid.NewGuid();
        var ticket = Ticket.Create(Guid.NewGuid(), "S", "D",
            TicketPriority.Medium, TicketChannel.Internal, Guid.NewGuid());
        ticket.Assign(Guid.NewGuid(), Guid.NewGuid()); // → Assigned

        _repo.Setup(r => r.FindByIdDetailedAsync(id, default)).ReturnsAsync(ticket);

        await _handler.Handle(
            new ChangeTicketStatusCommand(id, TicketStatus.InProgress, Guid.NewGuid()), default);

        Assert.Equal(TicketStatus.InProgress, ticket.Status);
        _repo.Verify(r => r.SaveChangesAsync(default), Times.Once);
    }

    [Fact]
    public async Task Handle_InvalidTransition_ThrowsInvalidOperationException()
    {
        var id = Guid.NewGuid();
        var ticket = Ticket.Create(Guid.NewGuid(), "S", "D",
            TicketPriority.Low, TicketChannel.Internal, Guid.NewGuid());
        // Still New — can't jump to Resolved

        _repo.Setup(r => r.FindByIdDetailedAsync(id, default)).ReturnsAsync(ticket);

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            _handler.Handle(
                new ChangeTicketStatusCommand(id, TicketStatus.Resolved, Guid.NewGuid()),
                default));
    }

    [Fact]
    public async Task Handle_NonExistentTicket_ThrowsKeyNotFoundException()
    {
        _repo.Setup(r => r.FindByIdDetailedAsync(It.IsAny<Guid>(), default))
             .ReturnsAsync((Ticket?)null);

        await Assert.ThrowsAsync<KeyNotFoundException>(() =>
            _handler.Handle(
                new ChangeTicketStatusCommand(Guid.NewGuid(), TicketStatus.InProgress, Guid.NewGuid()),
                default));
    }
}
```

- [ ] **Step 2: Run test to verify it fails**

```bash
dotnet test tests/CRM.Application.Tests/ --filter "ChangeTicketStatusCommandHandlerTests" -v n
```

Expected: FAIL — `ChangeTicketStatusCommand` does not exist yet.

- [ ] **Step 3: Implement command and handler**

```csharp
// src/CRM.Application/Tickets/Commands/ChangeTicketStatusCommand.cs
using CRM.Application.Tickets.Services;
using CRM.Domain.Tickets;
using CRM.Domain.Tickets.Enums;
using MediatR;

namespace CRM.Application.Tickets.Commands;

public record ChangeTicketStatusCommand(
    Guid TicketId,
    TicketStatus NewStatus,
    Guid ChangedByUserId) : IRequest;

public class ChangeTicketStatusCommandHandler : IRequestHandler<ChangeTicketStatusCommand>
{
    private readonly ITicketRepository _tickets;

    public ChangeTicketStatusCommandHandler(ITicketRepository tickets) => _tickets = tickets;

    public async Task Handle(ChangeTicketStatusCommand cmd, CancellationToken ct)
    {
        var ticket = await _tickets.FindByIdDetailedAsync(cmd.TicketId, ct)
            ?? throw new KeyNotFoundException($"Ticket {cmd.TicketId} not found.");

        if (!TicketStateMachine.IsValidTransition(ticket.Status, cmd.NewStatus))
            throw new InvalidOperationException(
                $"Cannot transition from {ticket.Status} to {cmd.NewStatus}.");

        ticket.ChangeStatus(cmd.NewStatus, cmd.ChangedByUserId);
        await _tickets.SaveChangesAsync(ct);
    }
}
```

- [ ] **Step 4: Run tests to verify they pass**

```bash
dotnet test tests/CRM.Application.Tests/ --filter "ChangeTicketStatusCommandHandlerTests" -v n
```

Expected: 3 tests PASS.

- [ ] **Step 5: Commit**

```bash
git add src/CRM.Application/Tickets/Commands/ChangeTicketStatusCommand.cs \
        tests/CRM.Application.Tests/Tickets/ChangeTicketStatusCommandHandlerTests.cs
git commit -m "feat(tickets): add ChangeTicketStatusCommand with state machine validation"
```

---

## Task 3: TicketsController — PATCH /api/tickets/{id}/status

**Files:**
- Modify: `src/CRM.API/Controllers/TicketsController.cs`
- Test: `tests/CRM.API.Tests/Tickets/TicketsControllerStatusTests.cs`

- [ ] **Step 1: Write the failing test**

```csharp
// tests/CRM.API.Tests/Tickets/TicketsControllerStatusTests.cs
using System.Net;
using System.Net.Http.Json;
using CRM.Domain.Tickets.Enums;
using MediatR;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using Xunit;

namespace CRM.API.Tests.Tickets;

public class TicketsControllerStatusTests
{
    private readonly Mock<IMediator> _mediator = new();

    private HttpClient BuildClient()
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
                "Bearer", TestJwtHelper.CreateTestToken(role: "Agent"));
        return client;
    }

    [Fact]
    public async Task ChangeStatus_ValidTransition_Returns204()
    {
        _mediator.Setup(m => m.Send(It.IsAny<IRequest>(), default)).Returns(Task.CompletedTask);
        var client = BuildClient();

        var response = await client.PatchAsJsonAsync(
            $"/api/tickets/{Guid.NewGuid()}/status",
            new { status = "InProgress" });

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
    }

    [Fact]
    public async Task ChangeStatus_InvalidTransition_Returns400()
    {
        _mediator.Setup(m => m.Send(It.IsAny<IRequest>(), default))
                 .ThrowsAsync(new InvalidOperationException("Invalid transition."));
        var client = BuildClient();

        var response = await client.PatchAsJsonAsync(
            $"/api/tickets/{Guid.NewGuid()}/status",
            new { status = "Resolved" });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }
}
```

- [ ] **Step 2: Run test to verify it fails**

```bash
dotnet test tests/CRM.API.Tests/ --filter "TicketsControllerStatusTests" -v n
```

Expected: FAIL — endpoint does not exist.

- [ ] **Step 3: Add ChangeStatus endpoint to TicketsController**

```csharp
// Add to src/CRM.API/Controllers/TicketsController.cs inside the class:

public record ChangeStatusRequest(TicketStatus Status);

[HttpPatch("{id:guid}/status")]
public async Task<IActionResult> ChangeStatus(
    Guid id, [FromBody] ChangeStatusRequest request, CancellationToken ct)
{
    try
    {
        await _mediator.Send(
            new ChangeTicketStatusCommand(id, request.Status, CurrentUserId), ct);
        return NoContent();
    }
    catch (KeyNotFoundException ex) { return NotFound(new { error = ex.Message }); }
    catch (InvalidOperationException ex) { return BadRequest(new { error = ex.Message }); }
}
```

- [ ] **Step 4: Run tests to verify they pass**

```bash
dotnet test tests/CRM.API.Tests/ --filter "TicketsControllerStatusTests" -v n
```

Expected: 2 tests PASS.

- [ ] **Step 5: Commit**

```bash
git add src/CRM.API/Controllers/TicketsController.cs \
        tests/CRM.API.Tests/Tickets/TicketsControllerStatusTests.cs
git commit -m "feat(api): add PATCH /api/tickets/{id}/status with state machine enforcement"
```
