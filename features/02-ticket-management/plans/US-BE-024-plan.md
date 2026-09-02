# Assign Ticket — Implementation Plan

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

**Story:** US-BE-024  
**Goal:** Implement `PATCH /api/tickets/{id}/assign` — assigns a ticket to a specific agent, changing status to Assigned and notifying the agent.

**Architecture:** `AssignTicketCommand(ticketId, agentId, assignedByUserId)` → handler validates agent exists and is active, calls `ticket.Assign(agentId, changedBy)`, persists, publishes `TicketAssignedEvent` (for notification). Admin/Manager only. Returns 404 if ticket not found, 400 if agent invalid or ticket in non-assignable status.

**Tech Stack:** .NET 10, ASP.NET Core, MediatR, EF Core, xUnit, Moq

---

## File Structure

| Action | Path |
|--------|------|
| Create | `src/CRM.Application/Tickets/Commands/AssignTicketCommand.cs` |
| Modify | `src/CRM.API/Controllers/TicketsController.cs` |
| Test   | `tests/CRM.Application.Tests/Tickets/AssignTicketCommandHandlerTests.cs` |
| Test   | `tests/CRM.API.Tests/Tickets/TicketsControllerAssignTests.cs` |

---

## Task 1: AssignTicket Command + Handler

**Files:**
- Create: `src/CRM.Application/Tickets/Commands/AssignTicketCommand.cs`
- Test: `tests/CRM.Application.Tests/Tickets/AssignTicketCommandHandlerTests.cs`

- [ ] **Step 1: Write the failing tests**

```csharp
// tests/CRM.Application.Tests/Tickets/AssignTicketCommandHandlerTests.cs
using CRM.Application.Tickets.Commands;
using CRM.Domain.Tickets;
using CRM.Domain.Tickets.Enums;
using CRM.Domain.Users;
using CRM.Infrastructure.Identity;
using Moq;
using Xunit;

namespace CRM.Application.Tests.Tickets;

public class AssignTicketCommandHandlerTests
{
    private readonly Mock<ITicketRepository> _ticketRepo = new();
    private readonly Mock<IUserRepository> _userRepo = new();
    private readonly AssignTicketCommandHandler _handler;

    public AssignTicketCommandHandlerTests()
    {
        _handler = new AssignTicketCommandHandler(_ticketRepo.Object, _userRepo.Object);
    }

    [Fact]
    public async Task Handle_ValidAssignment_SetsAssigneeAndStatusAssigned()
    {
        var ticketId = Guid.NewGuid();
        var agentId = Guid.NewGuid();
        var managerId = Guid.NewGuid();

        var ticket = Ticket.Create(Guid.NewGuid(), "Subj", "Desc",
            TicketPriority.Medium, TicketChannel.Internal, managerId);
        var agent = User.CreateForTest("agent@crm.test", "hash",
            UserRole.Agent, true, false, agentId);

        _ticketRepo.Setup(r => r.FindByIdDetailedAsync(ticketId, default)).ReturnsAsync(ticket);
        _userRepo.Setup(r => r.FindByIdAsync(agentId, default)).ReturnsAsync(agent);

        await _handler.Handle(new AssignTicketCommand(ticketId, agentId, managerId), default);

        Assert.Equal(agentId, ticket.AssignedToUserId);
        Assert.Equal(TicketStatus.Assigned, ticket.Status);
        _ticketRepo.Verify(r => r.SaveChangesAsync(default), Times.Once);
    }

    [Fact]
    public async Task Handle_NonExistentTicket_ThrowsKeyNotFoundException()
    {
        _ticketRepo.Setup(r => r.FindByIdDetailedAsync(It.IsAny<Guid>(), default))
                   .ReturnsAsync((Ticket?)null);

        await Assert.ThrowsAsync<KeyNotFoundException>(() =>
            _handler.Handle(
                new AssignTicketCommand(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid()),
                default));
    }

    [Fact]
    public async Task Handle_NonExistentAgent_ThrowsKeyNotFoundException()
    {
        var ticket = Ticket.Create(Guid.NewGuid(), "S", "D",
            TicketPriority.Low, TicketChannel.Internal, Guid.NewGuid());

        _ticketRepo.Setup(r => r.FindByIdDetailedAsync(It.IsAny<Guid>(), default))
                   .ReturnsAsync(ticket);
        _userRepo.Setup(r => r.FindByIdAsync(It.IsAny<Guid>(), default))
                 .ReturnsAsync((User?)null);

        await Assert.ThrowsAsync<KeyNotFoundException>(() =>
            _handler.Handle(
                new AssignTicketCommand(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid()),
                default));
    }

    [Fact]
    public async Task Handle_InactiveAgent_ThrowsInvalidOperationException()
    {
        var ticket = Ticket.Create(Guid.NewGuid(), "S", "D",
            TicketPriority.Low, TicketChannel.Internal, Guid.NewGuid());
        var inactiveAgent = User.CreateForTest("agent@crm.test", "hash",
            UserRole.Agent, false, false);

        _ticketRepo.Setup(r => r.FindByIdDetailedAsync(It.IsAny<Guid>(), default))
                   .ReturnsAsync(ticket);
        _userRepo.Setup(r => r.FindByIdAsync(It.IsAny<Guid>(), default))
                 .ReturnsAsync(inactiveAgent);

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            _handler.Handle(
                new AssignTicketCommand(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid()),
                default));
    }

    [Fact]
    public async Task Handle_ClosedTicket_ThrowsInvalidOperationException()
    {
        var ticketId = Guid.NewGuid();
        var ticket = Ticket.Create(Guid.NewGuid(), "S", "D",
            TicketPriority.Low, TicketChannel.Internal, Guid.NewGuid());
        ticket.ChangeStatus(TicketStatus.Closed, Guid.NewGuid());

        var agent = User.CreateForTest("a@b.com", "hash", UserRole.Agent, true, false);

        _ticketRepo.Setup(r => r.FindByIdDetailedAsync(ticketId, default)).ReturnsAsync(ticket);
        _userRepo.Setup(r => r.FindByIdAsync(It.IsAny<Guid>(), default)).ReturnsAsync(agent);

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            _handler.Handle(
                new AssignTicketCommand(ticketId, Guid.NewGuid(), Guid.NewGuid()),
                default));
    }
}
```

