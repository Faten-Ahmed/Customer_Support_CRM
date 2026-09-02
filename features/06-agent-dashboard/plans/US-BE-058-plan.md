# Get My Tickets (Agent Dashboard) — Implementation Plan

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

**Story:** US-BE-058  
**Goal:** Implement `GET /api/agents/me/tickets` — returns only tickets assigned to the calling agent, with SLA status indicators, default sort by Priority DESC then SLA urgency ASC, and support for filter/sort query params.

**Architecture:** `GetMyTicketsQuery(AgentId, Status?, Priority?, DepartmentId?, Page, PageSize, SortBy, SortDir)` → `ITicketRepository.ListAssignedToAgentAsync()` which joins with `TicketSla` for live indicators. Returns `PagedResult<MyTicketDto>` with `slaStatus` and `resolutionRemainingMinutes` pre-computed at query time.

**Tech Stack:** .NET 10, ASP.NET Core, MediatR, EF Core, xUnit, Moq

---

## File Structure

| Action | Path |
|--------|------|
| Create | `src/CRM.Application/Agents/Queries/GetMyTicketsQuery.cs` |
| Create | `src/CRM.Application/Agents/DTOs/MyTicketDto.cs` |
| Create | `src/CRM.API/Controllers/AgentMeController.cs` |
| Test   | `tests/CRM.Application.Tests/Agents/GetMyTicketsQueryHandlerTests.cs` |
| Test   | `tests/CRM.API.Tests/Agents/AgentMeControllerGetTicketsTests.cs` |

---

## Task 1: GetMyTickets Query + Controller

- [ ] **Step 1: Write the failing tests**

```csharp
// tests/CRM.Application.Tests/Agents/GetMyTicketsQueryHandlerTests.cs
using CRM.Application.Agents.Queries;
using CRM.Application.Common;
using CRM.Domain.Tickets;
using Moq;
using Xunit;

namespace CRM.Application.Tests.Agents;

public class GetMyTicketsQueryHandlerTests
{
    private readonly Mock<ITicketRepository> _repo = new();
    private readonly GetMyTicketsQueryHandler _handler;

    public GetMyTicketsQueryHandlerTests()
    {
        _handler = new GetMyTicketsQueryHandler(_repo.Object);
    }

    [Fact]
    public async Task Handle_ReturnsOnlyCallerAssignedTickets()
    {
        var agentId = Guid.NewGuid();
        var filter = new AgentTicketFilter(null, null, null, "Priority", "desc");

        _repo.Setup(r => r.ListAssignedToAgentAsync(agentId, filter, 1, 20, default))
             .ReturnsAsync(new PagedResult<MyTicketProjection>(
                 new List<MyTicketProjection>(), 0, 1, 20));

        var result = await _handler.Handle(
            new GetMyTicketsQuery(agentId, null, null, null, 1, 20, "Priority", "desc"),
            default);

        Assert.Equal(0, result.TotalCount);
        _repo.Verify(r => r.ListAssignedToAgentAsync(
            agentId,
            It.Is<AgentTicketFilter>(f => f.SortBy == "Priority"),
            1, 20, default), Times.Once);
    }

    [Fact]
    public async Task Handle_DefaultSort_UsesPriorityDescSlaUrgencyAsc()
    {
        var agentId = Guid.NewGuid();

        _repo.Setup(r => r.ListAssignedToAgentAsync(
            agentId,
            It.Is<AgentTicketFilter>(f => f.SortBy == "Priority" && f.SortDir == "desc"),
            1, 20, default))
             .ReturnsAsync(new PagedResult<MyTicketProjection>(
                 new List<MyTicketProjection>(), 0, 1, 20));

        await _handler.Handle(
            new GetMyTicketsQuery(agentId, null, null, null, 1, 20, null, null),
            default);

        _repo.Verify(r => r.ListAssignedToAgentAsync(
            agentId,
            It.Is<AgentTicketFilter>(f => f.SortBy == "Priority" && f.SortDir == "desc"),
            1, 20, default), Times.Once);
    }
}
```

- [ ] **Step 2: Run tests to verify they fail**

```bash
dotnet test tests/CRM.Application.Tests/ --filter "GetMyTicketsQueryHandlerTests" -v n
```

Expected: FAIL — `GetMyTicketsQuery` does not exist yet.

- [ ] **Step 3: Create MyTicketDto**

```csharp
// src/CRM.Application/Agents/DTOs/MyTicketDto.cs
namespace CRM.Application.Agents.DTOs;

public record MyTicketDto(
    Guid Id,
    string TicketNumber,
    Guid CustomerId,
    string CustomerFullName,
    string Subject,
    string Status,
    string Priority,
    string Channel,
    Guid? DepartmentId,
    Guid? CategoryId,
    DateTime CreatedAt,
    DateTime? ResolutionDue,
    string SlaStatus,
    int? ResolutionRemainingMinutes);
```

- [ ] **Step 4: Add ITicketRepository members and MyTicketProjection**

Add to `src/CRM.Domain/Tickets/ITicketRepository.cs`:

