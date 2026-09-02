# Transfer Ticket — Implementation Plan

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

**Story:** US-BE-026  
**Goal:** Implement `POST /api/v1/tickets/{id}/transfer` — transfers a ticket to a different **department** (department-only; no agent reassignment), clears the assignee, resets status to New, records the transfer in history.

**Architecture:** `TransferTicketCommand(ticketId, departmentId, transferNote, transferredByUserId)` → handler validates ticket is transferable (not Closed/Resolved) and that target department is active, calls `Ticket.Transfer(targetDepartmentId, transferNote, transferredBy)` which always clears the assignee and resets status to New.

> **⚠️ Implementation divergences from original plan:**
> - HTTP method is `POST` (not `PATCH`)
> - Route is `/api/v1/tickets/{id}/transfer`
> - No agent reassignment — ticket always goes back to unassigned/New
> - `TransferTicketCommand` has no `TargetAgentId`; `TargetDepartmentId` is required (non-nullable `Guid`)
> - Field renamed: `Reason` → `TransferNote`
> - `Ticket.Transfer()` signature: `(Guid targetDepartmentId, string transferNote, Guid transferredBy)`
> - Handler calls `ITicketRepository.IsDepartmentActiveAsync` to validate the target department
> - Endpoint authorized for `Admin,Manager,Agent` (not just `Admin,Manager`)
> - Returns `422` for validation failures (invalid dept), `404` for not found, `409` for status conflicts

**Tech Stack:** .NET 10, ASP.NET Core, MediatR, EF Core, xUnit, Moq

---

## File Structure

| Action | Path |
|--------|------|
| Create | `src/CRM.Application/Tickets/Commands/TransferTicketCommand.cs` |
| Modify | `src/CRM.Domain/Tickets/Ticket.cs` |
| Modify | `src/CRM.API/Controllers/TicketsController.cs` |
| Test   | `tests/CRM.Application.Tests/Tickets/TransferTicketCommandHandlerTests.cs` |
| Test   | `tests/CRM.API.Tests/Tickets/TicketsControllerTransferTests.cs` |

---

## Task 1: Add Transfer to Ticket Domain

**Files:**
- Modify: `src/CRM.Domain/Tickets/Ticket.cs`

- [ ] **Step 1: Add Transfer method to Ticket**

```csharp
// Add to Ticket class in src/CRM.Domain/Tickets/Ticket.cs:
// ⚠️ Department-only transfer: always clears assignee, always resets to New

public void Transfer(
    Guid targetDepartmentId,
    string transferNote,
    Guid transferredBy)
{
    var oldDept = DepartmentId?.ToString();
    var oldAgent = AssignedToUserId?.ToString();

    DepartmentId = targetDepartmentId;
    AssignedToUserId = null;
    Status = TicketStatus.New;
    UpdatedAt = DateTime.UtcNow;

    _history.Add(TicketHistory.Create(Id, "Transfer", oldDept, targetDepartmentId.ToString(), transferredBy));
    _history.Add(TicketHistory.Create(Id, "AssignedTo", oldAgent, null, transferredBy));
    _history.Add(TicketHistory.Create(Id, "TransferNote", null, transferNote, transferredBy));
}
```

- [ ] **Step 2: Commit**

```bash
git add src/CRM.Domain/Tickets/Ticket.cs
git commit -m "feat(domain): add Ticket.Transfer method"
```

---

## Task 2: TransferTicket Command + Handler

**Files:**
- Create: `src/CRM.Application/Tickets/Commands/TransferTicketCommand.cs`
- Test: `tests/CRM.Application.Tests/Tickets/TransferTicketCommandHandlerTests.cs`

- [ ] **Step 1: Write the failing tests**

