# CSAT Report — Implementation Plan

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

**Story:** US-BE-076  
**Goal:** Implement `GET /api/reports/csat` — returns CSAT overall metrics (avgRating, totalSent, totalSubmitted, responseRate), distribution (counts per rating 1–5), per-department breakdown, per-agent breakdown, and last 20 comments. `avgRating = null` when `totalSubmitted = 0`. Expired surveys count toward `totalSent` but not `avgRating` or `totalSubmitted`.

**Architecture:** `CsatReportQuery` adds `GetCsatReportAsync` to `IReportRepository`. Same 365-day limit and department scope logic. Adds `csat` action to `ReportsController`.

**Tech Stack:** .NET 10, ASP.NET Core, MediatR, EF Core, xUnit, Moq

---

## File Structure

| Action | Path |
|--------|------|
| Modify | `src/CRM.Domain/Reports/IReportRepository.cs` |
| Create | `src/CRM.Application/Reports/DTOs/CsatReportDto.cs` |
| Create | `src/CRM.Application/Reports/Queries/CsatReportQuery.cs` |
| Modify | `src/CRM.API/Controllers/ReportsController.cs` |
| Test   | `tests/CRM.Application.Tests/Reports/CsatReportQueryHandlerTests.cs` |

---

## Task 1: CSAT Report Query

> Note: `IReportRepository`, `ReportsController`, and `IUserRepository.GetDepartmentIdsAsync` are from US-BE-073. Implement that plan first.

- [ ] **Step 1: Write the failing tests**

```csharp
// tests/CRM.Application.Tests/Reports/CsatReportQueryHandlerTests.cs
using CRM.Application.Reports.Queries;
using CRM.Domain.Reports;
using CRM.Domain.Users;
using FluentValidation;
using Moq;
using Xunit;

namespace CRM.Application.Tests.Reports;

public class CsatReportQueryHandlerTests
{
    private readonly Mock<IReportRepository> _repo = new();
    private readonly Mock<IUserRepository> _users = new();
    private readonly CsatReportQueryHandler _handler;

    public CsatReportQueryHandlerTests()
    {
        _handler = new CsatReportQueryHandler(_repo.Object, _users.Object);
    }

    [Fact]
    public async Task Handle_AdminNoFilter_ReturnsCsatReport()
    {
        var adminId = Guid.NewGuid();
        var from = new DateTime(2025, 10, 1);
        var to = new DateTime(2025, 10, 31);

        _repo.Setup(r => r.GetCsatReportAsync(from, to, null, default))
             .ReturnsAsync(new CsatReportData(
                 new CsatOverallData(4.2m, 150, 120, 80.0m),
                 new Dictionary<int, int> { [1] = 2, [2] = 5, [3] = 10, [4] = 40, [5] = 63 },
                 new List<CsatByDepartmentData>(),
                 new List<CsatByAgentData>(),
                 new List<string>()));

        var result = await _handler.Handle(
            new CsatReportQuery(from, to, adminId, UserRole.Admin, null),
            default);

        Assert.Equal(4.2m, result.Overall.AvgRating);
        Assert.Equal(150, result.Overall.TotalSent);
        Assert.Equal(80.0m, result.Overall.ResponseRate);
    }

    [Fact]
    public async Task Handle_NoSubmissions_AvgRatingIsNull()
    {
        var adminId = Guid.NewGuid();
        var from = new DateTime(2025, 10, 1);
        var to = new DateTime(2025, 10, 31);

        _repo.Setup(r => r.GetCsatReportAsync(from, to, null, default))
             .ReturnsAsync(new CsatReportData(
                 new CsatOverallData(null, 50, 0, 0.0m),
                 new Dictionary<int, int> { [1] = 0, [2] = 0, [3] = 0, [4] = 0, [5] = 0 },
                 new List<CsatByDepartmentData>(),
                 new List<CsatByAgentData>(),
                 new List<string>()));

        var result = await _handler.Handle(
            new CsatReportQuery(from, to, adminId, UserRole.Admin, null),
            default);

        Assert.Null(result.Overall.AvgRating);
        Assert.Equal(50, result.Overall.TotalSent);
        Assert.Equal(0, result.Overall.TotalSubmitted);
    }

    [Fact]
    public async Task Handle_DateRangeExceeds365Days_ThrowsValidationException()
    {
        var adminId = Guid.NewGuid();
        var from = new DateTime(2025, 1, 1);
        var to = from.AddDays(366);

        await Assert.ThrowsAsync<ValidationException>(() =>
            _handler.Handle(
                new CsatReportQuery(from, to, adminId, UserRole.Admin, null),
                default));
    }

    [Fact]
    public async Task Handle_AgentScope_ScopesToOwnDepartments()
    {
        var agentId = Guid.NewGuid();
        var deptId = Guid.NewGuid();
        var from = new DateTime(2025, 10, 1);
        var to = new DateTime(2025, 10, 31);

        _users.Setup(u => u.GetDepartmentIdsAsync(agentId, default))
              .ReturnsAsync(new List<Guid> { deptId });

        _repo.Setup(r => r.GetCsatReportAsync(
            from, to, It.Is<IReadOnlyList<Guid>?>(ids => ids != null && ids.Contains(deptId)), default))
             .ReturnsAsync(new CsatReportData(
                 new CsatOverallData(4.0m, 10, 8, 80.0m),
                 new Dictionary<int, int>(),
                 new List<CsatByDepartmentData>(),
                 new List<CsatByAgentData>(),
                 new List<string>()));

        await _handler.Handle(
            new CsatReportQuery(from, to, agentId, UserRole.Agent, null),
            default);

        _users.Verify(u => u.GetDepartmentIdsAsync(agentId, default), Times.Once);
    }
}
```

