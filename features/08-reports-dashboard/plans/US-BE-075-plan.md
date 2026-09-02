# Agent Performance Report — Implementation Plan

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

**Story:** US-BE-075  
**Goal:** Implement `GET /api/reports/agents` — returns per-agent performance metrics (ticketsHandled, ticketsResolved, avgFirstResponseMinutes, avgResolutionMinutes, slaComplianceRate, csatScore, csatResponseCount, escalationRate). Admin/Manager only; Agent role returns 403. Agents with zero tickets in the period are excluded. `csatScore = null` when `csatResponseCount = 0`.

**Architecture:** `AgentPerformanceReportQuery` adds `GetAgentPerformanceAsync` to `IReportRepository`. Manager sees their department only; cross-department returns 403. Adds `agents` action to `ReportsController`.

**Tech Stack:** .NET 10, ASP.NET Core, MediatR, EF Core, xUnit, Moq

---

## File Structure

| Action | Path |
|--------|------|
| Modify | `src/CRM.Domain/Reports/IReportRepository.cs` |
| Create | `src/CRM.Application/Reports/DTOs/AgentPerformanceReportDto.cs` |
| Create | `src/CRM.Application/Reports/Queries/AgentPerformanceReportQuery.cs` |
| Modify | `src/CRM.API/Controllers/ReportsController.cs` |
| Test   | `tests/CRM.Application.Tests/Reports/AgentPerformanceReportQueryHandlerTests.cs` |

---

## Task 1: Agent Performance Report Query

> Note: `IReportRepository`, `ReportsController`, and `IUserRepository` additions are from US-BE-073/074. Implement those plans first.

- [ ] **Step 1: Write the failing tests**

```csharp
// tests/CRM.Application.Tests/Reports/AgentPerformanceReportQueryHandlerTests.cs
using CRM.Application.Reports.Queries;
using CRM.Domain.Reports;
using CRM.Domain.Users;
using FluentValidation;
using Moq;
using Xunit;

namespace CRM.Application.Tests.Reports;

public class AgentPerformanceReportQueryHandlerTests
{
    private readonly Mock<IReportRepository> _repo = new();
    private readonly Mock<IUserRepository> _users = new();
    private readonly AgentPerformanceReportQueryHandler _handler;

    public AgentPerformanceReportQueryHandlerTests()
    {
        _handler = new AgentPerformanceReportQueryHandler(_repo.Object, _users.Object);
    }

    [Fact]
    public async Task Handle_AdminNoFilter_ReturnsAgentList()
    {
        var adminId = Guid.NewGuid();
        var from = new DateTime(2025, 10, 1);
        var to = new DateTime(2025, 10, 31);
        var agentId = Guid.NewGuid();

        _repo.Setup(r => r.GetAgentPerformanceAsync(from, to, null, default))
             .ReturnsAsync(new List<AgentPerformanceData>
             {
                 new(agentId, "Alice Smith", 45, 40, 15.3m, 240.1m, 93.5m, 4.2m, 2, 5.1m)
             });

        var result = await _handler.Handle(
            new AgentPerformanceReportQuery(from, to, adminId, UserRole.Admin, null),
            default);

        Assert.Single(result);
        Assert.Equal("Alice Smith", result[0].AgentName);
        Assert.Equal(45, result[0].TicketsHandled);
    }

    [Fact]
    public async Task Handle_AgentRole_ThrowsUnauthorizedAccessException()
    {
        var agentId = Guid.NewGuid();
        var from = new DateTime(2025, 10, 1);
        var to = new DateTime(2025, 10, 31);

        await Assert.ThrowsAsync<UnauthorizedAccessException>(() =>
            _handler.Handle(
                new AgentPerformanceReportQuery(from, to, agentId, UserRole.Agent, null),
                default));
    }

    [Fact]
    public async Task Handle_DateRangeExceeds365Days_ThrowsValidationException()
    {
        var adminId = Guid.NewGuid();
        var from = new DateTime(2025, 1, 1);
        var to = from.AddDays(366);

        await Assert.ThrowsAsync<ValidationException>(() =>
            _handler.Handle(
                new AgentPerformanceReportQuery(from, to, adminId, UserRole.Admin, null),
                default));
    }

    [Fact]
    public async Task Handle_NoCsatResponses_CsatScoreIsNull()
    {
        var adminId = Guid.NewGuid();
        var agentId = Guid.NewGuid();
        var from = new DateTime(2025, 10, 1);
        var to = new DateTime(2025, 10, 31);

        _repo.Setup(r => r.GetAgentPerformanceAsync(from, to, null, default))
             .ReturnsAsync(new List<AgentPerformanceData>
             {
                 new(agentId, "Bob Jones", 10, 8, 20.0m, 300.0m, 80.0m, null, 0, 2.0m)
             });

        var result = await _handler.Handle(
            new AgentPerformanceReportQuery(from, to, adminId, UserRole.Admin, null),
            default);

        Assert.Null(result[0].CsatScore);
        Assert.Equal(0, result[0].CsatResponseCount);
    }

    [Fact]
    public async Task Handle_ManagerCrossDepartmentFilter_ThrowsUnauthorizedAccessException()
    {
        var managerId = Guid.NewGuid();
        var managerDeptId = Guid.NewGuid();
        var otherDeptId = Guid.NewGuid();
        var from = new DateTime(2025, 10, 1);
        var to = new DateTime(2025, 10, 31);

        _users.Setup(u => u.GetDepartmentIdsAsync(managerId, default))
              .ReturnsAsync(new List<Guid> { managerDeptId });

        await Assert.ThrowsAsync<UnauthorizedAccessException>(() =>
            _handler.Handle(
                new AgentPerformanceReportQuery(from, to, managerId, UserRole.Manager, otherDeptId),
                default));
    }
}
```