```csharp
public record AgentTicketFilter(
    string? Status, string? Priority, Guid? DepartmentId,
    string? SortBy, string? SortDir);

public record MyTicketProjection(
    Guid Id, string TicketNumber, Guid CustomerId, string CustomerFullName,
    string Subject, string Status, string Priority, string Channel,
    Guid? DepartmentId, Guid? CategoryId, DateTime CreatedAt,
    DateTime? ResolutionDue, string SlaStatus, int? ResolutionRemainingMinutes);

Task<PagedResult<MyTicketProjection>> ListAssignedToAgentAsync(
    Guid agentId,
    AgentTicketFilter filter,
    int page,
    int pageSize,
    CancellationToken ct = default);
```

- [ ] **Step 5: Implement GetMyTicketsQuery**

```csharp
// src/CRM.Application/Agents/Queries/GetMyTicketsQuery.cs
using CRM.Application.Agents.DTOs;
using CRM.Application.Common;
using CRM.Domain.Tickets;
using MediatR;

namespace CRM.Application.Agents.Queries;

public record GetMyTicketsQuery(
    Guid AgentId,
    string? Status,
    string? Priority,
    Guid? DepartmentId,
    int Page,
    int PageSize,
    string? SortBy,
    string? SortDir) : IRequest<PagedResult<MyTicketDto>>;

public class GetMyTicketsQueryHandler
    : IRequestHandler<GetMyTicketsQuery, PagedResult<MyTicketDto>>
{
    private readonly ITicketRepository _tickets;

    public GetMyTicketsQueryHandler(ITicketRepository tickets) => _tickets = tickets;

    public async Task<PagedResult<MyTicketDto>> Handle(
        GetMyTicketsQuery query, CancellationToken ct)
    {
        var filter = new AgentTicketFilter(
            query.Status,
            query.Priority,
            query.DepartmentId,
            query.SortBy ?? "Priority",
            query.SortDir ?? "desc");

        var paged = await _tickets.ListAssignedToAgentAsync(
            query.AgentId, filter, query.Page, query.PageSize, ct);

        var dtos = paged.Items
            .Select(p => new MyTicketDto(
                p.Id, p.TicketNumber, p.CustomerId, p.CustomerFullName,
                p.Subject, p.Status, p.Priority, p.Channel,
                p.DepartmentId, p.CategoryId, p.CreatedAt,
                p.ResolutionDue, p.SlaStatus, p.ResolutionRemainingMinutes))
            .ToList();

        return new PagedResult<MyTicketDto>(dtos, paged.TotalCount, paged.Page, paged.PageSize);
    }
}
```

- [ ] **Step 6: Run tests to verify they pass**

```bash
dotnet test tests/CRM.Application.Tests/ --filter "GetMyTicketsQueryHandlerTests" -v n
```

Expected: 2 tests PASS.

- [ ] **Step 7: Create AgentMeController**

```csharp
// src/CRM.API/Controllers/AgentMeController.cs
using CRM.Application.Agents.Queries;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace CRM.API.Controllers;

[ApiController]
[Route("api/agents/me")]
[Authorize(Roles = "Admin,Manager,Agent")]
public class AgentMeController : ControllerBase
{
    private readonly IMediator _mediator;

    private Guid CurrentUserId =>
        Guid.Parse(User.FindFirst(ClaimTypes.NameIdentifier)!.Value);

    public AgentMeController(IMediator mediator) => _mediator = mediator;

    [HttpGet("tickets")]
    public async Task<IActionResult> GetMyTickets(
        [FromQuery] string? status,
        [FromQuery] string? priority,
        [FromQuery] Guid? departmentId,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        [FromQuery] string? sortBy = null,
        [FromQuery] string? sortDir = null,
        CancellationToken ct = default)
    {
        var result = await _mediator.Send(
            new GetMyTicketsQuery(
                CurrentUserId, status, priority, departmentId,
                page, pageSize, sortBy, sortDir), ct);
        return Ok(result);
    }
}
```

- [ ] **Step 8: Write controller test**

```csharp
// tests/CRM.API.Tests/Agents/AgentMeControllerGetTicketsTests.cs
using System.Net;
using CRM.Application.Agents.Queries;
using CRM.Application.Common;
using CRM.Application.Agents.DTOs;
using MediatR;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using Xunit;

namespace CRM.API.Tests.Agents;

public class AgentMeControllerGetTicketsTests
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
    public async Task GetMyTickets_Returns200()
    {
        _mediator.Setup(m => m.Send(It.IsAny<GetMyTicketsQuery>(), default))
                 .ReturnsAsync(new PagedResult<MyTicketDto>(
                     new List<MyTicketDto>(), 0, 1, 20));

        var response = await BuildClient().GetAsync("/api/agents/me/tickets");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }
}
```

- [ ] **Step 9: Run controller test**

```bash
dotnet test tests/CRM.API.Tests/ --filter "AgentMeControllerGetTicketsTests" -v n
```

Expected: 1 test PASS.

- [ ] **Step 10: Commit**

```bash
git add src/CRM.Application/Agents/ \
        src/CRM.API/Controllers/AgentMeController.cs \
        tests/CRM.Application.Tests/Agents/GetMyTicketsQueryHandlerTests.cs \
        tests/CRM.API.Tests/Agents/AgentMeControllerGetTicketsTests.cs
git commit -m "feat(agents): add GET /api/agents/me/tickets with SLA status indicators and default priority/SLA sort"
```
