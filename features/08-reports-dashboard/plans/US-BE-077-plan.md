# Dashboard KPIs Endpoint — Implementation Plan

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

**Story:** US-BE-077  
**Goal:** Implement `GET /api/dashboard/kpis` — returns a live KPI snapshot including openTickets, slaBreachRate, 7-day rolling avgFirstResponseMinutes/avgResolutionMinutes, 30-day rolling csatScore, agentUtilization, ticketsTodayCreated/Resolved, escalationRate, unassignedTickets, agentWorkload[]. Role-scoped: Admin (org-wide or by dept), Manager (their dept), Agent (personal KPIs, no agentWorkload). `calculatedAt` timestamp in response.

**Architecture:** `GetDashboardKpisQuery` delegates to `IDashboardRepository.GetKpisAsync(scope)`. Scope is computed by the handler based on role and optional `?departmentId`. `DashboardController` at `/api/dashboard`.

**Tech Stack:** .NET 10, ASP.NET Core, MediatR, EF Core, xUnit, Moq

---

## File Structure

| Action | Path |
|--------|------|
| Create | `src/CRM.Domain/Dashboard/IDashboardRepository.cs` |
| Create | `src/CRM.Application/Dashboard/DTOs/DashboardKpiDto.cs` |
| Create | `src/CRM.Application/Dashboard/Queries/GetDashboardKpisQuery.cs` |
| Create | `src/CRM.API/Controllers/DashboardController.cs` |
| Test   | `tests/CRM.Application.Tests/Dashboard/GetDashboardKpisQueryHandlerTests.cs` |
| Test   | `tests/CRM.API.Tests/Dashboard/DashboardControllerTests.cs` |

---

## Task 1: Dashboard KPIs Query

> Note: `IUserRepository.GetDepartmentIdsAsync` is added in US-BE-073. Implement that plan first.

- [ ] **Step 1: Write the failing tests**

```csharp
// tests/CRM.Application.Tests/Dashboard/GetDashboardKpisQueryHandlerTests.cs
using CRM.Application.Dashboard.Queries;
using CRM.Domain.Dashboard;
using CRM.Domain.Users;
using Moq;
using Xunit;

namespace CRM.Application.Tests.Dashboard;

public class GetDashboardKpisQueryHandlerTests
{
    private readonly Mock<IDashboardRepository> _repo = new();
    private readonly Mock<IUserRepository> _users = new();
    private readonly GetDashboardKpisQueryHandler _handler;

    public GetDashboardKpisQueryHandlerTests()
    {
        _handler = new GetDashboardKpisQueryHandler(_repo.Object, _users.Object);
    }

    [Fact]
    public async Task Handle_AdminNoFilter_ReturnsOrgWideKpis()
    {
        var adminId = Guid.NewGuid();

        _repo.Setup(r => r.GetKpisAsync(null, null, default))
             .ReturnsAsync(new DashboardKpiData(
                 120, new Dictionary<string, int> { ["Critical"] = 5 },
                 12.5m, 8.0m, 240.0m, 4.3m, 85.0m, 30, 28, 5.2m, 15,
                 new List<AgentWorkloadData>(), DateTime.UtcNow));

        var result = await _handler.Handle(
            new GetDashboardKpisQuery(adminId, UserRole.Admin, null),
            default);

        Assert.Equal(120, result.OpenTickets);
        Assert.NotNull(result.AgentWorkload);
    }

    [Fact]
    public async Task Handle_AgentRole_ReturnsPersonalKpisWithNoWorkloadArray()
    {
        var agentId = Guid.NewGuid();
        var deptId = Guid.NewGuid();

        _users.Setup(u => u.GetDepartmentIdsAsync(agentId, default))
              .ReturnsAsync(new List<Guid> { deptId });

        _repo.Setup(r => r.GetKpisAsync(It.IsAny<IReadOnlyList<Guid>?>(), agentId, default))
             .ReturnsAsync(new DashboardKpiData(
                 10, new Dictionary<string, int>(),
                 15.0m, 5.0m, 300.0m, 4.0m, 90.0m, 3, 2, 0.0m, 1,
                 new List<AgentWorkloadData>(), DateTime.UtcNow));

        var result = await _handler.Handle(
            new GetDashboardKpisQuery(agentId, UserRole.Agent, null),
            default);

        Assert.Null(result.AgentWorkload);
    }

    [Fact]
    public async Task Handle_ManagerNoFilter_ScopesToOwnDepartments()
    {
        var managerId = Guid.NewGuid();
        var deptId = Guid.NewGuid();

        _users.Setup(u => u.GetDepartmentIdsAsync(managerId, default))
              .ReturnsAsync(new List<Guid> { deptId });

        _repo.Setup(r => r.GetKpisAsync(
            It.Is<IReadOnlyList<Guid>?>(ids => ids != null && ids.Contains(deptId)),
            null, default))
             .ReturnsAsync(new DashboardKpiData(
                 40, new Dictionary<string, int>(),
                 11.0m, 6.0m, 200.0m, 4.1m, 88.0m, 10, 8, 3.0m, 5,
                 new List<AgentWorkloadData>(), DateTime.UtcNow));

        await _handler.Handle(
            new GetDashboardKpisQuery(managerId, UserRole.Manager, null),
            default);

        _users.Verify(u => u.GetDepartmentIdsAsync(managerId, default), Times.Once);
    }
}
```

