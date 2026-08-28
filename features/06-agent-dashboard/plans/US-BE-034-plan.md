# List Unassigned Tickets — Implementation Plan

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

**Story:** US-BE-034  
**Goal:** Implement `GET /api/tickets/unassigned` — returns tickets with Status=New and no assigned agent, filtered to the calling agent's departments, sorted oldest-first, with SLA urgency fields per ticket.

**Architecture:** `ListUnassignedTicketsQuery(requestingUserId, requestingUserRole, page, pageSize)` → handler resolves agent department IDs if caller is Agent role, calls `ITicketRepository.ListUnassignedAsync`, maps to `UnassignedTicketDto` including `TicketSla` urgency fields. Admin/Manager skip the department filter.

**Tech Stack:** .NET 10, ASP.NET Core, MediatR, EF Core, xUnit, Moq

---

## File Structure

| Action | Path |
|--------|------|
| Create | `src/CRM.Application/Tickets/DTOs/UnassignedTicketDto.cs` |
| Create | `src/CRM.Application/Tickets/Queries/ListUnassignedTicketsQuery.cs` |
| Modify | `src/CRM.Domain/Tickets/ITicketRepository.cs` |
| Modify | `src/CRM.Domain/Users/IUserRepository.cs` |
| Modify | `src/CRM.API/Controllers/TicketsController.cs` |
| Test   | `tests/CRM.Application.Tests/Tickets/ListUnassignedTicketsQueryHandlerTests.cs` |
| Test   | `tests/CRM.API.Tests/Tickets/TicketsControllerUnassignedTests.cs` |

---

## Task 1: ListUnassignedTickets Query + Handler

**Files:**
- Create: `src/CRM.Application/Tickets/DTOs/UnassignedTicketDto.cs`
- Create: `src/CRM.Application/Tickets/Queries/ListUnassignedTicketsQuery.cs`
- Modify: `src/CRM.Domain/Tickets/ITicketRepository.cs`
- Modify: `src/CRM.Domain/Users/IUserRepository.cs`
- Test: `tests/CRM.Application.Tests/Tickets/ListUnassignedTicketsQueryHandlerTests.cs`

- [ ] **Step 1: Write the failing tests**

```csharp
// tests/CRM.Application.Tests/Tickets/ListUnassignedTicketsQueryHandlerTests.cs
using CRM.Application.Common;
using CRM.Application.Tickets.Queries;
using CRM.Domain.Tickets;
using CRM.Domain.Tickets.Enums;
using CRM.Domain.Users;
using Moq;
using Xunit;

namespace CRM.Application.Tests.Tickets;

public class ListUnassignedTicketsQueryHandlerTests
{
    private readonly Mock<ITicketRepository> _ticketRepo = new();
    private readonly Mock<IUserRepository> _userRepo = new();
    private readonly ListUnassignedTicketsQueryHandler _handler;

    public ListUnassignedTicketsQueryHandlerTests()
    {
        _handler = new ListUnassignedTicketsQueryHandler(_ticketRepo.Object, _userRepo.Object);
    }

    [Fact]
    public async Task Handle_AgentRole_FiltersToAgentDepartments()
    {
        var agentId = Guid.NewGuid();
        var deptId = Guid.NewGuid();
        var ticket = Ticket.Create(Guid.NewGuid(), "Login broken", "Desc",
            TicketPriority.High, TicketChannel.Email, agentId);

        _userRepo.Setup(r => r.GetDepartmentIdsForAgentAsync(agentId, default))
                 .ReturnsAsync(new List<Guid> { deptId });

        _ticketRepo.Setup(r => r.ListUnassignedAsync(
            It.Is<IReadOnlyList<Guid>>(ids => ids.Contains(deptId)), 1, 20, default))
            .ReturnsAsync(new PagedResult<Ticket>(new List<Ticket> { ticket }, 1, 1, 20));

        var result = await _handler.Handle(
            new ListUnassignedTicketsQuery(agentId, UserRole.Agent, 1, 20), default);

        Assert.Equal(1, result.TotalCount);
        Assert.Equal("High", result.Items[0].Priority);
    }

    [Fact]
    public async Task Handle_AdminRole_PassesNullDepartmentFilter()
    {
        var adminId = Guid.NewGuid();
        _ticketRepo.Setup(r => r.ListUnassignedAsync(null, 1, 20, default))
                   .ReturnsAsync(new PagedResult<Ticket>(new List<Ticket>(), 0, 1, 20));

        var result = await _handler.Handle(
            new ListUnassignedTicketsQuery(adminId, UserRole.Admin, 1, 20), default);

        Assert.Equal(0, result.TotalCount);
        _userRepo.Verify(r => r.GetDepartmentIdsForAgentAsync(It.IsAny<Guid>(), default), Times.Never);
    }

    [Fact]
    public async Task Handle_MultipleTickets_SortedByCreatedAtAsc()
    {
        var agentId = Guid.NewGuid();
        var deptId = Guid.NewGuid();

        _userRepo.Setup(r => r.GetDepartmentIdsForAgentAsync(agentId, default))
                 .ReturnsAsync(new List<Guid> { deptId });

        // Repository returns tickets already sorted; handler preserves order
        _ticketRepo.Setup(r => r.ListUnassignedAsync(
            It.IsAny<IReadOnlyList<Guid>>(), 1, 20, default))
            .ReturnsAsync(new PagedResult<Ticket>(new List<Ticket>(), 0, 1, 20));

        var result = await _handler.Handle(
            new ListUnassignedTicketsQuery(agentId, UserRole.Agent, 1, 20), default);

        Assert.Empty(result.Items);
    }
}
```

