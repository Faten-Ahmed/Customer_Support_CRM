# Ticket Volume Report — Implementation Plan

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

**Story:** US-BE-073  
**Goal:** Implement `GET /api/reports/tickets` — computes ticket volume summary (totalCreated, totalResolved, totalClosed, openAtEndOfPeriod), breakdowns by status/priority/channel, and a daily/weekly/monthly trend series. Role-scoped: Agent = their departments, Manager = their primary department, Admin = all.

**Architecture:** `TicketVolumeReportQuery(DateFrom, DateTo, RequestingUserId, RequestingUserRole, DepartmentId?, GroupBy)` → validates date range ≤ 365 days, resolves scope (Agent→dept IDs via `IUserRepository`, Manager→primary dept, Admin→unrestricted), delegates to `IReportRepository.GetTicketVolumeAsync()`. Returns `TicketVolumeReportDto`.

**Tech Stack:** .NET 10, ASP.NET Core, MediatR, EF Core, xUnit, Moq

---

## File Structure

| Action | Path |
|--------|------|
| Create | `src/CRM.Domain/Reports/IReportRepository.cs` |
| Create | `src/CRM.Application/Reports/DTOs/TicketVolumeReportDto.cs` |
| Create | `src/CRM.Application/Reports/Queries/TicketVolumeReportQuery.cs` |
| Create | `src/CRM.API/Controllers/ReportsController.cs` |
| Test   | `tests/CRM.Application.Tests/Reports/TicketVolumeReportQueryHandlerTests.cs` |
| Test   | `tests/CRM.API.Tests/Reports/ReportsControllerTicketVolumeTests.cs` |

---

## Task 1: Ticket Volume Report Query

- [ ] **Step 1: Write the failing tests**

```csharp
// tests/CRM.Application.Tests/Reports/TicketVolumeReportQueryHandlerTests.cs
using CRM.Application.Reports.Queries;
using CRM.Domain.Reports;
using CRM.Domain.Users;
using FluentValidation;
using Moq;
using Xunit;

namespace CRM.Application.Tests.Reports;

public class TicketVolumeReportQueryHandlerTests
{
    private readonly Mock<IReportRepository> _repo = new();
    private readonly Mock<IUserRepository> _users = new();
    private readonly TicketVolumeReportQueryHandler _handler;

    public TicketVolumeReportQueryHandlerTests()
    {
        _handler = new TicketVolumeReportQueryHandler(_repo.Object, _users.Object);
    }

    [Fact]
    public async Task Handle_AdminNoFilter_ReturnsReport()
    {
        var adminId = Guid.NewGuid();
        var from = new DateTime(2025, 10, 1);
        var to = new DateTime(2025, 10, 31);

        _repo.Setup(r => r.GetTicketVolumeAsync(
            from, to, null, "day", default))
             .ReturnsAsync(new TicketVolumeData(
                 320, 298, 275, 22,
                 new Dictionary<string, int>(),
                 new Dictionary<string, int>(),
                 new Dictionary<string, int>(),
                 new List<TrendPoint>()));

        var result = await _handler.Handle(
            new TicketVolumeReportQuery(from, to, adminId, UserRole.Admin, null, "day"),
            default);

        Assert.Equal(320, result.Summary.TotalCreated);
    }

    [Fact]
    public async Task Handle_DateRangeExceeds365Days_ThrowsValidationException()
    {
        var adminId = Guid.NewGuid();
        var from = new DateTime(2025, 1, 1);
        var to = from.AddDays(366);

        await Assert.ThrowsAsync<ValidationException>(() =>
            _handler.Handle(
                new TicketVolumeReportQuery(from, to, adminId, UserRole.Admin, null, "day"),
                default));
    }

    [Fact]
    public async Task Handle_AgentRequestingData_ScopesToOwnDepartments()
    {
        var agentId = Guid.NewGuid();
        var deptId = Guid.NewGuid();
        var from = new DateTime(2025, 10, 1);
        var to = new DateTime(2025, 10, 31);

        _users.Setup(u => u.GetDepartmentIdsAsync(agentId, default))
              .ReturnsAsync(new List<Guid> { deptId });

        _repo.Setup(r => r.GetTicketVolumeAsync(
            from, to, It.Is<IReadOnlyList<Guid>?>(ids => ids != null && ids.Contains(deptId)), "day", default))
             .ReturnsAsync(new TicketVolumeData(
                 10, 9, 8, 2,
                 new Dictionary<string, int>(),
                 new Dictionary<string, int>(),
                 new Dictionary<string, int>(),
                 new List<TrendPoint>()));

        var result = await _handler.Handle(
            new TicketVolumeReportQuery(from, to, agentId, UserRole.Agent, null, "day"),
            default);

        Assert.Equal(10, result.Summary.TotalCreated);
        _users.Verify(u => u.GetDepartmentIdsAsync(agentId, default), Times.Once);
    }

    [Fact]
    public async Task Handle_AgentRequestsOutOfScopeDept_ThrowsUnauthorizedAccessException()
    {
        var agentId = Guid.NewGuid();
        var agentDeptId = Guid.NewGuid();
        var otherDeptId = Guid.NewGuid();
        var from = new DateTime(2025, 10, 1);
        var to = new DateTime(2025, 10, 31);

        _users.Setup(u => u.GetDepartmentIdsAsync(agentId, default))
              .ReturnsAsync(new List<Guid> { agentDeptId });

        await Assert.ThrowsAsync<UnauthorizedAccessException>(() =>
            _handler.Handle(
                new TicketVolumeReportQuery(from, to, agentId, UserRole.Agent, otherDeptId, "day"),
                default));
    }
}
```