- [ ] **Step 2: Run tests to verify they fail**

```bash
dotnet test tests/CRM.Application.Tests/ --filter "AgentPerformanceReportQueryHandlerTests" -v n
```

Expected: FAIL — `AgentPerformanceReportQuery` does not exist yet.

- [ ] **Step 3: Add GetAgentPerformanceAsync to IReportRepository**

Open `src/CRM.Domain/Reports/IReportRepository.cs` and add:

```csharp
public record AgentPerformanceData(
    Guid AgentId,
    string AgentName,
    int TicketsHandled,
    int TicketsResolved,
    decimal AvgFirstResponseMinutes,
    decimal AvgResolutionMinutes,
    decimal SlaComplianceRate,
    decimal? CsatScore,
    int CsatResponseCount,
    decimal EscalationRate);
```

Add to `IReportRepository` interface:

```csharp
Task<IReadOnlyList<AgentPerformanceData>> GetAgentPerformanceAsync(
    DateTime dateFrom, DateTime dateTo,
    IReadOnlyList<Guid>? departmentIds,
    CancellationToken ct = default);
```

- [ ] **Step 4: Create AgentPerformanceReportDto**

```csharp
// src/CRM.Application/Reports/DTOs/AgentPerformanceReportDto.cs
namespace CRM.Application.Reports.DTOs;

public record AgentPerformanceDto(
    Guid AgentId,
    string AgentName,
    int TicketsHandled,
    int TicketsResolved,
    decimal AvgFirstResponseMinutes,
    decimal AvgResolutionMinutes,
    decimal SlaComplianceRate,
    decimal? CsatScore,
    int CsatResponseCount,
    decimal EscalationRate);
```

- [ ] **Step 5: Implement AgentPerformanceReportQuery**