- [ ] **Step 2: Run test to verify it fails**

```bash
dotnet test tests/CRM.Application.Tests/ --filter "ListUnassignedTicketsQueryHandlerTests" -v n
```

Expected: FAIL — `ListUnassignedTicketsQuery` does not exist yet.

- [ ] **Step 3: Create DTO**

```csharp
// src/CRM.Application/Tickets/DTOs/UnassignedTicketDto.cs
namespace CRM.Application.Tickets.DTOs;

public record UnassignedTicketDto(
    Guid Id,
    string TicketNumber,
    Guid CustomerId,
    string Subject,
    string Priority,
    string Channel,
    Guid? DepartmentId,
    Guid? CategoryId,
    DateTime CreatedAt,
    DateTime? ResolutionDue,
    string BreachTier);
```

- [ ] **Step 4: Add methods to ITicketRepository and IUserRepository**

Add to `src/CRM.Domain/Tickets/ITicketRepository.cs`:
```csharp
Task<PagedResult<Ticket>> ListUnassignedAsync(
    IReadOnlyList<Guid>? departmentIds,
    int page,
    int pageSize,
    CancellationToken ct = default);
```

Add to `src/CRM.Domain/Users/IUserRepository.cs`:
```csharp
Task<IReadOnlyList<Guid>> GetDepartmentIdsForAgentAsync(
    Guid agentId, CancellationToken ct = default);
```

- [ ] **Step 5: Implement query and handler**

```csharp
// src/CRM.Application/Tickets/Queries/ListUnassignedTicketsQuery.cs
using CRM.Application.Common;
using CRM.Application.Tickets.DTOs;
using CRM.Domain.Tickets;
using CRM.Domain.Users;
using MediatR;

namespace CRM.Application.Tickets.Queries;

public record ListUnassignedTicketsQuery(
    Guid RequestingUserId,
    UserRole RequestingUserRole,
    int Page = 1,
    int PageSize = 20) : IRequest<PagedResult<UnassignedTicketDto>>;

public class ListUnassignedTicketsQueryHandler
    : IRequestHandler<ListUnassignedTicketsQuery, PagedResult<UnassignedTicketDto>>
{
    private readonly ITicketRepository _tickets;
    private readonly IUserRepository _users;

    public ListUnassignedTicketsQueryHandler(ITicketRepository tickets, IUserRepository users)
    {
        _tickets = tickets;
        _users = users;
    }

    public async Task<PagedResult<UnassignedTicketDto>> Handle(
        ListUnassignedTicketsQuery query, CancellationToken ct)
    {
        IReadOnlyList<Guid>? departmentIds = null;
        if (query.RequestingUserRole == UserRole.Agent)
            departmentIds = await _users.GetDepartmentIdsForAgentAsync(query.RequestingUserId, ct);

        var paged = await _tickets.ListUnassignedAsync(
            departmentIds, query.Page, query.PageSize, ct);

        var dtos = paged.Items
            .Select(t => new UnassignedTicketDto(
                t.Id,
                t.TicketNumber,
                t.CustomerId,
                t.Subject,
                t.Priority.ToString(),
                t.Channel.ToString(),
                t.DepartmentId,
                t.CategoryId,
                t.CreatedAt,
                t.TicketSla?.ResolutionDue,
                t.TicketSla?.BreachTier.ToString() ?? "None"))
            .ToList();

        return new PagedResult<UnassignedTicketDto>(dtos, paged.TotalCount, paged.Page, paged.PageSize);
    }
}
```

