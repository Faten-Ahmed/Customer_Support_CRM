# Update Ticket — Implementation Plan

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

**Story:** US-BE-023  
**Goal:** Implement `PUT /api/tickets/{id}` — allows updating a ticket's subject, description, priority, category, department, and custom field values. Records history for each changed field.

**Architecture:** `UpdateTicketCommand(id, subject, description, priority, categoryId, departmentId, customFieldValues, updatedByUserId)` → handler fetches ticket, applies changes to allowed fields via domain methods, records history entries, saves. Returns `TicketDetailDto`.

**Tech Stack:** .NET 10, ASP.NET Core, MediatR, FluentValidation, EF Core, xUnit, Moq

---

## File Structure

| Action | Path |
|--------|------|
| Create | `src/CRM.Application/Tickets/Commands/UpdateTicketCommand.cs` |
| Create | `src/CRM.Application/Tickets/Validators/UpdateTicketCommandValidator.cs` |
| Modify | `src/CRM.Domain/Tickets/Ticket.cs` |
| Modify | `src/CRM.API/Controllers/TicketsController.cs` |
| Test   | `tests/CRM.Application.Tests/Tickets/UpdateTicketCommandHandlerTests.cs` |
| Test   | `tests/CRM.API.Tests/Tickets/TicketsControllerUpdateTests.cs` |

---

## Task 1: Add UpdateDetails to Ticket Domain

**Files:**
- Modify: `src/CRM.Domain/Tickets/Ticket.cs`

- [ ] **Step 1: Add UpdateDetails method to Ticket**

```csharp
// Add to Ticket class in src/CRM.Domain/Tickets/Ticket.cs:

public void UpdateDetails(
    string subject,
    string description,
    TicketPriority priority,
    Guid? categoryId,
    Guid? departmentId,
    string? customFieldValues,
    Guid changedBy)
{
    if (Subject != subject)
    {
        _history.Add(TicketHistory.Create(Id, "Subject", Subject, subject, changedBy));
        Subject = subject;
    }
    if (Description != description)
    {
        _history.Add(TicketHistory.Create(Id, "Description", null, "(updated)", changedBy));
        Description = description;
    }
    if (Priority != priority)
    {
        _history.Add(TicketHistory.Create(Id, "Priority", Priority.ToString(), priority.ToString(), changedBy));
        Priority = priority;
    }
    if (CategoryId != categoryId || DepartmentId != departmentId)
    {
        _history.Add(TicketHistory.Create(Id, "CategoryId",
            CategoryId?.ToString(), categoryId?.ToString(), changedBy));
        CategoryId = categoryId;
        DepartmentId = departmentId;
    }
    CustomFieldValues = customFieldValues;
    UpdatedAt = DateTime.UtcNow;
}
```

- [ ] **Step 2: Commit**

```bash
git add src/CRM.Domain/Tickets/Ticket.cs
git commit -m "feat(domain): add Ticket.UpdateDetails with field-level history recording"
```

---

## Task 2: UpdateTicket Command + Handler + Validator

**Files:**
- Create: `src/CRM.Application/Tickets/Commands/UpdateTicketCommand.cs`
- Create: `src/CRM.Application/Tickets/Validators/UpdateTicketCommandValidator.cs`
- Test: `tests/CRM.Application.Tests/Tickets/UpdateTicketCommandHandlerTests.cs`

- [ ] **Step 1: Write the failing tests**