```csharp
// src/CRM.Application/Reports/Queries/AgentPerformanceReportQuery.cs
using CRM.Application.Reports.DTOs;
using CRM.Domain.Reports;
using CRM.Domain.Users;
using FluentValidation;
using FluentValidation.Results;
using MediatR;

namespace CRM.Application.Reports.Queries;

public record AgentPerformanceReportQuery(
    DateTime DateFrom,
    DateTime DateTo,
    Guid RequestingUserId,
    UserRole RequestingUserRole,
    Guid? DepartmentId) : IRequest<IReadOnlyList<AgentPerformanceDto>>;

public class AgentPerformanceReportQueryHandler
    : IRequestHandler<AgentPerformanceReportQuery, IReadOnlyList<AgentPerformanceDto>>
{
    private readonly IReportRepository _reports;
    private readonly IUserRepository _users;

    public AgentPerformanceReportQueryHandler(IReportRepository reports, IUserRepository users)
    {
        _reports = reports;
        _users = users;
    }

    public async Task<IReadOnlyList<AgentPerformanceDto>> Handle(
        AgentPerformanceReportQuery query, CancellationToken ct)
    {
        if (query.RequestingUserRole == UserRole.Agent)
            throw new UnauthorizedAccessException(
                "Agents are not permitted to view the agent performance report.");

        if ((query.DateTo - query.DateFrom).TotalDays > 365)
            throw new ValidationException(new[]
            {
                new ValidationFailure("DateRange",
                    "Date range cannot exceed 365 days.", "DATE_RANGE_TOO_LARGE")
            });

        IReadOnlyList<Guid>? effectiveDepartmentIds = null;

        if (query.RequestingUserRole == UserRole.Manager)
        {
            var managerDeptIds = await _users.GetDepartmentIdsAsync(query.RequestingUserId, ct);
            if (query.DepartmentId.HasValue)
            {
                if (!managerDeptIds.Contains(query.DepartmentId.Value))
                    throw new UnauthorizedAccessException(
                        "Managers can only view reports for their own departments.");
                effectiveDepartmentIds = new[] { query.DepartmentId.Value };
            }
            else
            {
                effectiveDepartmentIds = managerDeptIds;
            }
        }
        else if (query.DepartmentId.HasValue)
        {
            effectiveDepartmentIds = new[] { query.DepartmentId.Value };
        }

        var data = await _reports.GetAgentPerformanceAsync(
            query.DateFrom, query.DateTo, effectiveDepartmentIds, ct);

        return data.Select(d => new AgentPerformanceDto(
            d.AgentId, d.AgentName,
            d.TicketsHandled, d.TicketsResolved,
            d.AvgFirstResponseMinutes, d.AvgResolutionMinutes,
            d.SlaComplianceRate, d.CsatScore, d.CsatResponseCount,
            d.EscalationRate)).ToList();
    }
}
```

- [ ] **Step 6: Add AgentPerformance action to ReportsController**

Open `src/CRM.API/Controllers/ReportsController.cs` and add:

```csharp
[HttpGet("agents")]
public async Task<IActionResult> AgentPerformance(
    [FromQuery] DateTime dateFrom,
    [FromQuery] DateTime dateTo,
    [FromQuery] Guid? departmentId,
    CancellationToken ct = default)
{
    try
    {
        var result = await _mediator.Send(
            new AgentPerformanceReportQuery(
                dateFrom, dateTo, CurrentUserId, CurrentUserRole, departmentId), ct);
        return Ok(new { data = result });
    }
    catch (FluentValidation.ValidationException ex)
        { return UnprocessableEntity(new { error = ex.Message }); }
    catch (UnauthorizedAccessException ex)
        { return StatusCode(403, new { error = ex.Message }); }
}
```

- [ ] **Step 7: Run tests to verify they pass**

```bash
dotnet test tests/CRM.Application.Tests/ --filter "AgentPerformanceReportQueryHandlerTests" -v n
```

Expected: 5 tests PASS.

- [ ] **Step 8: Commit**

```bash
git add src/CRM.Domain/Reports/IReportRepository.cs \
        src/CRM.Application/Reports/DTOs/AgentPerformanceReportDto.cs \
        src/CRM.Application/Reports/Queries/AgentPerformanceReportQuery.cs \
        src/CRM.API/Controllers/ReportsController.cs \
        tests/CRM.Application.Tests/Reports/AgentPerformanceReportQueryHandlerTests.cs
git commit -m "feat(reports): add GET /api/reports/agents — per-agent performance metrics, Admin/Manager only"
```