```csharp
// tests/CRM.Application.Tests/Tickets/TransferTicketCommandHandlerTests.cs
using CRM.Application.Tickets.Commands;
using CRM.Domain.Tickets;
using CRM.Domain.Tickets.Enums;
using CRM.Infrastructure.Identity;
using Moq;
using Xunit;

namespace CRM.Application.Tests.Tickets;

public class TransferTicketCommandHandlerTests
{
    private readonly Mock<ITicketRepository> _ticketRepo = new();
    private readonly Mock<IUserRepository> _userRepo = new();
    private readonly TransferTicketCommandHandler _handler;

    public TransferTicketCommandHandlerTests()
    {
        _handler = new TransferTicketCommandHandler(_ticketRepo.Object, _userRepo.Object);
    }

    [Fact]
    public async Task Handle_ValidTransfer_UpdatesDepartmentAndAgent()
    {
        var ticketId = Guid.NewGuid();
        var ticket = Ticket.Create(Guid.NewGuid(), "S", "D",
            TicketPriority.Medium, TicketChannel.Internal, Guid.NewGuid());
        ticket.Assign(Guid.NewGuid(), Guid.NewGuid());

        var newAgentId = Guid.NewGuid();
        var newDeptId = Guid.NewGuid();
        var agent = User.CreateForTest("a@b.com", "h", UserRole.Agent, true, false, newAgentId);

        _ticketRepo.Setup(r => r.FindByIdDetailedAsync(ticketId, default)).ReturnsAsync(ticket);
        _userRepo.Setup(r => r.FindByIdAsync(newAgentId, default)).ReturnsAsync(agent);

        await _handler.Handle(new TransferTicketCommand(
            ticketId, newDeptId, newAgentId, "Specialist needed", Guid.NewGuid()), default);

        Assert.Equal(newDeptId, ticket.DepartmentId);
        Assert.Equal(newAgentId, ticket.AssignedToUserId);
        _ticketRepo.Verify(r => r.SaveChangesAsync(default), Times.Once);
    }

    [Fact]
    public async Task Handle_ClosedTicket_ThrowsInvalidOperationException()
    {
        var id = Guid.NewGuid();
        var ticket = Ticket.Create(Guid.NewGuid(), "S", "D",
            TicketPriority.Low, TicketChannel.Internal, Guid.NewGuid());
        ticket.ChangeStatus(TicketStatus.Closed, Guid.NewGuid());

        _ticketRepo.Setup(r => r.FindByIdDetailedAsync(id, default)).ReturnsAsync(ticket);

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            _handler.Handle(new TransferTicketCommand(
                id, Guid.NewGuid(), null, "reason", Guid.NewGuid()), default));
    }

    [Fact]
    public async Task Handle_DeptTransferOnlyNoAgent_ClearsAssignee()
    {
        var id = Guid.NewGuid();
        var ticket = Ticket.Create(Guid.NewGuid(), "S", "D",
            TicketPriority.Low, TicketChannel.Internal, Guid.NewGuid());
        ticket.Assign(Guid.NewGuid(), Guid.NewGuid());

        _ticketRepo.Setup(r => r.FindByIdDetailedAsync(id, default)).ReturnsAsync(ticket);

        await _handler.Handle(new TransferTicketCommand(
            id, Guid.NewGuid(), null, "Dept only transfer", Guid.NewGuid()), default);

        Assert.Null(ticket.AssignedToUserId);
        Assert.Equal(TicketStatus.New, ticket.Status);
    }
}
```

- [ ] **Step 2: Run test to verify it fails**

```bash
dotnet test tests/CRM.Application.Tests/ --filter "TransferTicketCommandHandlerTests" -v n
```

Expected: FAIL — `TransferTicketCommand` does not exist yet.

- [ ] **Step 3: Implement command and handler**

```csharp
// src/CRM.Application/Tickets/Commands/TransferTicketCommand.cs
// ⚠️ No TargetAgentId; TargetDepartmentId is required (non-nullable); field is TransferNote not Reason
using CRM.Domain.Tickets;
using CRM.Domain.Tickets.Enums;
using MediatR;

namespace CRM.Application.Tickets.Commands;

public record TransferTicketCommand(
    Guid TicketId,
    Guid DepartmentId,
    string TransferNote,
    Guid TransferredByUserId) : IRequest;

public class TransferTicketCommandHandler : IRequestHandler<TransferTicketCommand>
{
    private readonly ITicketRepository _tickets;
    private readonly IUserRepository _users;

    public TransferTicketCommandHandler(ITicketRepository tickets, IUserRepository users)
    {
        _tickets = tickets;
        _users = users;
    }

    public async Task Handle(TransferTicketCommand cmd, CancellationToken ct)
    {
        var ticket = await _tickets.FindByIdDetailedAsync(cmd.TicketId, ct)
            ?? throw new KeyNotFoundException($"Ticket {cmd.TicketId} not found.");

        if (ticket.Status == TicketStatus.Closed || ticket.Status == TicketStatus.Resolved)
            throw new InvalidOperationException(
                $"Cannot transfer a ticket in {ticket.Status} status.");

        if (cmd.TargetAgentId.HasValue)
        {
            var agent = await _users.FindByIdAsync(cmd.TargetAgentId.Value, ct)
                ?? throw new KeyNotFoundException("Target agent not found.");
            if (!agent.IsActive)
                throw new InvalidOperationException("Cannot transfer to inactive agent.");
        }

        ticket.Transfer(cmd.TargetDepartmentId, cmd.TargetAgentId, cmd.Reason, cmd.TransferredByUserId);
        await _tickets.SaveChangesAsync(ct);
    }
}
```

