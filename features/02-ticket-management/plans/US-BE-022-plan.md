# List Tickets — Implementation Plan

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

**Story:** US-BE-022  
**Goal:** Implement `GET /api/tickets` — returns a paginated, filterable ticket list. Agents see only their assigned tickets by default; Managers/Admins see all. Supports filters: status, priority, customerId, assignedToUserId, categoryId, search (subject/ticket number).

**Architecture:** `ListTicketsQuery(filters, page, pageSize, sortBy, sortDesc, requestingUserId, requestingUserRole)` → handler delegates to `ITicketRepository.ListAsync(filter)`. Role-scoping applied in handler: if Agent, adds `assignedToUserId = requestingUserId` unless override is explicitly allowed by role.

**Tech Stack:** .NET 10, ASP.NET Core, MediatR, EF Core, xUnit, Moq

---

## File Structure

| Action | Path |
|--------|------|
| Create | `src/CRM.Application/Tickets/Queries/ListTicketsQuery.cs` |
| Modify | `src/CRM.API/Controllers/TicketsController.cs` |
| Test   | `tests/CRM.Application.Tests/Tickets/ListTicketsQueryHandlerTests.cs` |
| Test   | `tests/CRM.API.Tests/Tickets/TicketsControllerListTests.cs` |

---

## Task 1: ListTickets Query + Handler

**Files:**
- Create: `src/CRM.Application/Tickets/Queries/ListTicketsQuery.cs`
- Test: `tests/CRM.Application.Tests/Tickets/ListTicketsQueryHandlerTests.cs`

- [ ] **Step 1: Write the failing tests**

```csharp
// tests/CRM.Application.Tests/Tickets/ListTicketsQueryHandlerTests.cs
using CRM.Application.Common;
using CRM.Application.Tickets.DTOs;
using CRM.Application.Tickets.Queries;
using CRM.Domain.Tickets;
using CRM.Domain.Tickets.Enums;
using CRM.Domain.Users;
using Moq;
using Xunit;

namespace CRM.Application.Tests.Tickets;

public class ListTicketsQueryHandlerTests
{
    private readonly Mock<ITicketRepository> _repo = new();
    private readonly ListTicketsQueryHandler _handler;

    public ListTicketsQueryHandlerTests()
    {
        _handler = new ListTicketsQueryHandler(_repo.Object);
    }

    [Fact]
    public async Task Handle_AdminRole_DoesNotForceAssigneeFilter()
    {
        var adminId = Guid.NewGuid();
        _repo.Setup(r => r.ListAsync(It.IsAny<TicketFilter>(), default))
             .ReturnsAsync(new PagedResult<TicketSummaryDto>(
                 new List<TicketSummaryDto>(), 0, 1, 20));

        await _handler.Handle(new ListTicketsQuery(
            null, null, null, null, null, 1, 20, "createdAt", false,
            adminId, UserRole.Admin), default);

        _repo.Verify(r => r.ListAsync(
            It.Is<TicketFilter>(f => f.AssignedToUserId == null), default), Times.Once);
    }

    [Fact]
    public async Task Handle_AgentRole_ForcesAssigneeFilterToSelf()
    {
        var agentId = Guid.NewGuid();
        _repo.Setup(r => r.ListAsync(It.IsAny<TicketFilter>(), default))
             .ReturnsAsync(new PagedResult<TicketSummaryDto>(
                 new List<TicketSummaryDto>(), 0, 1, 20));

        await _handler.Handle(new ListTicketsQuery(
            null, null, null, null, null, 1, 20, "createdAt", false,
            agentId, UserRole.Agent), default);

        _repo.Verify(r => r.ListAsync(
            It.Is<TicketFilter>(f => f.AssignedToUserId == agentId), default), Times.Once);
    }

    [Fact]
    public async Task Handle_StatusFilter_PassedToRepository()
    {
        _repo.Setup(r => r.ListAsync(It.IsAny<TicketFilter>(), default))
             .ReturnsAsync(new PagedResult<TicketSummaryDto>(
                 new List<TicketSummaryDto>(), 0, 1, 20));

        await _handler.Handle(new ListTicketsQuery(
            TicketStatus.New, null, null, null, null, 1, 20, "createdAt", false,
            Guid.NewGuid(), UserRole.Manager), default);

        _repo.Verify(r => r.ListAsync(
            It.Is<TicketFilter>(f => f.Status == TicketStatus.New), default), Times.Once);
    }
}
```

- [ ] **Step 2: Run test to verify it fails**

```bash
dotnet test tests/CRM.Application.Tests/ --filter "ListTicketsQueryHandlerTests" -v n
```

Expected: FAIL — `ListTicketsQuery` does not exist yet.

- [ ] **Step 3: Implement query and handler**