- [ ] **Step 2: Run tests to verify they fail**

```bash
dotnet test tests/CRM.Application.Tests/ --filter "TicketVolumeReportQueryHandlerTests" -v n
```

Expected: FAIL — `TicketVolumeReportQuery` does not exist yet.

- [ ] **Step 3: Create IReportRepository**

```csharp
// src/CRM.Domain/Reports/IReportRepository.cs
namespace CRM.Domain.Reports;

public record TicketVolumeData(
    int TotalCreated, int TotalResolved, int TotalClosed, int OpenAtEndOfPeriod,
    Dictionary<string, int> ByStatus,
    Dictionary<string, int> ByPriority,
    Dictionary<string, int> ByChannel,
    List<TrendPoint> Trend);

public record TrendPoint(string Date, int Created, int Resolved);

public interface IReportRepository
{
    Task<TicketVolumeData> GetTicketVolumeAsync(
        DateTime dateFrom, DateTime dateTo,
        IReadOnlyList<Guid>? departmentIds,
        string groupBy,
        CancellationToken ct = default);
}
```

- [ ] **Step 4: Add GetDepartmentIdsAsync to IUserRepository**

Open `src/CRM.Domain/Users/IUserRepository.cs` and add:

```csharp
Task<IReadOnlyList<Guid>> GetDepartmentIdsAsync(Guid userId, CancellationToken ct = default);
```

- [ ] **Step 5: Create TicketVolumeReportDto**

```csharp
// src/CRM.Application/Reports/DTOs/TicketVolumeReportDto.cs
namespace CRM.Application.Reports.DTOs;

public record TicketVolumeReportDto(
    VolumeSummary Summary,
    Dictionary<string, int> ByStatus,
    Dictionary<string, int> ByPriority,
    Dictionary<string, int> ByChannel,
    IReadOnlyList<TrendPointDto> Trend);

public record VolumeSummary(
    int TotalCreated, int TotalResolved, int TotalClosed, int OpenAtEndOfPeriod);

public record TrendPointDto(string Date, int Created, int Resolved);
```

- [ ] **Step 6: Implement TicketVolumeReportQuery**

```csharp
// src/CRM.Application/Reports/Queries/TicketVolumeReportQuery.cs
using CRM.Application.Reports.DTOs;
using CRM.Domain.Reports;
using CRM.Domain.Users;
using FluentValidation;
using FluentValidation.Results;
using MediatR;

namespace CRM.Application.Reports.Queries;

public record TicketVolumeReportQuery(
    DateTime DateFrom,
    DateTime DateTo,
    Guid RequestingUserId,
    UserRole RequestingUserRole,
    Guid? DepartmentId,
    string GroupBy) : IRequest<TicketVolumeReportDto>;

public class TicketVolumeReportQueryHandler
    : IRequestHandler<TicketVolumeReportQuery, TicketVolumeReportDto>
{
    private readonly IReportRepository _reports;
    private readonly IUserRepository _users;

    public TicketVolumeReportQueryHandler(IReportRepository reports, IUserRepository users)
    {
        _reports = reports;
        _users = users;
    }

    public async Task<TicketVolumeReportDto> Handle(
        TicketVolumeReportQuery query, CancellationToken ct)
    {
        if ((query.DateTo - query.DateFrom).TotalDays > 365)
            throw new ValidationException(new[]
            {
                new ValidationFailure("DateRange",
                    "Date range cannot exceed 365 days.", "DATE_RANGE_TOO_LARGE")
            });

        IReadOnlyList<Guid>? effectiveDepartmentIds = null;

        if (query.RequestingUserRole == UserRole.Agent)
        {
            var agentDeptIds = await _users.GetDepartmentIdsAsync(query.RequestingUserId, ct);
            if (query.DepartmentId.HasValue)
            {
                if (!agentDeptIds.Contains(query.DepartmentId.Value))
                    throw new UnauthorizedAccessException(
                        "Agents can only view reports for their own departments.");
                effectiveDepartmentIds = new[] { query.DepartmentId.Value };
            }
            else
            {
                effectiveDepartmentIds = agentDeptIds;
            }
        }
        else if (query.RequestingUserRole == UserRole.Manager)
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

        var data = await _reports.GetTicketVolumeAsync(
            query.DateFrom, query.DateTo, effectiveDepartmentIds, query.GroupBy, ct);

        return new TicketVolumeReportDto(
            new VolumeSummary(
                data.TotalCreated, data.TotalResolved,
                data.TotalClosed, data.OpenAtEndOfPeriod),
            data.ByStatus,
            data.ByPriority,
            data.ByChannel,
            data.Trend.Select(t => new TrendPointDto(t.Date, t.Created, t.Resolved)).ToList());
    }
}
```

