# Get Ticket — Implementation Plan

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

**Story:** US-BE-021  
**Goal:** Implement `GET /api/v1/tickets/{id}` — returns full ticket detail (subject, description, status, SLA info, assignee, category, messages summary) for Admin/Manager/Agent.

**Architecture:** `GetTicketQuery(id)` → handler fetches `Ticket` with related data (assignee name, category name, SLA record) via `ITicketRepository.FindByIdDetailedAsync`, maps to `TicketDetailDto`. Returns 404 if not found.

> **⚠️ Implementation divergences from original plan:**
> - `TicketDetailDto` includes additional fields: `SubjectAr`, `DescriptionAr`, `DepartmentId`, `CategoryId`
> - `DepartmentName` and `CategoryName` are resolved via separate `ITicketRepository` methods (`GetDepartmentNameAsync`, `GetCategoryNameAsync`) — not via EF Core navigation properties / joins
> - Full `TicketDetailDto` signature: `(Guid Id, string TicketNumber, Guid CustomerId, string CustomerName, string Subject, string SubjectAr, string Description, string DescriptionAr, string Status, string Priority, string Channel, Guid? AssignedToUserId, string? AssignedToName, Guid? DepartmentId, string? DepartmentName, Guid? CategoryId, string? CategoryName, string? CustomFieldValues, SlaInfoDto? Sla, DateTime CreatedAt, DateTime UpdatedAt, DateTime? ResolvedAt, DateTime? ClosedAt)`

**Tech Stack:** .NET 10, ASP.NET Core, MediatR, EF Core, xUnit, Moq

---

## File Structure

| Action | Path |
|--------|------|
| Create | `src/CRM.Application/Tickets/Queries/GetTicketQuery.cs` |
| Create | `src/CRM.Application/Tickets/DTOs/TicketDetailDto.cs` |
| Modify | `src/CRM.API/Controllers/TicketsController.cs` |
| Test   | `tests/CRM.Application.Tests/Tickets/GetTicketQueryHandlerTests.cs` |
| Test   | `tests/CRM.API.Tests/Tickets/TicketsControllerGetTests.cs` |

---

## Task 1: GetTicket Query + Handler + DTO

**Files:**
- Create: `src/CRM.Application/Tickets/DTOs/TicketDetailDto.cs`
- Create: `src/CRM.Application/Tickets/Queries/GetTicketQuery.cs`
- Test: `tests/CRM.Application.Tests/Tickets/GetTicketQueryHandlerTests.cs`

- [ ] **Step 1: Write the failing tests**

```csharp
// tests/CRM.Application.Tests/Tickets/GetTicketQueryHandlerTests.cs
using CRM.Application.Tickets.Queries;
using CRM.Domain.Tickets;
using CRM.Domain.Tickets.Enums;
using Moq;
using Xunit;

namespace CRM.Application.Tests.Tickets;

public class GetTicketQueryHandlerTests
{
    private readonly Mock<ITicketRepository> _repo = new();
    private readonly GetTicketQueryHandler _handler;

    public GetTicketQueryHandlerTests()
    {
        _handler = new GetTicketQueryHandler(_repo.Object);
    }

    [Fact]
    public async Task Handle_ExistingTicket_ReturnsTicketDetailDto()
    {
        var id = Guid.NewGuid();
        var ticket = Ticket.Create(
            Guid.NewGuid(), "Cannot login", "Description",
            TicketPriority.High, TicketChannel.Internal, Guid.NewGuid());

        _repo.Setup(r => r.FindByIdDetailedAsync(id, default)).ReturnsAsync(ticket);

        var result = await _handler.Handle(new GetTicketQuery(id), default);

        Assert.Equal("Cannot login", result.Subject);
        Assert.Equal("New", result.Status);
        Assert.Equal("High", result.Priority);
    }

    [Fact]
    public async Task Handle_NonExistentTicket_ThrowsKeyNotFoundException()
    {
        _repo.Setup(r => r.FindByIdDetailedAsync(It.IsAny<Guid>(), default))
             .ReturnsAsync((Ticket?)null);

        await Assert.ThrowsAsync<KeyNotFoundException>(() =>
            _handler.Handle(new GetTicketQuery(Guid.NewGuid()), default));
    }
}
```