- [ ] **Step 2: Run tests to verify they fail**

```bash
dotnet test tests/CRM.Application.Tests/ --filter "GetDashboardKpisQueryHandlerTests" -v n
```

Expected: FAIL — `GetDashboardKpisQuery` does not exist yet.

- [ ] **Step 3: Create IDashboardRepository**

```csharp
// src/CRM.Domain/Dashboard/IDashboardRepository.cs
namespace CRM.Domain.Dashboard;

public record AgentWorkloadData(
    Guid AgentId, string AgentName, int OpenTickets, string AvailabilityStatus);

public record DashboardKpiData(
    int OpenTickets,
    Dictionary<string, int> OpenByPriority,
    decimal SlaBreachRate,
    decimal AvgFirstResponseMinutes7Day,
    decimal AvgResolutionMinutes7Day,
    decimal? CsatScore30Day,
    decimal AgentUtilization,
    int TicketsTodayCreated,
    int TicketsTodayResolved,
    decimal EscalationRate,
    int UnassignedTickets,
    List<AgentWorkloadData> AgentWorkload,
    DateTime CalculatedAt);

public interface IDashboardRepository
{
    Task<DashboardKpiData> GetKpisAsync(
        IReadOnlyList<Guid>? departmentIds,
        Guid? agentId,
        CancellationToken ct = default);
}
```

- [ ] **Step 4: Create DashboardKpiDto**

```csharp
// src/CRM.Application/Dashboard/DTOs/DashboardKpiDto.cs
namespace CRM.Application.Dashboard.DTOs;

public record DashboardKpiDto(
    int OpenTickets,
    Dictionary<string, int> OpenByPriority,
    decimal SlaBreachRate,
    decimal AvgFirstResponseMinutes7Day,
    decimal AvgResolutionMinutes7Day,
    decimal? CsatScore30Day,
    decimal AgentUtilization,
    int TicketsTodayCreated,
    int TicketsTodayResolved,
    decimal EscalationRate,
    int UnassignedTickets,
    IReadOnlyList<AgentWorkloadDto>? AgentWorkload,
    DateTime CalculatedAt);

public record AgentWorkloadDto(
    Guid AgentId, string AgentName, int OpenTickets, string AvailabilityStatus);
```

- [ ] **Step 5: Implement GetDashboardKpisQuery**

```csharp
// src/CRM.Application/Dashboard/Queries/GetDashboardKpisQuery.cs
using CRM.Application.Dashboard.DTOs;
using CRM.Domain.Dashboard;
using CRM.Domain.Users;
using MediatR;

namespace CRM.Application.Dashboard.Queries;

public record GetDashboardKpisQuery(
    Guid RequestingUserId,
    UserRole RequestingUserRole,
    Guid? DepartmentId) : IRequest<DashboardKpiDto>;

public class GetDashboardKpisQueryHandler
    : IRequestHandler<GetDashboardKpisQuery, DashboardKpiDto>
{
    private readonly IDashboardRepository _dashboard;
    private readonly IUserRepository _users;

    public GetDashboardKpisQueryHandler(IDashboardRepository dashboard, IUserRepository users)
    {
        _dashboard = dashboard;
        _users = users;
    }

    public async Task<DashboardKpiDto> Handle(
        GetDashboardKpisQuery query, CancellationToken ct)
    {
        IReadOnlyList<Guid>? effectiveDepartmentIds = null;
        Guid? effectiveAgentId = null;
        bool includeWorkload = true;

        if (query.RequestingUserRole == UserRole.Agent)
        {
            var agentDeptIds = await _users.GetDepartmentIdsAsync(query.RequestingUserId, ct);
            effectiveDepartmentIds = agentDeptIds;
            effectiveAgentId = query.RequestingUserId;
            includeWorkload = false;
        }
        else if (query.RequestingUserRole == UserRole.Manager)
        {
            var managerDeptIds = await _users.GetDepartmentIdsAsync(query.RequestingUserId, ct);
            effectiveDepartmentIds = query.DepartmentId.HasValue
                ? new[] { query.DepartmentId.Value }
                : managerDeptIds;
        }
        else if (query.DepartmentId.HasValue)
        {
            effectiveDepartmentIds = new[] { query.DepartmentId.Value };
        }

        var data = await _dashboard.GetKpisAsync(
            effectiveDepartmentIds, effectiveAgentId, ct);

        return new DashboardKpiDto(
            data.OpenTickets,
            data.OpenByPriority,
            data.SlaBreachRate,
            data.AvgFirstResponseMinutes7Day,
            data.AvgResolutionMinutes7Day,
            data.CsatScore30Day,
            data.AgentUtilization,
            data.TicketsTodayCreated,
            data.TicketsTodayResolved,
            data.EscalationRate,
            data.UnassignedTickets,
            includeWorkload
                ? data.AgentWorkload.Select(w => new AgentWorkloadDto(
                    w.AgentId, w.AgentName, w.OpenTickets, w.AvailabilityStatus)).ToList()
                : null,
            data.CalculatedAt);
    }
}
```