- [ ] **Step 2: Run test to verify it fails**

```bash
dotnet test tests/CRM.Application.Tests/ --filter "AssignTicketCommandHandlerTests" -v n
```

Expected: FAIL — `AssignTicketCommand` does not exist yet.

- [ ] **Step 3: Implement command and handler**

```csharp
// src/CRM.Application/Tickets/Commands/AssignTicketCommand.cs
using CRM.Domain.Tickets;
using CRM.Domain.Tickets.Enums;
using CRM.Infrastructure.Identity;
using MediatR;

namespace CRM.Application.Tickets.Commands;

public record AssignTicketCommand(
    Guid TicketId,
    Guid AgentId,
    Guid AssignedByUserId) : IRequest;

public class AssignTicketCommandHandler : IRequestHandler<AssignTicketCommand>
{
    private readonly ITicketRepository _tickets;
    private readonly IUserRepository _users;

    public AssignTicketCommandHandler(ITicketRepository tickets, IUserRepository users)
    {
        _tickets = tickets;
        _users = users;
    }

    public async Task Handle(AssignTicketCommand cmd, CancellationToken ct)
    {
        var ticket = await _tickets.FindByIdDetailedAsync(cmd.TicketId, ct)
            ?? throw new KeyNotFoundException($"Ticket {cmd.TicketId} not found.");

        if (ticket.Status == TicketStatus.Closed || ticket.Status == TicketStatus.Resolved)
            throw new InvalidOperationException(
                $"Cannot assign a ticket in {ticket.Status} status.");

        var agent = await _users.FindByIdAsync(cmd.AgentId, ct)
            ?? throw new KeyNotFoundException($"Agent {cmd.AgentId} not found.");

        if (!agent.IsActive)
            throw new InvalidOperationException("Cannot assign to an inactive agent.");

        ticket.Assign(cmd.AgentId, cmd.AssignedByUserId);
        await _tickets.SaveChangesAsync(ct);
    }
}
```