- [ ] **Step 2: Run test to verify it fails**

```bash
dotnet test tests/CRM.Application.Tests/ --filter "GetTicketQueryHandlerTests" -v n
```

Expected: FAIL — `GetTicketQuery` does not exist yet.

- [ ] **Step 3: Create DTO**

```csharp
// src/CRM.Application/Tickets/DTOs/TicketDetailDto.cs
namespace CRM.Application.Tickets.DTOs;

public record TicketHistoryEntryDto(
    string FieldChanged,
    string? OldValue,
    string? NewValue,
    string ChangedByName,
    DateTime ChangedAt);

public record SlaInfoDto(
    DateTime? FirstResponseDue,
    DateTime? ResolutionDue,
    bool FirstResponseBreached,
    bool ResolutionBreached,
    string BreachTier);

public record TicketDetailDto(
    Guid Id,
    string TicketNumber,
    Guid CustomerId,
    string CustomerName,
    string Subject,
    string? SubjectAr,
    string Description,
    string? DescriptionAr,
    string Status,
    string Priority,
    string Channel,
    Guid? AssignedToUserId,
    string? AssignedToName,
    string? CategoryName,
    string? DepartmentName,
    string? CustomFieldValues,
    SlaInfoDto? Sla,
    DateTime CreatedAt,
    DateTime UpdatedAt,
    DateTime? ResolvedAt,
    DateTime? ClosedAt);
```

- [ ] **Step 4: Create query and handler**

```csharp
// src/CRM.Application/Tickets/Queries/GetTicketQuery.cs
using CRM.Application.Tickets.DTOs;
using CRM.Domain.Tickets;
using MediatR;

namespace CRM.Application.Tickets.Queries;

public record GetTicketQuery(Guid TicketId) : IRequest<TicketDetailDto>;

public class GetTicketQueryHandler : IRequestHandler<GetTicketQuery, TicketDetailDto>
{
    private readonly ITicketRepository _tickets;

    public GetTicketQueryHandler(ITicketRepository tickets) => _tickets = tickets;

    public async Task<TicketDetailDto> Handle(GetTicketQuery query, CancellationToken ct)
    {
        var ticket = await _tickets.FindByIdDetailedAsync(query.TicketId, ct)
            ?? throw new KeyNotFoundException($"Ticket {query.TicketId} not found.");

        return new TicketDetailDto(
            Id: ticket.Id,
            TicketNumber: ticket.TicketNumber,
            CustomerId: ticket.CustomerId,
            CustomerName: ticket.Customer?.FullName ?? "Unknown",
            Subject: ticket.Subject,
            SubjectAr: ticket.SubjectAr,
            Description: ticket.Description,
            DescriptionAr: ticket.DescriptionAr,
            Status: ticket.Status.ToString(),
            Priority: ticket.Priority.ToString(),
            Channel: ticket.Channel.ToString(),
            AssignedToUserId: ticket.AssignedToUserId,
            AssignedToName: ticket.AssignedTo is null
                ? null : $"{ticket.AssignedTo.FirstName} {ticket.AssignedTo.LastName}",
            CategoryName: ticket.Category?.Name,
            DepartmentName: ticket.Department?.Name,
            CustomFieldValues: ticket.CustomFieldValues,
            Sla: ticket.TicketSla is null ? null : new SlaInfoDto(
                ticket.TicketSla.FirstResponseDue,
                ticket.TicketSla.ResolutionDue,
                ticket.TicketSla.FirstResponseBreached,
                ticket.TicketSla.ResolutionBreached,
                ticket.TicketSla.BreachTier.ToString()),
            CreatedAt: ticket.CreatedAt,
            UpdatedAt: ticket.UpdatedAt,
            ResolvedAt: ticket.ResolvedAt,
            ClosedAt: ticket.ClosedAt);
    }
}
```

