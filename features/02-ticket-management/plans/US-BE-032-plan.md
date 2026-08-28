# Get Ticket History — Implementation Plan

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

**Story:** US-BE-032  
**Goal:** Implement `GET /api/tickets/{id}/history` — returns the audit trail of all changes to a ticket, paginated and sorted by date descending.

**Architecture:** `GetTicketHistoryQuery(ticketId, page, pageSize)` → handler fetches `TicketHistory` records from `ITicketHistoryRepository`, enriches with user names, maps to `TicketHistoryEntryDto`. Admin/Manager/Agent only.

**Tech Stack:** .NET 10, ASP.NET Core, MediatR, EF Core, xUnit, Moq

---

## File Structure

| Action | Path |
|--------|------|
| Create | `src/CRM.Domain/Tickets/ITicketHistoryRepository.cs` |
| Create | `src/CRM.Application/Tickets/Queries/GetTicketHistoryQuery.cs` |
| Modify | `src/CRM.API/Controllers/TicketsController.cs` |
| Test   | `tests/CRM.Application.Tests/Tickets/GetTicketHistoryQueryHandlerTests.cs` |
| Test   | `tests/CRM.API.Tests/Tickets/TicketsControllerHistoryTests.cs` |

---

## Task 1: ITicketHistoryRepository + GetTicketHistory Query + Handler

**Files:**
- Create: `src/CRM.Domain/Tickets/ITicketHistoryRepository.cs`
- Create: `src/CRM.Application/Tickets/Queries/GetTicketHistoryQuery.cs`
- Test: `tests/CRM.Application.Tests/Tickets/GetTicketHistoryQueryHandlerTests.cs`

- [ ] **Step 1: Create repository interface**

```csharp
// src/CRM.Domain/Tickets/ITicketHistoryRepository.cs
using CRM.Application.Common;

namespace CRM.Domain.Tickets;

public interface ITicketHistoryRepository
{
    Task<PagedResult<TicketHistory>> ListByTicketAsync(
        Guid ticketId, int page, int pageSize, CancellationToken ct = default);
}
```

- [ ] **Step 2: Write the failing tests**

```csharp
// tests/CRM.Application.Tests/Tickets/GetTicketHistoryQueryHandlerTests.cs
using CRM.Application.Common;
using CRM.Application.Tickets.DTOs;
using CRM.Application.Tickets.Queries;
using CRM.Domain.Tickets;
using Moq;
using Xunit;

namespace CRM.Application.Tests.Tickets;

public class GetTicketHistoryQueryHandlerTests
{
    private readonly Mock<ITicketHistoryRepository> _repo = new();
    private readonly GetTicketHistoryQueryHandler _handler;

    public GetTicketHistoryQueryHandlerTests()
    {
        _handler = new GetTicketHistoryQueryHandler(_repo.Object);
    }

    [Fact]
    public async Task Handle_ReturnsPagedHistoryEntries()
    {
        var ticketId = Guid.NewGuid();
        var agentId = Guid.NewGuid();
        var entries = new List<TicketHistory>
        {
            TicketHistory.Create(ticketId, "Status", "New", "Assigned", agentId),
            TicketHistory.Create(ticketId, "Priority", "Low", "High", agentId)
        };

        _repo.Setup(r => r.ListByTicketAsync(ticketId, 1, 20, default))
             .ReturnsAsync(new PagedResult<TicketHistory>(entries, 2, 1, 20));

        var result = await _handler.Handle(
            new GetTicketHistoryQuery(ticketId, 1, 20), default);

        Assert.Equal(2, result.TotalCount);
        Assert.Equal("Status", result.Items[0].FieldChanged);
    }

    [Fact]
    public async Task Handle_EmptyHistory_ReturnsEmptyPage()
    {
        var ticketId = Guid.NewGuid();
        _repo.Setup(r => r.ListByTicketAsync(ticketId, 1, 20, default))
             .ReturnsAsync(new PagedResult<TicketHistory>(new List<TicketHistory>(), 0, 1, 20));

        var result = await _handler.Handle(
            new GetTicketHistoryQuery(ticketId, 1, 20), default);

        Assert.Equal(0, result.TotalCount);
        Assert.Empty(result.Items);
    }
}
```

- [ ] **Step 3: Run test to verify it fails**

```bash
dotnet test tests/CRM.Application.Tests/ --filter "GetTicketHistoryQueryHandlerTests" -v n
```

Expected: FAIL — `GetTicketHistoryQuery` does not exist yet.

- [ ] **Step 4: Implement query and handler**