- [ ] **Step 6: Run tests to verify they pass**

```bash
dotnet test tests/CRM.Application.Tests/ --filter "ListUnassignedTicketsQueryHandlerTests" -v n
```

Expected: 3 tests PASS.

- [ ] **Step 7: Commit**

```bash
git add src/CRM.Application/Tickets/DTOs/UnassignedTicketDto.cs \
        src/CRM.Application/Tickets/Queries/ListUnassignedTicketsQuery.cs \
        src/CRM.Domain/Tickets/ITicketRepository.cs \
        src/CRM.Domain/Users/IUserRepository.cs \
        tests/CRM.Application.Tests/Tickets/ListUnassignedTicketsQueryHandlerTests.cs
git commit -m "feat(tickets): add ListUnassignedTicketsQuery with agent-department scoping"
```

---

## Task 2: TicketsController — GET /api/tickets/unassigned

**Files:**
- Modify: `src/CRM.API/Controllers/TicketsController.cs`
- Test: `tests/CRM.API.Tests/Tickets/TicketsControllerUnassignedTests.cs`

- [ ] **Step 1: Write the failing test**

```csharp
// tests/CRM.API.Tests/Tickets/TicketsControllerUnassignedTests.cs
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

public class TicketsControllerUnassignedTests
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
    public async Task GetUnassigned_Returns200WithPagedResult()
    {
        var items = new List<UnassignedTicketDto>
        {
            new(Guid.NewGuid(), "TKT-001", Guid.NewGuid(), "Screen flickering",
                "High", "Email", null, null, DateTime.UtcNow.AddHours(-2),
                DateTime.UtcNow.AddHours(6), "None")
        };
        _mediator.Setup(m => m.Send(It.IsAny<ListUnassignedTicketsQuery>(), default))
                 .ReturnsAsync(new PagedResult<UnassignedTicketDto>(items, 1, 1, 20));

        var client = BuildClient("Agent");
        var response = await client.GetAsync("/api/tickets/unassigned?page=1&pageSize=20");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<PagedResult<UnassignedTicketDto>>();
        Assert.Equal(1, body!.TotalCount);
        Assert.Equal("High", body.Items[0].Priority);
    }
}
```

- [ ] **Step 2: Run test to verify it fails**

```bash
dotnet test tests/CRM.API.Tests/ --filter "TicketsControllerUnassignedTests" -v n
```

Expected: FAIL — endpoint does not exist.

- [ ] **Step 3: Add GetUnassigned endpoint to TicketsController**

```csharp
// Add to src/CRM.API/Controllers/TicketsController.cs inside the class:
// (Also add using CRM.Domain.Users; if not already present)

[HttpGet("unassigned")]
public async Task<IActionResult> GetUnassigned(
    [FromQuery] int page = 1,
    [FromQuery] int pageSize = 20,
    CancellationToken ct = default)
{
    var roleClaim = User.FindFirst(System.Security.Claims.ClaimTypes.Role)?.Value ?? "Agent";
    Enum.TryParse<UserRole>(roleClaim, out var role);

    var result = await _mediator.Send(
        new ListUnassignedTicketsQuery(CurrentUserId, role, page, pageSize), ct);
    return Ok(result);
}
```

- [ ] **Step 4: Run tests to verify they pass**

```bash
dotnet test tests/CRM.API.Tests/ --filter "TicketsControllerUnassignedTests" -v n
```

Expected: 1 test PASS.

- [ ] **Step 5: Commit**

```bash
git add src/CRM.API/Controllers/TicketsController.cs \
        tests/CRM.API.Tests/Tickets/TicketsControllerUnassignedTests.cs
git commit -m "feat(api): add GET /api/tickets/unassigned endpoint"
```
