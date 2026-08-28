# Portal Close Ticket — Implementation Plan

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

**Story:** US-BE-081  
**Goal:** Implement `POST /api/portal/tickets/{id}/close` — customer closes their own ticket. Any open status → `Closed`. Returns 422 `TICKET_ALREADY_CLOSED` if already closed. Returns 403 if ticket belongs to another customer. Publishes `TicketClosed` domain event (triggers CSAT survey in US-BE-092). Writes `TicketHistory` entry with `ClosedBy = Customer`.

**Architecture:** `ClosePortalTicketCommand(TicketId, CustomerId)` → ownership check → `ticket.Close(closedBy: "Customer")` → publishes `TicketClosedEvent` via MediatR → `SaveChangesAsync`. History entry written by `Ticket.Close()` method.

**Tech Stack:** .NET 10, ASP.NET Core, MediatR, EF Core, xUnit, Moq

---

## File Structure

| Action | Path |
|--------|------|
| Create | `src/CRM.Application/Portal/Commands/ClosePortalTicketCommand.cs` |
| Modify | `src/CRM.API/Controllers/PortalController.cs` |
| Test   | `tests/CRM.Application.Tests/Portal/ClosePortalTicketCommandHandlerTests.cs` |

---

## Task 1: Portal Close Ticket

> Note: `Ticket` entity and `ITicketRepository` are from US-BE-019. `PortalController` is from US-BE-080. `TicketHistory` entity is from US-BE-020. Implement those plans first.

- [ ] **Step 1: Write the failing tests**

```csharp
// tests/CRM.Application.Tests/Portal/ClosePortalTicketCommandHandlerTests.cs
using CRM.Application.Portal.Commands;
using CRM.Domain.Tickets;
using FluentValidation;
using MediatR;
using Moq;
using Xunit;

namespace CRM.Application.Tests.Portal;

public class ClosePortalTicketCommandHandlerTests
{
    private readonly Mock<ITicketRepository> _tickets = new();
    private readonly Mock<IPublisher> _publisher = new();
    private readonly ClosePortalTicketCommandHandler _handler;

    public ClosePortalTicketCommandHandlerTests()
    {
        _handler = new ClosePortalTicketCommandHandler(_tickets.Object, _publisher.Object);
    }

    [Fact]
    public async Task Handle_OpenTicket_ClosesAndPublishesEvent()
    {
        var customerId = Guid.NewGuid();
        var ticket = Ticket.Create("Test", customerId, Guid.NewGuid(), "Email");
        _tickets.Setup(r => r.FindByIdAsync(ticket.Id, default)).ReturnsAsync(ticket);

        var result = await _handler.Handle(
            new ClosePortalTicketCommand(ticket.Id, customerId), default);

        Assert.Equal("Closed", result.Status);
        _publisher.Verify(p => p.Publish(
            It.Is<TicketClosedEvent>(e => e.TicketId == ticket.Id),
            default), Times.Once);
        _tickets.Verify(r => r.SaveChangesAsync(default), Times.Once);
    }

    [Fact]
    public async Task Handle_AlreadyClosed_ThrowsValidationException()
    {
        var customerId = Guid.NewGuid();
        var ticket = Ticket.Create("Test", customerId, Guid.NewGuid(), "Email");
        ticket.Close("Customer");
        _tickets.Setup(r => r.FindByIdAsync(ticket.Id, default)).ReturnsAsync(ticket);

        var ex = await Assert.ThrowsAsync<ValidationException>(() =>
            _handler.Handle(
                new ClosePortalTicketCommand(ticket.Id, customerId), default));

        Assert.Contains("TICKET_ALREADY_CLOSED", ex.Errors.First().ErrorCode);
    }

    [Fact]
    public async Task Handle_OtherCustomerTicket_ThrowsUnauthorizedAccessException()
    {
        var customerId = Guid.NewGuid();
        var otherCustomerId = Guid.NewGuid();
        var ticket = Ticket.Create("Test", otherCustomerId, Guid.NewGuid(), "Email");
        _tickets.Setup(r => r.FindByIdAsync(ticket.Id, default)).ReturnsAsync(ticket);

        await Assert.ThrowsAsync<UnauthorizedAccessException>(() =>
            _handler.Handle(
                new ClosePortalTicketCommand(ticket.Id, customerId), default));
    }

    [Fact]
    public async Task Handle_HistoryEntryWritten()
    {
        var customerId = Guid.NewGuid();
        var ticket = Ticket.Create("Test", customerId, Guid.NewGuid(), "Email");
        _tickets.Setup(r => r.FindByIdAsync(ticket.Id, default)).ReturnsAsync(ticket);

        await _handler.Handle(
            new ClosePortalTicketCommand(ticket.Id, customerId), default);

        Assert.Contains(ticket.History, h => h.ClosedBy == "Customer");
    }
}
```