- [ ] **Step 4: Run tests to verify they pass**

```bash
dotnet test tests/CRM.Application.Tests/ --filter "AssignTicketCommandHandlerTests" -v n
```

Expected: 5 tests PASS.

- [ ] **Step 5: Commit**

```bash
git add src/CRM.Application/Tickets/Commands/AssignTicketCommand.cs \
        tests/CRM.Application.Tests/Tickets/AssignTicketCommandHandlerTests.cs
git commit -m "feat(tickets): add AssignTicketCommand with status and agent validation"
```

---

## Task 2: TicketsController — PATCH /api/tickets/{id}/assign

**Files:**
- Modify: `src/CRM.API/Controllers/TicketsController.cs`
- Test: `tests/CRM.API.Tests/Tickets/TicketsControllerAssignTests.cs`

- [ ] **Step 1: Write the failing test**

```csharp
// tests/CRM.API.Tests/Tickets/TicketsControllerAssignTests.cs
using System.Net;
using System.Net.Http.Json;
using MediatR;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using Xunit;

namespace CRM.API.Tests.Tickets;

public class TicketsControllerAssignTests
{
    private readonly Mock<IMediator> _mediator = new();

    private HttpClient BuildClient(string role = "Manager")
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
                "Bearer", TestJwtHelper.CreateTestToken(role: role));
        return client;
    }

    [Fact]
    public async Task Assign_ValidRequest_Returns204()
    {
        _mediator.Setup(m => m.Send(It.IsAny<IRequest>(), default)).Returns(Task.CompletedTask);
        var client = BuildClient();

        var response = await client.PatchAsJsonAsync(
            $"/api/tickets/{Guid.NewGuid()}/assign",
            new { agentId = Guid.NewGuid() });

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
    }

    [Fact]
    public async Task Assign_AsAgent_Returns403()
    {
        var client = BuildClient(role: "Agent");

        var response = await client.PatchAsJsonAsync(
            $"/api/tickets/{Guid.NewGuid()}/assign",
            new { agentId = Guid.NewGuid() });

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task Assign_InvalidAgent_Returns400()
    {
        _mediator.Setup(m => m.Send(It.IsAny<IRequest>(), default))
                 .ThrowsAsync(new InvalidOperationException("Agent inactive."));
        var client = BuildClient();

        var response = await client.PatchAsJsonAsync(
            $"/api/tickets/{Guid.NewGuid()}/assign",
            new { agentId = Guid.NewGuid() });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }
}
```

- [ ] **Step 2: Run test to verify it fails**

```bash
dotnet test tests/CRM.API.Tests/ --filter "TicketsControllerAssignTests" -v n
```

Expected: FAIL — endpoint does not exist.

- [ ] **Step 3: Add Assign endpoint to TicketsController**

```csharp
// Add to src/CRM.API/Controllers/TicketsController.cs inside the class:

public record AssignTicketRequest(Guid AgentId);

[Authorize(Roles = "Admin,Manager")]
[HttpPatch("{id:guid}/assign")]
public async Task<IActionResult> Assign(
    Guid id, [FromBody] AssignTicketRequest request, CancellationToken ct)
{
    try
    {
        await _mediator.Send(new AssignTicketCommand(id, request.AgentId, CurrentUserId), ct);
        return NoContent();
    }
    catch (KeyNotFoundException ex) { return NotFound(new { error = ex.Message }); }
    catch (InvalidOperationException ex) { return BadRequest(new { error = ex.Message }); }
}
```

- [ ] **Step 4: Run tests to verify they pass**

```bash
dotnet test tests/CRM.API.Tests/ --filter "TicketsControllerAssignTests" -v n
```

Expected: 3 tests PASS.

- [ ] **Step 5: Commit**

```bash
git add src/CRM.API/Controllers/TicketsController.cs \
        tests/CRM.API.Tests/Tickets/TicketsControllerAssignTests.cs
git commit -m "feat(api): add PATCH /api/tickets/{id}/assign (Manager/Admin only)"
```