- [ ] **Step 2: Run tests to verify they fail**

```bash
dotnet test tests/CRM.Application.Tests/ --filter "CsatReportQueryHandlerTests" -v n
```

Expected: FAIL — `CsatReportQuery` does not exist yet.

- [ ] **Step 3: Add GetCsatReportAsync to IReportRepository**

Open `src/CRM.Domain/Reports/IReportRepository.cs` and add:

```csharp
public record CsatOverallData(
    decimal? AvgRating, int TotalSent, int TotalSubmitted, decimal ResponseRate);

public record CsatByDepartmentData(
    Guid DepartmentId, string DepartmentName, decimal? AvgRating, int TotalSubmitted);

public record CsatByAgentData(
    Guid AgentId, string AgentName, decimal? AvgRating, int TotalSubmitted);

public record CsatReportData(
    CsatOverallData Overall,
    Dictionary<int, int> Distribution,
    List<CsatByDepartmentData> ByDepartment,
    List<CsatByAgentData> ByAgent,
    List<string> RecentComments);
```

Add to `IReportRepository` interface:

```csharp
Task<CsatReportData> GetCsatReportAsync(
    DateTime dateFrom, DateTime dateTo,
    IReadOnlyList<Guid>? departmentIds,
    CancellationToken ct = default);
```

- [ ] **Step 4: Create CsatReportDto**

```csharp
// src/CRM.Application/Reports/DTOs/CsatReportDto.cs
namespace CRM.Application.Reports.DTOs;

public record CsatReportDto(
    CsatOverallDto Overall,
    Dictionary<int, int> Distribution,
    IReadOnlyList<CsatByDepartmentDto> ByDepartment,
    IReadOnlyList<CsatByAgentDto> ByAgent,
    IReadOnlyList<string> RecentComments);

public record CsatOverallDto(
    decimal? AvgRating, int TotalSent, int TotalSubmitted, decimal ResponseRate);

public record CsatByDepartmentDto(
    Guid DepartmentId, string DepartmentName, decimal? AvgRating, int TotalSubmitted);

public record CsatByAgentDto(
    Guid AgentId, string AgentName, decimal? AvgRating, int TotalSubmitted);
```

- [ ] **Step 5: Implement CsatReportQuery**