```csharp
// src/CRM.Application/Tickets/Queries/GetTicketHistoryQuery.cs
using CRM.Application.Common;
using CRM.Application.Tickets.DTOs;
using CRM.Domain.Tickets;
using MediatR;

namespace CRM.Application.Tickets.Queries;

public record GetTicketHistoryQuery(
    Guid TicketId, int Page, int PageSize) : IRequest<PagedResult<TicketHistoryEntryDto>>;

public class GetTicketHistoryQueryHandler
    : IRequestHandler<GetTicketHistoryQuery, PagedResult<TicketHistoryEntryDto>>
{
    private readonly ITicketHistoryRepository _history;

    public GetTicketHistoryQueryHandler(ITicketHistoryRepository history) => _history = history;

    public async Task<PagedResult<TicketHistoryEntryDto>> Handle(
        GetTicketHistoryQuery query, CancellationToken ct)
    {
        var paged = await _history.ListByTicketAsync(query.TicketId, query.Page, query.PageSize, ct);

        var dtos = paged.Items
            .Select(h => new TicketHistoryEntryDto(
                h.FieldChanged, h.OldValue, h.NewValue,
                h.ChangedByUserId.ToString(), // Enriched with user name in infra
                h.ChangedAt))
            .ToList();

        return new PagedResult<TicketHistoryEntryDto>(dtos, paged.TotalCount, paged.Page, paged.PageSize);
    }
}
```

- [ ] **Step 5: Run tests to verify they pass**

```bash
dotnet test tests/CRM.Application.Tests/ --filter "GetTicketHistoryQueryHandlerTests" -v n
```

Expected: 2 tests PASS.

- [ ] **Step 6: Commit**

```bash
git add src/CRM.Domain/Tickets/ITicketHistoryRepository.cs \
        src/CRM.Application/Tickets/Queries/GetTicketHistoryQuery.cs \
        tests/CRM.Application.Tests/Tickets/GetTicketHistoryQueryHandlerTests.cs
git commit -m "feat(tickets): add GetTicketHistoryQuery"
```

---

## Task 2: TicketsController — GET /api/tickets/{id}/history

**Files:**
- Modify: `src/CRM.API/Controllers/TicketsController.cs`
- Test: `tests/CRM.API.Tests/Tickets/TicketsControllerHistoryTests.cs`

- [ ] **Step 1: Write the failing test**

```csharp
// tests/CRM.API.Tests/Tickets/TicketsControllerHistoryTests.cs
using System.Net;
using System.Net.Http.Json;
using CRM.Application.Common;
using CRM.Application.Tickets.DTOs;
using CRM.Application.Tickets.Queries;
using MediatR;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using Xunit;

namespace CRM.API.Tests.Tickets;

public class TicketsControllerHistoryTests
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
    public async Task GetHistory_Returns200WithPagedEntries()
    {
        var ticketId = Guid.NewGuid();
        var items = new List<TicketHistoryEntryDto>
        {
            new("Status", "New", "Assigned", "Ali Hassan", DateTime.UtcNow.AddHours(-1))
        };
        _mediator.Setup(m => m.Send(
            It.Is<GetTicketHistoryQuery>(q => q.TicketId == ticketId), default))
            .ReturnsAsync(new PagedResult<TicketHistoryEntryDto>(items, 1, 1, 20));

        var client = BuildClient();
        var response = await client.GetAsync($"/api/tickets/{ticketId}/history?page=1&pageSize=20");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<PagedResult<TicketHistoryEntryDto>>();
        Assert.Equal(1, body!.TotalCount);
    }
}
```

- [ ] **Step 2: Run test to verify it fails**

```bash
dotnet test tests/CRM.API.Tests/ --filter "TicketsControllerHistoryTests" -v n
```

Expected: FAIL — endpoint does not exist.

- [ ] **Step 3: Add GetHistory endpoint to TicketsController**

```csharp
// Add to src/CRM.API/Controllers/TicketsController.cs inside the class:

[HttpGet("{id:guid}/history")]
public async Task<IActionResult> GetHistory(
    Guid id,
    [FromQuery] int page = 1,
    [FromQuery] int pageSize = 20,
    CancellationToken ct = default)
{
    var result = await _mediator.Send(
        new GetTicketHistoryQuery(id, page, pageSize), ct);
    return Ok(result);
}
```

- [ ] **Step 4: Run tests to verify they pass**

```bash
dotnet test tests/CRM.API.Tests/ --filter "TicketsControllerHistoryTests" -v n
```

Expected: 1 test PASS.

- [ ] **Step 5: Commit**

```bash
git add src/CRM.API/Controllers/TicketsController.cs \
        tests/CRM.API.Tests/Tickets/TicketsControllerHistoryTests.cs
git commit -m "feat(api): add GET /api/tickets/{id}/history endpoint"
```