- [ ] **Step 5: Run tests to verify they pass**

```bash
dotnet test tests/CRM.Application.Tests/ --filter "GetTicketQueryHandlerTests" -v n
```

Expected: 2 tests PASS.

- [ ] **Step 6: Commit**

```bash
git add src/CRM.Application/Tickets/Queries/GetTicketQuery.cs \
        src/CRM.Application/Tickets/DTOs/TicketDetailDto.cs \
        tests/CRM.Application.Tests/Tickets/GetTicketQueryHandlerTests.cs
git commit -m "feat(tickets): add GetTicketQuery with full detail mapping"
```

---

## Task 2: TicketsController — GET /api/tickets/{id}

**Files:**
- Modify: `src/CRM.API/Controllers/TicketsController.cs`
- Test: `tests/CRM.API.Tests/Tickets/TicketsControllerGetTests.cs`

- [ ] **Step 1: Write the failing test**

```csharp
// tests/CRM.API.Tests/Tickets/TicketsControllerGetTests.cs
using System.Net;
using System.Net.Http.Json;
using CRM.Application.Tickets.DTOs;
using CRM.Application.Tickets.Queries;
using MediatR;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using Xunit;

namespace CRM.API.Tests.Tickets;

public class TicketsControllerGetTests
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
    public async Task GetTicket_Existing_Returns200()
    {
        var id = Guid.NewGuid();
        _mediator.Setup(m => m.Send(It.Is<GetTicketQuery>(q => q.TicketId == id), default))
                 .ReturnsAsync(new TicketDetailDto(
                     id, "TKT-001", Guid.NewGuid(), "Ali Hassan",
                     "Cannot login", null, "Description goes here", null,
                     "New", "High", "Internal",
                     null, null, null, null, null, null,
                     DateTime.UtcNow, DateTime.UtcNow, null, null));

        var client = BuildClient();
        var response = await client.GetAsync($"/api/tickets/{id}");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<TicketDetailDto>();
        Assert.Equal("Cannot login", body!.Subject);
    }

    [Fact]
    public async Task GetTicket_NonExistent_Returns404()
    {
        var id = Guid.NewGuid();
        _mediator.Setup(m => m.Send(It.Is<GetTicketQuery>(q => q.TicketId == id), default))
                 .ThrowsAsync(new KeyNotFoundException("Not found."));

        var client = BuildClient();
        var response = await client.GetAsync($"/api/tickets/{id}");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }
}
```

- [ ] **Step 2: Run test to verify it fails**

```bash
dotnet test tests/CRM.API.Tests/ --filter "TicketsControllerGetTests" -v n
```

Expected: FAIL — `GetById` is a stub returning `Ok()`.

- [ ] **Step 3: Implement GetById in TicketsController**

```csharp
// Replace stub GetById in src/CRM.API/Controllers/TicketsController.cs:

[HttpGet("{id:guid}")]
public async Task<IActionResult> GetById(Guid id, CancellationToken ct)
{
    try
    {
        var result = await _mediator.Send(new GetTicketQuery(id), ct);
        return Ok(result);
    }
    catch (KeyNotFoundException ex)
    {
        return NotFound(new { error = ex.Message });
    }
}
```

- [ ] **Step 4: Run tests to verify they pass**

```bash
dotnet test tests/CRM.API.Tests/ --filter "TicketsControllerGetTests" -v n
```

Expected: 2 tests PASS.

- [ ] **Step 5: Commit**

```bash
git add src/CRM.API/Controllers/TicketsController.cs \
        tests/CRM.API.Tests/Tickets/TicketsControllerGetTests.cs
git commit -m "feat(api): implement GET /api/tickets/{id} endpoint"
```