```csharp
// tests/CRM.Application.Tests/Tickets/UpdateTicketCommandHandlerTests.cs
using CRM.Application.Tickets.Commands;
using CRM.Domain.Tickets;
using CRM.Domain.Tickets.Enums;
using Moq;
using Xunit;

namespace CRM.Application.Tests.Tickets;

public class UpdateTicketCommandHandlerTests
{
    private readonly Mock<ITicketRepository> _repo = new();
    private readonly UpdateTicketCommandHandler _handler;

    public UpdateTicketCommandHandlerTests()
    {
        _handler = new UpdateTicketCommandHandler(_repo.Object);
    }

    [Fact]
    public async Task Handle_ValidUpdate_ChangesSubjectAndPriority()
    {
        var id = Guid.NewGuid();
        var ticket = Ticket.Create(
            Guid.NewGuid(), "Old Subject", "Old Desc",
            TicketPriority.Low, TicketChannel.Internal, Guid.NewGuid());

        _repo.Setup(r => r.FindByIdDetailedAsync(id, default)).ReturnsAsync(ticket);

        var result = await _handler.Handle(new UpdateTicketCommand(
            id, "New Subject", "New Desc", TicketPriority.High,
            null, null, null, Guid.NewGuid()), default);

        Assert.Equal("New Subject", result.Subject);
        Assert.Equal("High", result.Priority);
        _repo.Verify(r => r.SaveChangesAsync(default), Times.Once);
    }

    [Fact]
    public async Task Handle_NonExistentTicket_ThrowsKeyNotFoundException()
    {
        _repo.Setup(r => r.FindByIdDetailedAsync(It.IsAny<Guid>(), default))
             .ReturnsAsync((Ticket?)null);

        await Assert.ThrowsAsync<KeyNotFoundException>(() =>
            _handler.Handle(new UpdateTicketCommand(
                Guid.NewGuid(), "S", "D", TicketPriority.Low,
                null, null, null, Guid.NewGuid()), default));
    }

    [Fact]
    public async Task Handle_ClosedTicket_ThrowsInvalidOperationException()
    {
        var id = Guid.NewGuid();
        var ticket = Ticket.Create(
            Guid.NewGuid(), "Subj", "Desc",
            TicketPriority.Low, TicketChannel.Internal, Guid.NewGuid());
        ticket.ChangeStatus(TicketStatus.Closed, Guid.NewGuid());

        _repo.Setup(r => r.FindByIdDetailedAsync(id, default)).ReturnsAsync(ticket);

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            _handler.Handle(new UpdateTicketCommand(
                id, "S", "D", TicketPriority.Low,
                null, null, null, Guid.NewGuid()), default));
    }
}
```

- [ ] **Step 2: Run test to verify it fails**

```bash
dotnet test tests/CRM.Application.Tests/ --filter "UpdateTicketCommandHandlerTests" -v n
```

Expected: FAIL — `UpdateTicketCommand` does not exist yet.

- [ ] **Step 3: Implement command and handler**

```csharp
// src/CRM.Application/Tickets/Commands/UpdateTicketCommand.cs
using CRM.Application.Tickets.DTOs;
using CRM.Application.Tickets.Queries;
using CRM.Domain.Tickets;
using CRM.Domain.Tickets.Enums;
using MediatR;

namespace CRM.Application.Tickets.Commands;

public record UpdateTicketCommand(
    Guid TicketId,
    string Subject,
    string Description,
    TicketPriority Priority,
    Guid? CategoryId,
    Guid? DepartmentId,
    string? CustomFieldValues,
    Guid UpdatedByUserId) : IRequest<TicketDetailDto>;

public class UpdateTicketCommandHandler : IRequestHandler<UpdateTicketCommand, TicketDetailDto>
{
    private readonly ITicketRepository _tickets;

    public UpdateTicketCommandHandler(ITicketRepository tickets) => _tickets = tickets;

    public async Task<TicketDetailDto> Handle(UpdateTicketCommand cmd, CancellationToken ct)
    {
        var ticket = await _tickets.FindByIdDetailedAsync(cmd.TicketId, ct)
            ?? throw new KeyNotFoundException($"Ticket {cmd.TicketId} not found.");

        if (ticket.Status == TicketStatus.Closed)
            throw new InvalidOperationException("Cannot edit a closed ticket.");

        ticket.UpdateDetails(
            cmd.Subject, cmd.Description, cmd.Priority,
            cmd.CategoryId, cmd.DepartmentId, cmd.CustomFieldValues,
            cmd.UpdatedByUserId);

        await _tickets.SaveChangesAsync(ct);

        // Reuse GetTicketQuery handler mapping — delegate to avoid duplication
        return new TicketDetailDto(
            ticket.Id, ticket.TicketNumber, ticket.CustomerId,
            ticket.Customer?.FullName ?? "Unknown",
            ticket.Subject, ticket.Description, ticket.Status.ToString(),
            ticket.Priority.ToString(), ticket.Channel.ToString(),
            ticket.AssignedToUserId, null,
            ticket.Category?.Name, ticket.Department?.Name,
            ticket.CustomFieldValues, null,
            ticket.CreatedAt, ticket.UpdatedAt, ticket.ResolvedAt, ticket.ClosedAt);
    }
}
```

- [ ] **Step 4: Create validator**

```csharp
// src/CRM.Application/Tickets/Validators/UpdateTicketCommandValidator.cs
using CRM.Application.Tickets.Commands;
using FluentValidation;

namespace CRM.Application.Tickets.Validators;

public class UpdateTicketCommandValidator : AbstractValidator<UpdateTicketCommand>
{
    public UpdateTicketCommandValidator()
    {
        RuleFor(x => x.TicketId).NotEmpty();
        RuleFor(x => x.Subject).NotEmpty().MaximumLength(500);
        RuleFor(x => x.Description).NotEmpty().MaximumLength(10000);
        RuleFor(x => x.UpdatedByUserId).NotEmpty();
    }
}
```