- [ ] **Step 7: Run tests to verify they pass**

```bash
dotnet test tests/CRM.Application.Tests/ --filter "TicketVolumeReportQueryHandlerTests" -v n
```

Expected: 4 tests PASS.

- [ ] **Step 8: Create ReportsController**

```csharp
// src/CRM.API/Controllers/ReportsController.cs
using CRM.Application.Reports.Queries;
using CRM.Domain.Users;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace CRM.API.Controllers;

[ApiController]
[Route("api/reports")]
[Authorize]
public class ReportsController : ControllerBase
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

    public ReportsController(IMediator mediator) => _mediator = mediator;

    [HttpGet("tickets")]
    public async Task<IActionResult> TicketVolume(
        [FromQuery] DateTime dateFrom,
        [FromQuery] DateTime dateTo,
        [FromQuery] Guid? departmentId,
        [FromQuery] string groupBy = "day",
        CancellationToken ct = default)
    {
        try
        {
            var result = await _mediator.Send(
                new TicketVolumeReportQuery(
                    dateFrom, dateTo, CurrentUserId, CurrentUserRole,
                    departmentId, groupBy), ct);
            return Ok(new { data = result });
        }
        catch (FluentValidation.ValidationException ex)
            { return UnprocessableEntity(new { error = ex.Message }); }
        catch (UnauthorizedAccessException ex)
            { return StatusCode(403, new { error = ex.Message }); }
    }
}
```

- [ ] **Step 9: Write controller test**

```csharp
// tests/CRM.API.Tests/Reports/ReportsControllerTicketVolumeTests.cs
using System.Net;
using CRM.Application.Reports.DTOs;
using CRM.Application.Reports.Queries;
using MediatR;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using Xunit;

namespace CRM.API.Tests.Reports;

public class ReportsControllerTicketVolumeTests
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
    public async Task TicketVolume_ValidQuery_Returns200()
    {
        _mediator.Setup(m => m.Send(It.IsAny<TicketVolumeReportQuery>(), default))
                 .ReturnsAsync(new TicketVolumeReportDto(
                     new VolumeSummary(100, 90, 85, 10),
                     new Dictionary<string, int>(),
                     new Dictionary<string, int>(),
                     new Dictionary<string, int>(),
                     new List<TrendPointDto>()));

        var response = await BuildClient().GetAsync(
            "/api/reports/tickets?dateFrom=2025-10-01&dateTo=2025-10-31");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task TicketVolume_RangeExceeds365Days_Returns422()
    {
        _mediator.Setup(m => m.Send(It.IsAny<TicketVolumeReportQuery>(), default))
                 .ThrowsAsync(new FluentValidation.ValidationException("DATE_RANGE_TOO_LARGE"));

        var response = await BuildClient().GetAsync(
            "/api/reports/tickets?dateFrom=2025-01-01&dateTo=2026-01-03");

        Assert.Equal(HttpStatusCode.UnprocessableEntity, response.StatusCode);
    }
}
```

- [ ] **Step 10: Run controller tests**

```bash
dotnet test tests/CRM.API.Tests/ --filter "ReportsControllerTicketVolumeTests" -v n
```

Expected: 2 tests PASS.

- [ ] **Step 11: Commit**

```bash
git add src/CRM.Domain/Reports/IReportRepository.cs \
        src/CRM.Application/Reports/ \
        src/CRM.API/Controllers/ReportsController.cs \
        tests/CRM.Application.Tests/Reports/TicketVolumeReportQueryHandlerTests.cs \
        tests/CRM.API.Tests/Reports/ReportsControllerTicketVolumeTests.cs
git commit -m "feat(reports): add GET /api/reports/tickets — ticket volume report with department-scoped access and 365-day limit"
```