```csharp
// src/CRM.Application/Reports/Queries/CsatReportQuery.cs
using CRM.Application.Reports.DTOs;
using CRM.Domain.Reports;
using CRM.Domain.Users;
using FluentValidation;
using FluentValidation.Results;
using MediatR;

namespace CRM.Application.Reports.Queries;

public record CsatReportQuery(
    DateTime DateFrom,
    DateTime DateTo,
    Guid RequestingUserId,
    UserRole RequestingUserRole,
    Guid? DepartmentId) : IRequest<CsatReportDto>;

public class CsatReportQueryHandler
    : IRequestHandler<CsatReportQuery, CsatReportDto>
{
    private readonly IReportRepository _reports;
    private readonly IUserRepository _users;

    public CsatReportQueryHandler(IReportRepository reports, IUserRepository users)
    {
        _reports = reports;
        _users = users;
    }

    public async Task<CsatReportDto> Handle(CsatReportQuery query, CancellationToken ct)
    {
        if ((query.DateTo - query.DateFrom).TotalDays > 365)
            throw new ValidationException(new[]
            {
                new ValidationFailure("DateRange",
                    "Date range cannot exceed 365 days.", "DATE_RANGE_TOO_LARGE")
            });

        IReadOnlyList<Guid>? effectiveDepartmentIds = null;

        if (query.RequestingUserRole is UserRole.Agent or UserRole.Manager)
        {
            var userDeptIds = await _users.GetDepartmentIdsAsync(query.RequestingUserId, ct);
            if (query.DepartmentId.HasValue)
            {
                if (!userDeptIds.Contains(query.DepartmentId.Value))
                    throw new UnauthorizedAccessException(
                        "Access to this department's report is not permitted.");
                effectiveDepartmentIds = new[] { query.DepartmentId.Value };
            }
            else
            {
                effectiveDepartmentIds = userDeptIds;
            }
        }
        else if (query.DepartmentId.HasValue)
        {
            effectiveDepartmentIds = new[] { query.DepartmentId.Value };
        }

        var data = await _reports.GetCsatReportAsync(
            query.DateFrom, query.DateTo, effectiveDepartmentIds, ct);

        return new CsatReportDto(
            new CsatOverallDto(
                data.Overall.AvgRating,
                data.Overall.TotalSent,
                data.Overall.TotalSubmitted,
                data.Overall.ResponseRate),
            data.Distribution,
            data.ByDepartment.Select(d => new CsatByDepartmentDto(
                d.DepartmentId, d.DepartmentName, d.AvgRating, d.TotalSubmitted)).ToList(),
            data.ByAgent.Select(a => new CsatByAgentDto(
                a.AgentId, a.AgentName, a.AvgRating, a.TotalSubmitted)).ToList(),
            data.RecentComments);
    }
}
```

- [ ] **Step 6: Add Csat action to ReportsController**

Open `src/CRM.API/Controllers/ReportsController.cs` and add:

```csharp
[HttpGet("csat")]
public async Task<IActionResult> Csat(
    [FromQuery] DateTime dateFrom,
    [FromQuery] DateTime dateTo,
    [FromQuery] Guid? departmentId,
    CancellationToken ct = default)
{
    try
    {
        var result = await _mediator.Send(
            new CsatReportQuery(
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
dotnet test tests/CRM.Application.Tests/ --filter "CsatReportQueryHandlerTests" -v n
```

Expected: 4 tests PASS.

- [ ] **Step 8: Commit**

```bash
git add src/CRM.Domain/Reports/IReportRepository.cs \
        src/CRM.Application/Reports/DTOs/CsatReportDto.cs \
        src/CRM.Application/Reports/Queries/CsatReportQuery.cs \
        src/CRM.API/Controllers/ReportsController.cs \
        tests/CRM.Application.Tests/Reports/CsatReportQueryHandlerTests.cs
git commit -m "feat(reports): add GET /api/reports/csat — CSAT overall, distribution, by-dept/agent, recent comments"
```