- [ ] **Step 5: Run tests to verify they pass**

```bash
dotnet test tests/CRM.Application.Tests/ --filter "UpdateTicketCommandHandlerTests" -v n
```

Expected: 3 tests PASS.

- [ ] **Step 6: Commit**

```bash
git add src/CRM.Application/Tickets/Commands/UpdateTicketCommand.cs \
        src/CRM.Application/Tickets/Validators/UpdateTicketCommandValidator.cs \
        tests/CRM.Application.Tests/Tickets/UpdateTicketCommandHandlerTests.cs
git commit -m "feat(tickets): add UpdateTicketCommand with history tracking"
```

---

## Task 3: TicketsController — PUT /api/tickets/{id}

**Files:**
- Modify: `src/CRM.API/Controllers/TicketsController.cs`
- Test: `tests/CRM.API.Tests/Tickets/TicketsControllerUpdateTests.cs`

- [ ] **Step 1: Write the failing test**

```csharp
// tests/CRM.API.Tests/Tickets/TicketsControllerUpdateTests.cs
using System.Net;
using System.Net.Http.Json;
using CRM.Application.Tickets.Commands;
using CRM.Application.Tickets.DTOs;
using MediatR;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using Xunit;

namespace CRM.API.Tests.Tickets;

public class TicketsControllerUpdateTests
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
    public async Task UpdateTicket_ValidBody_Returns200()
    {
        var id = Guid.NewGuid();
        _mediator.Setup(m => m.Send(It.IsAny<UpdateTicketCommand>(), default))
                 .ReturnsAsync(new TicketDetailDto(
                     id, "TKT-001", Guid.NewGuid(), "Ali Hassan",
                     "New Subject", "New Desc", "New", "High", "Internal",
                     null, null, null, null, null, null,
                     DateTime.UtcNow, DateTime.UtcNow, null, null));

        var client = BuildClient();
        var response = await client.PutAsJsonAsync($"/api/tickets/{id}",
            new { subject = "New Subject", description = "New Desc", priority = "High" });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task UpdateTicket_ClosedTicket_Returns409()
    {
        _mediator.Setup(m => m.Send(It.IsAny<UpdateTicketCommand>(), default))
                 .ThrowsAsync(new InvalidOperationException("Cannot edit closed ticket."));

        var client = BuildClient();
        var response = await client.PutAsJsonAsync($"/api/tickets/{Guid.NewGuid()}",
            new { subject = "S", description = "D", priority = "Low" });

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
    }
}
```

- [ ] **Step 2: Run test to verify it fails**

```bash
dotnet test tests/CRM.API.Tests/ --filter "TicketsControllerUpdateTests" -v n
```

Expected: FAIL — `PUT /api/tickets/{id}` does not exist.

- [ ] **Step 3: Add Update endpoint to TicketsController**

```csharp
// Add to src/CRM.API/Controllers/TicketsController.cs inside the class:

public record UpdateTicketRequest(
    string Subject, string Description, TicketPriority Priority,
    Guid? CategoryId, Guid? DepartmentId, string? CustomFieldValues);

[HttpPut("{id:guid}")]
public async Task<IActionResult> Update(
    Guid id, [FromBody] UpdateTicketRequest request, CancellationToken ct)
{
    try
    {
        var result = await _mediator.Send(new UpdateTicketCommand(
            id, request.Subject, request.Description, request.Priority,
            request.CategoryId, request.DepartmentId, request.CustomFieldValues,
            CurrentUserId), ct);
        return Ok(result);
    }
    catch (KeyNotFoundException ex) { return NotFound(new { error = ex.Message }); }
    catch (InvalidOperationException ex) { return Conflict(new { error = ex.Message }); }
}
```

- [ ] **Step 4: Run tests to verify they pass**

```bash
dotnet test tests/CRM.API.Tests/ --filter "TicketsControllerUpdateTests" -v n
```

Expected: 2 tests PASS.

- [ ] **Step 5: Commit**

```bash
git add src/CRM.API/Controllers/TicketsController.cs \
        tests/CRM.API.Tests/Tickets/TicketsControllerUpdateTests.cs
git commit -m "feat(api): add PUT /api/tickets/{id} endpoint"
```
