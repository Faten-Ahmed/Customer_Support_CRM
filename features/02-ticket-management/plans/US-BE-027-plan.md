# Escalate Ticket — Implementation Plan

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

**Story:** US-BE-027  
**Goal:** Implement `PATCH /api/tickets/{id}/escalate` — escalates a ticket (status → Escalated), records reason in history, and notifies the manager of the ticket's department.

**Architecture:** `EscalateTicketCommand(ticketId, reason, escalatedByUserId)` → handler validates current status allows escalation (InProgress only), calls `ticket.ChangeStatus(Escalated, changedBy)`, records reason in history, saves. A notification event is dispatched to the department manager (notification in US-BE-053).

**Tech Stack:** .NET 10, ASP.NET Core, MediatR, EF Core, xUnit, Moq

---

## File Structure

| Action | Path |
|--------|------|
| Create | `src/CRM.Application/Tickets/Commands/EscalateTicketCommand.cs` |
| Modify | `src/CRM.API/Controllers/TicketsController.cs` |
| Test   | `tests/CRM.Application.Tests/Tickets/EscalateTicketCommandHandlerTests.cs` |
| Test   | `tests/CRM.API.Tests/Tickets/TicketsControllerEscalateTests.cs` |

---

## Task 1: EscalateTicket Command + Handler

**Files:**
- Create: `src/CRM.Application/Tickets/Commands/EscalateTicketCommand.cs`
- Test: `tests/CRM.Application.Tests/Tickets/EscalateTicketCommandHandlerTests.cs`

- [ ] **Step 1: Write the failing tests**

```csharp
// tests/CRM.Application.Tests/Tickets/EscalateTicketCommandHandlerTests.cs
using CRM.Application.Tickets.Commands;
using CRM.Domain.Tickets;
using CRM.Domain.Tickets.Enums;
using Moq;
using Xunit;

namespace CRM.Application.Tests.Tickets;

public class EscalateTicketCommandHandlerTests
{
    private readonly Mock<ITicketRepository> _repo = new();
    private readonly EscalateTicketCommandHandler _handler;

    public EscalateTicketCommandHandlerTests()
    {
        _handler = new EscalateTicketCommandHandler(_repo.Object);
    }

    private static Ticket MakeTicket(TicketStatus status = TicketStatus.InProgress)
    {
        var ticket = Ticket.Create(Guid.NewGuid(), "S", "D",
            TicketPriority.High, TicketChannel.Internal, Guid.NewGuid());
        if (status == TicketStatus.InProgress)
        {
            ticket.Assign(Guid.NewGuid(), Guid.NewGuid());
            ticket.ChangeStatus(TicketStatus.InProgress, Guid.NewGuid());
        }
        return ticket;
    }

    [Fact]
    public async Task Handle_InProgressTicket_EscalatesAndRecordsReason()
    {
        var id = Guid.NewGuid();
        var ticket = MakeTicket(TicketStatus.InProgress);
        _repo.Setup(r => r.FindByIdDetailedAsync(id, default)).ReturnsAsync(ticket);

        await _handler.Handle(
            new EscalateTicketCommand(id, "SLA about to breach", Guid.NewGuid()), default);

        Assert.Equal(TicketStatus.Escalated, ticket.Status);
        Assert.Contains(ticket.History, h => h.FieldChanged == "EscalationReason");
        _repo.Verify(r => r.SaveChangesAsync(default), Times.Once);
    }

    [Fact]
    public async Task Handle_NewTicket_ThrowsInvalidOperationException()
    {
        var id = Guid.NewGuid();
        var ticket = Ticket.Create(Guid.NewGuid(), "S", "D",
            TicketPriority.Low, TicketChannel.Internal, Guid.NewGuid());

        _repo.Setup(r => r.FindByIdDetailedAsync(id, default)).ReturnsAsync(ticket);

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            _handler.Handle(
                new EscalateTicketCommand(id, "reason", Guid.NewGuid()), default));
    }

    [Fact]
    public async Task Handle_NonExistentTicket_ThrowsKeyNotFoundException()
    {
        _repo.Setup(r => r.FindByIdDetailedAsync(It.IsAny<Guid>(), default))
             .ReturnsAsync((Ticket?)null);

        await Assert.ThrowsAsync<KeyNotFoundException>(() =>
            _handler.Handle(
                new EscalateTicketCommand(Guid.NewGuid(), "r", Guid.NewGuid()), default));
    }
}
```

- [ ] **Step 2: Run test to verify it fails**

```bash
dotnet test tests/CRM.Application.Tests/ --filter "EscalateTicketCommandHandlerTests" -v n
```

Expected: FAIL — `EscalateTicketCommand` does not exist yet.

- [ ] **Step 3: Implement command and handler**