- [ ] **Step 6: Run tests to verify they pass**

```bash
dotnet test tests/CRM.Application.Tests/ --filter "GetDashboardKpisQueryHandlerTests" -v n
```

Expected: 3 tests PASS.

- [ ] **Step 7: Create DashboardController**

```csharp
// src/CRM.API/Controllers/DashboardController.cs
using CRM.Application.Dashboard.Queries;
using CRM.Domain.Users;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace CRM.API.Controllers;

[ApiController]
[Route("api/dashboard")]
[Authorize]
public class DashboardController : ControllerBase
{
    private readonly IMediator _mediator;

    private Guid CurrentUserId =>
        Guid.Parse(User.FindFirst(ClaimTypes.NameIdentifier)!.Value);

    private UserRole CurrentUserRole
    {
        get
        {
            Enum.TryParse<UserRole>(
                User.FindFirst(ClaimTypes.Role)?.Value ?? "Agent", out var role);
            return role;
        }
    }

    public DashboardController(IMediator mediator) => _mediator = mediator;

    [HttpGet("kpis")]
    public async Task<IActionResult> Kpis(
        [FromQuery] Guid? departmentId,
        CancellationToken ct = default)
    {
        var result = await _mediator.Send(
            new GetDashboardKpisQuery(CurrentUserId, CurrentUserRole, departmentId), ct);
        return Ok(new { data = result });
    }
}
```

- [ ] **Step 8: Write controller test**

```csharp
// tests/CRM.API.Tests/Dashboard/DashboardControllerTests.cs
using System.Net;
using CRM.Application.Dashboard.DTOs;
using CRM.Application.Dashboard.Queries;
using MediatR;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using Xunit;

namespace CRM.API.Tests.Dashboard;

public class DashboardControllerTests
{
    private readonly Mock<IMediator> _mediator = new();

    private HttpClient BuildClient(string role = "Admin")
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
    public async Task Kpis_ValidRequest_Returns200()
    {
        _mediator.Setup(m => m.Send(It.IsAny<GetDashboardKpisQuery>(), default))
                 .ReturnsAsync(new DashboardKpiDto(
                     120, new Dictionary<string, int>(),
                     12.5m, 8.0m, 240.0m, 4.3m, 85.0m, 30, 28, 5.2m, 15,
                     new List<AgentWorkloadDto>(), DateTime.UtcNow));

        var response = await BuildClient().GetAsync("/api/dashboard/kpis");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }
}
```

- [ ] **Step 9: Run controller tests**

```bash
dotnet test tests/CRM.API.Tests/ --filter "DashboardControllerTests" -v n
```

Expected: 1 test PASS.

- [ ] **Step 10: Commit**

```bash
git add src/CRM.Domain/Dashboard/ \
        src/CRM.Application/Dashboard/ \
        src/CRM.API/Controllers/DashboardController.cs \
        tests/CRM.Application.Tests/Dashboard/GetDashboardKpisQueryHandlerTests.cs \
        tests/CRM.API.Tests/Dashboard/DashboardControllerTests.cs
git commit -m "feat(dashboard): add GET /api/dashboard/kpis — live KPI snapshot with role-scoped agentWorkload"
```