- [ ] **Step 2: Run tests to verify they fail**

```bash
dotnet test tests/CRM.Application.Tests/ --filter "ClosePortalTicketCommandHandlerTests" -v n
```

Expected: FAIL — `ClosePortalTicketCommand` does not exist yet.

- [ ] **Step 3: Add Close method to Ticket entity and TicketClosedEvent**

Open `src/CRM.Domain/Tickets/Ticket.cs` and add if not present:

```csharp
// Add TicketClosedEvent to src/CRM.Domain/Tickets/Events/TicketClosedEvent.cs
using MediatR;
namespace CRM.Domain.Tickets.Events;
public record TicketClosedEvent(Guid TicketId, Guid CustomerId, Guid DepartmentId) : INotification;
```

In the `Ticket` entity, add:

```csharp
public void Close(string closedBy)
{
    if (Status == "Closed")
        return; // caller should check before calling

    Status = "Closed";
    ClosedAt = DateTime.UtcNow;
    History.Add(new TicketHistory(Id, $"Ticket closed by {closedBy}", DateTime.UtcNow)
    {
        ClosedBy = closedBy
    });
}
```

And add `ClosedBy` property to `TicketHistory`:

```csharp
public string? ClosedBy { get; set; }
```

- [ ] **Step 4: Implement ClosePortalTicketCommand**

```csharp
// src/CRM.Application/Portal/Commands/ClosePortalTicketCommand.cs
using CRM.Domain.Tickets;
using CRM.Domain.Tickets.Events;
using FluentValidation;
using FluentValidation.Results;
using MediatR;

namespace CRM.Application.Portal.Commands;

public record ClosePortalTicketCommand(Guid TicketId, Guid CustomerId)
    : IRequest<CloseTicketResult>;

public record CloseTicketResult(Guid Id, string Status);

public class ClosePortalTicketCommandHandler
    : IRequestHandler<ClosePortalTicketCommand, CloseTicketResult>
{
    private readonly ITicketRepository _tickets;
    private readonly IPublisher _publisher;

    public ClosePortalTicketCommandHandler(
        ITicketRepository tickets, IPublisher publisher)
    {
        _tickets = tickets;
        _publisher = publisher;
    }

    public async Task<CloseTicketResult> Handle(
        ClosePortalTicketCommand cmd, CancellationToken ct)
    {
        var ticket = await _tickets.FindByIdAsync(cmd.TicketId, ct)
            ?? throw new KeyNotFoundException($"Ticket {cmd.TicketId} not found.");

        if (ticket.CustomerId != cmd.CustomerId)
            throw new UnauthorizedAccessException(
                "You can only close your own tickets.");

        if (ticket.Status == "Closed")
            throw new ValidationException(new[]
            {
                new ValidationFailure("Status",
                    "Ticket is already closed.", "TICKET_ALREADY_CLOSED")
            });

        ticket.Close("Customer");
        await _tickets.SaveChangesAsync(ct);

        await _publisher.Publish(
            new TicketClosedEvent(ticket.Id, ticket.CustomerId, ticket.DepartmentId), ct);

        return new CloseTicketResult(ticket.Id, ticket.Status);
    }
}
```

- [ ] **Step 5: Run tests to verify they pass**

```bash
dotnet test tests/CRM.Application.Tests/ --filter "ClosePortalTicketCommandHandlerTests" -v n
```

Expected: 4 tests PASS.

- [ ] **Step 6: Add CloseTicket action to PortalController**

Open `src/CRM.API/Controllers/PortalController.cs` and add:

```csharp
[HttpPost("tickets/{id:guid}/close")]
public async Task<IActionResult> CloseTicket(Guid id, CancellationToken ct)
{
    try
    {
        var result = await _mediator.Send(
            new ClosePortalTicketCommand(id, CurrentCustomerId), ct);
        return Ok(new { data = result });
    }
    catch (KeyNotFoundException ex) { return NotFound(new { error = ex.Message }); }
    catch (UnauthorizedAccessException ex)
        { return StatusCode(403, new { error = ex.Message }); }
    catch (FluentValidation.ValidationException ex)
        { return UnprocessableEntity(new { error = ex.Errors.First().ErrorCode }); }
}
```

- [ ] **Step 7: Commit**

```bash
git add src/CRM.Domain/Tickets/ \
        src/CRM.Application/Portal/Commands/ClosePortalTicketCommand.cs \
        src/CRM.API/Controllers/PortalController.cs \
        tests/CRM.Application.Tests/Portal/ClosePortalTicketCommandHandlerTests.cs
git commit -m "feat(portal): add POST /api/portal/tickets/{id}/close — customer-initiated ticket close with CSAT event"
```