```csharp
// src/CRM.Application/Tickets/Commands/EscalateTicketCommand.cs
using CRM.Application.Tickets.Services;
using CRM.Domain.Tickets;
using CRM.Domain.Tickets.Enums;
using MediatR;

namespace CRM.Application.Tickets.Commands;

public record EscalateTicketCommand(
    Guid TicketId,
    string Reason,
    Guid EscalatedByUserId) : IRequest;

public class EscalateTicketCommandHandler : IRequestHandler<EscalateTicketCommand>
{
    private readonly ITicketRepository _tickets;

    public EscalateTicketCommandHandler(ITicketRepository tickets) => _tickets = tickets;

    public async Task Handle(EscalateTicketCommand cmd, CancellationToken ct)
    {
        var ticket = await _tickets.FindByIdDetailedAsync(cmd.TicketId, ct)
            ?? throw new KeyNotFoundException($"Ticket {cmd.TicketId} not found.");

        if (!TicketStateMachine.IsValidTransition(ticket.Status, TicketStatus.Escalated))
            throw new InvalidOperationException(
                $"Cannot escalate a ticket in {ticket.Status} status. Only InProgress tickets can be escalated.");

        ticket.ChangeStatus(TicketStatus.Escalated, cmd.EscalatedByUserId);
        ticket.RecordEscalationReason(cmd.Reason, cmd.EscalatedByUserId);
        await _tickets.SaveChangesAsync(ct);
    }
}
```

Add `RecordEscalationReason` to `Ticket.cs`:

```csharp
// Add to Ticket class in src/CRM.Domain/Tickets/Ticket.cs:
public void RecordEscalationReason(string reason, Guid changedBy)
    => _history.Add(TicketHistory.Create(Id, "EscalationReason", null, reason, changedBy));
```

- [ ] **Step 4: Run tests to verify they pass**

```bash
dotnet test tests/CRM.Application.Tests/ --filter "EscalateTicketCommandHandlerTests" -v n
```

Expected: 3 tests PASS.

- [ ] **Step 5: Commit**

```bash
git add src/CRM.Application/Tickets/Commands/EscalateTicketCommand.cs \
        src/CRM.Domain/Tickets/Ticket.cs \
        tests/CRM.Application.Tests/Tickets/EscalateTicketCommandHandlerTests.cs
git commit -m "feat(tickets): add EscalateTicketCommand with reason recording"
```

---

## Task 2: TicketsController — PATCH /api/tickets/{id}/escalate

**Files:**
- Modify: `src/CRM.API/Controllers/TicketsController.cs`
- Test: `tests/CRM.API.Tests/Tickets/TicketsControllerEscalateTests.cs`

- [ ] **Step 1: Write the failing test**

```csharp
// tests/CRM.API.Tests/Tickets/TicketsControllerEscalateTests.cs
using System.Net;
using System.Net.Http.Json;
using MediatR;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using Xunit;

namespace CRM.API.Tests.Tickets;

public class TicketsControllerEscalateTests
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
    public async Task Escalate_ValidRequest_Returns204()
    {
        _mediator.Setup(m => m.Send(It.IsAny<IRequest>(), default)).Returns(Task.CompletedTask);
        var client = BuildClient();

        var response = await client.PatchAsJsonAsync(
            $"/api/tickets/{Guid.NewGuid()}/escalate",
            new { reason = "SLA about to breach" });

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
    }

    [Fact]
    public async Task Escalate_InvalidStatus_Returns400()
    {
        _mediator.Setup(m => m.Send(It.IsAny<IRequest>(), default))
                 .ThrowsAsync(new InvalidOperationException("Cannot escalate."));
        var client = BuildClient();

        var response = await client.PatchAsJsonAsync(
            $"/api/tickets/{Guid.NewGuid()}/escalate",
            new { reason = "r" });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }
}
```

- [ ] **Step 2: Run test to verify it fails**

```bash
dotnet test tests/CRM.API.Tests/ --filter "TicketsControllerEscalateTests" -v n
```

Expected: FAIL — endpoint does not exist.

- [ ] **Step 3: Add Escalate endpoint to TicketsController**

```csharp
// Add to src/CRM.API/Controllers/TicketsController.cs inside the class:

public record EscalateTicketRequest(string Reason);

[HttpPatch("{id:guid}/escalate")]
public async Task<IActionResult> Escalate(
    Guid id, [FromBody] EscalateTicketRequest request, CancellationToken ct)
{
    try
    {
        await _mediator.Send(
            new EscalateTicketCommand(id, request.Reason, CurrentUserId), ct);
        return NoContent();
    }
    catch (KeyNotFoundException ex) { return NotFound(new { error = ex.Message }); }
    catch (InvalidOperationException ex) { return BadRequest(new { error = ex.Message }); }
}
```

- [ ] **Step 4: Run tests to verify they pass**

```bash
dotnet test tests/CRM.API.Tests/ --filter "TicketsControllerEscalateTests" -v n
```

Expected: 2 tests PASS.

- [ ] **Step 5: Commit**

```bash
git add src/CRM.API/Controllers/TicketsController.cs \
        tests/CRM.API.Tests/Tickets/TicketsControllerEscalateTests.cs
git commit -m "feat(api): add PATCH /api/tickets/{id}/escalate endpoint"
```