```csharp
// src/CRM.Application/Tickets/Queries/ListTicketsQuery.cs
using CRM.Application.Common;
using CRM.Application.Tickets.DTOs;
using CRM.Domain.Tickets;
using CRM.Domain.Tickets.Enums;
using CRM.Domain.Users;
using MediatR;

namespace CRM.Application.Tickets.Queries;

public record TicketFilter(
    TicketStatus? Status,
    TicketPriority? Priority,
    Guid? CustomerId,
    Guid? AssignedToUserId,
    Guid? CategoryId,
    string? Search,
    int Page,
    int PageSize,
    string SortBy,
    bool SortDesc);

public record ListTicketsQuery(
    TicketStatus? Status,
    TicketPriority? Priority,
    Guid? CustomerId,
    Guid? AssignedToUserId,
    Guid? CategoryId,
    int Page,
    int PageSize,
    string SortBy,
    bool SortDesc,
    Guid RequestingUserId,
    UserRole RequestingUserRole,
    string? Search = null) : IRequest<PagedResult<TicketSummaryDto>>;

public class ListTicketsQueryHandler
    : IRequestHandler<ListTicketsQuery, PagedResult<TicketSummaryDto>>
{
    private readonly ITicketRepository _tickets;

    public ListTicketsQueryHandler(ITicketRepository tickets) => _tickets = tickets;

    public async Task<PagedResult<TicketSummaryDto>> Handle(
        ListTicketsQuery query, CancellationToken ct)
    {
        // Agents can only see their own tickets
        var assignedToFilter = query.RequestingUserRole == UserRole.Agent
            ? query.RequestingUserId
            : query.AssignedToUserId;

        var filter = new TicketFilter(
            query.Status, query.Priority, query.CustomerId,
            assignedToFilter, query.CategoryId, query.Search,
            query.Page, query.PageSize, query.SortBy, query.SortDesc);

        return await _tickets.ListAsync(filter, ct);
    }
}
```

- [ ] **Step 4: Run tests to verify they pass**

```bash
dotnet test tests/CRM.Application.Tests/ --filter "ListTicketsQueryHandlerTests" -v n
```

Expected: 3 tests PASS.

- [ ] **Step 5: Commit**

```bash
git add src/CRM.Application/Tickets/Queries/ListTicketsQuery.cs \
        tests/CRM.Application.Tests/Tickets/ListTicketsQueryHandlerTests.cs
git commit -m "feat(tickets): add ListTicketsQuery with role-based scoping"
```

---

## Task 2: TicketsController — GET /api/tickets

**Files:**
- Modify: `src/CRM.API/Controllers/TicketsController.cs`
- Test: `tests/CRM.API.Tests/Tickets/TicketsControllerListTests.cs`

- [ ] **Step 1: Write the failing test**

```csharp
// tests/CRM.API.Tests/Tickets/TicketsControllerListTests.cs
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

public class TicketsControllerListTests
{
    private readonly Mock<IMediator> _mediator = new();

    private HttpClient BuildClient(string role = "Agent")
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
    public async Task ListTickets_Returns200WithPagedResult()
    {
        var items = new List<TicketSummaryDto>
        {
            new(Guid.NewGuid(), "TKT-001", Guid.NewGuid(), "Ali Hassan",
                "Cannot login", "New", "High", "Internal",
                null, null, DateTime.UtcNow, DateTime.UtcNow)
        };
        _mediator.Setup(m => m.Send(It.IsAny<ListTicketsQuery>(), default))
                 .ReturnsAsync(new PagedResult<TicketSummaryDto>(items, 1, 1, 20));

        var client = BuildClient();
        var response = await client.GetAsync("/api/tickets?page=1&pageSize=20");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<PagedResult<TicketSummaryDto>>();
        Assert.Equal(1, body!.TotalCount);
    }

    [Fact]
    public async Task ListTickets_Unauthenticated_Returns401()
    {
        var factory = new WebApplicationFactory<Program>();
        var response = await factory.CreateClient().GetAsync("/api/tickets");
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }
}
```

- [ ] **Step 2: Run test to verify it fails**

```bash
dotnet test tests/CRM.API.Tests/ --filter "TicketsControllerListTests" -v n
```

Expected: FAIL — `GET /api/tickets` does not exist yet.

- [ ] **Step 3: Add List endpoint to TicketsController**

```csharp
// Add to src/CRM.API/Controllers/TicketsController.cs inside the class:
// (Also add using CRM.Domain.Users; at top)

[HttpGet]
public async Task<IActionResult> List(
    [FromQuery] TicketStatus? status,
    [FromQuery] TicketPriority? priority,
    [FromQuery] Guid? customerId,
    [FromQuery] Guid? assignedToUserId,
    [FromQuery] Guid? categoryId,
    [FromQuery] string? search,
    [FromQuery] int page = 1,
    [FromQuery] int pageSize = 20,
    [FromQuery] string sortBy = "createdAt",
    [FromQuery] bool sortDesc = false,
    CancellationToken ct = default)
{
    var roleClaim = User.FindFirst(System.Security.Claims.ClaimTypes.Role)?.Value
        ?? "Agent";
    Enum.TryParse<UserRole>(roleClaim, out var role);

    var result = await _mediator.Send(new ListTicketsQuery(
        status, priority, customerId, assignedToUserId, categoryId,
        page, pageSize, sortBy, sortDesc, CurrentUserId, role, search), ct);

    return Ok(result);
}
```

- [ ] **Step 4: Run tests to verify they pass**

```bash
dotnet test tests/CRM.API.Tests/ --filter "TicketsControllerListTests" -v n
```

Expected: 2 tests PASS.

- [ ] **Step 5: Commit**

```bash
git add src/CRM.API/Controllers/TicketsController.cs \
        tests/CRM.API.Tests/Tickets/TicketsControllerListTests.cs
git commit -m "feat(api): add GET /api/tickets with role-scoped filtering"
```