- [ ] **Step 4: Run tests to verify they pass**

```bash
dotnet test tests/CRM.Application.Tests/ --filter "TransferTicketCommandHandlerTests" -v n
```

Expected: 3 tests PASS.

- [ ] **Step 5: Commit**

```bash
git add src/CRM.Application/Tickets/Commands/TransferTicketCommand.cs \
        tests/CRM.Application.Tests/Tickets/TransferTicketCommandHandlerTests.cs
git commit -m "feat(tickets): add TransferTicketCommand"
```

---

## Task 3: TicketsController — PATCH /api/tickets/{id}/transfer

**Files:**
- Modify: `src/CRM.API/Controllers/TicketsController.cs`
- Test: `tests/CRM.API.Tests/Tickets/TicketsControllerTransferTests.cs`

- [ ] **Step 1: Write the failing test**

```csharp
// tests/CRM.API.Tests/Tickets/TicketsControllerTransferTests.cs
using System.Net;
using System.Net.Http.Json;
using MediatR;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using Xunit;

namespace CRM.API.Tests.Tickets;

public class TicketsControllerTransferTests
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
                "Bearer", TestJwtHelper.CreateTestToken(role: "Manager"));
        return client;
    }

    [Fact]
    public async Task Transfer_ValidRequest_Returns204()
    {
        _mediator.Setup(m => m.Send(It.IsAny<IRequest>(), default)).Returns(Task.CompletedTask);
        var client = BuildClient();

        var response = await client.PatchAsJsonAsync(
            $"/api/tickets/{Guid.NewGuid()}/transfer",
            new { targetDepartmentId = Guid.NewGuid(), reason = "Specialist needed" });

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
    }

    [Fact]
    public async Task Transfer_ClosedTicket_Returns409()
    {
        _mediator.Setup(m => m.Send(It.IsAny<IRequest>(), default))
                 .ThrowsAsync(new InvalidOperationException("Cannot transfer closed ticket."));
        var client = BuildClient();

        var response = await client.PatchAsJsonAsync(
            $"/api/tickets/{Guid.NewGuid()}/transfer",
            new { targetDepartmentId = Guid.NewGuid(), reason = "reason" });

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
    }
}
```

- [ ] **Step 2: Run test to verify it fails**

```bash
dotnet test tests/CRM.API.Tests/ --filter "TicketsControllerTransferTests" -v n
```

Expected: FAIL — endpoint does not exist.

- [ ] **Step 3: Add Transfer endpoint to TicketsController**

```csharp
// Add to src/CRM.API/Controllers/TicketsController.cs inside the class:
// ⚠️ POST not PATCH; open to Agent too; TransferNote not Reason; returns 422 for invalid dept

public record TransferTicketRequest(Guid DepartmentId, string TransferNote);

[Authorize(Roles = "Admin,Manager,Agent")]
[HttpPost("{id:guid}/transfer")]
public async Task<IActionResult> Transfer(
    Guid id, [FromBody] TransferTicketRequest request, CancellationToken ct)
{
    try
    {
        await _mediator.Send(new TransferTicketCommand(
            id, request.DepartmentId, request.TransferNote, CurrentUserId), ct);
        return NoContent();
    }
    catch (KeyNotFoundException ex) { return NotFound(new { error = ex.Message }); }
    catch (InvalidOperationException ex) when (ex.Message.StartsWith("Department"))
        { return UnprocessableEntity(new { errors = new[] { new { code = "INVALID_DEPARTMENT", message = ex.Message } } }); }
    catch (InvalidOperationException ex) { return Conflict(new { error = ex.Message }); }
}
```

- [ ] **Step 4: Run tests to verify they pass**

```bash
dotnet test tests/CRM.API.Tests/ --filter "TicketsControllerTransferTests" -v n
```

Expected: 2 tests PASS.

- [ ] **Step 5: Commit**

```bash
git add src/CRM.API/Controllers/TicketsController.cs \
        tests/CRM.API.Tests/Tickets/TicketsControllerTransferTests.cs
git commit -m "feat(api): add PATCH /api/tickets/{id}/transfer endpoint"
```
