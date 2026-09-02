# SLA Compliance Report — Implementation Plan

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

**Story:** US-BE-074  
**Goal:** Implement `GET /api/reports/sla` — returns SLA compliance rates (first response + resolution), average response/resolution times in business minutes, per-priority breakdown, and breach reason counts. Role-scoped same as ticket volume report.

**Architecture:** `SlaComplianceReportQuery` adds `GetSlaComplianceAsync` to `IReportRepository`. Handler enforces same 365-day limit and department scope logic as US-BE-073. Adds `sla` action to existing `ReportsController`.

**Tech Stack:** .NET 10, ASP.NET Core, MediatR, EF Core, xUnit, Moq

---

## File Structure

| Action | Path |
|--------|------|
| Modify | `src/CRM.Domain/Reports/IReportRepository.cs` |
| Create | `src/CRM.Application/Reports/DTOs/SlaComplianceReportDto.cs` |
| Create | `src/CRM.Application/Reports/Queries/SlaComplianceReportQuery.cs` |
| Modify | `src/CRM.API/Controllers/ReportsController.cs` |
| Test   | `tests/CRM.Application.Tests/Reports/SlaComplianceReportQueryHandlerTests.cs` |

---

## Task 1: SLA Compliance Report Query

> Note: `IReportRepository` and `ReportsController` are created in US-BE-073. Implement that plan first.

- [ ] **Step 1: Write the failing tests**

```csharp
// tests/CRM.Application.Tests/Reports/SlaComplianceReportQueryHandlerTests.cs
using CRM.Application.Reports.Queries;
using CRM.Domain.Reports;
using CRM.Domain.Users;
using FluentValidation;
using Moq;
using Xunit;

namespace CRM.Application.Tests.Reports;

public class SlaComplianceReportQueryHandlerTests
{
    private readonly Mock<IReportRepository> _repo = new();
    private readonly Mock<IUserRepository> _users = new();
    private readonly SlaComplianceReportQueryHandler _handler;

    public SlaComplianceReportQueryHandlerTests()
    {
        _handler = new SlaComplianceReportQueryHandler(_repo.Object, _users.Object);
    }

    [Fact]
    public async Task Handle_AdminAllDepts_ReturnsComplianceReport()
    {
        var adminId = Guid.NewGuid();
        var from = new DateTime(2025, 10, 1);
        var to = new DateTime(2025, 10, 31);

        _repo.Setup(r => r.GetSlaComplianceAsync(from, to, null, null, default))
             .ReturnsAsync(new SlaComplianceData(
                 92.5m, 88.3m, 14.2m, 240.5m,
                 new Dictionary<string, SlaComplianceByPriority>(),
                 new SlaBreachReasons(5, 12, 3)));

        var result = await _handler.Handle(
            new SlaComplianceReportQuery(from, to, adminId, UserRole.Admin, null, null),
            default);

        Assert.Equal(92.5m, result.FirstResponseComplianceRate);
        Assert.Equal(88.3m, result.ResolutionComplianceRate);
    }

    [Fact]
    public async Task Handle_DateRangeExceeds365Days_ThrowsValidationException()
    {
        var adminId = Guid.NewGuid();
        var from = new DateTime(2025, 1, 1);
        var to = from.AddDays(366);

        await Assert.ThrowsAsync<ValidationException>(() =>
            _handler.Handle(
                new SlaComplianceReportQuery(from, to, adminId, UserRole.Admin, null, null),
                default));
    }

    [Fact]
    public async Task Handle_AgentAccessesOtherDept_ThrowsUnauthorizedAccessException()
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
                new SlaComplianceReportQuery(from, to, agentId, UserRole.Agent, otherDeptId, null),
                default));
    }

    [Fact]
    public async Task Handle_PriorityFilter_PassedToRepository()
    {
        var adminId = Guid.NewGuid();
        var from = new DateTime(2025, 10, 1);
        var to = new DateTime(2025, 10, 31);

        _repo.Setup(r => r.GetSlaComplianceAsync(from, to, null, "Critical", default))
             .ReturnsAsync(new SlaComplianceData(
                 80.0m, 75.0m, 8.5m, 180.0m,
                 new Dictionary<string, SlaComplianceByPriority>(),
                 new SlaBreachReasons(2, 5, 1)));

        var result = await _handler.Handle(
            new SlaComplianceReportQuery(from, to, adminId, UserRole.Admin, null, "Critical"),
            default);

        _repo.Verify(r => r.GetSlaComplianceAsync(from, to, null, "Critical", default), Times.Once);
    }
}
```

- [ ] **Step 2: Run tests to verify they fail**

```bash
dotnet test tests/CRM.Application.Tests/ --filter "SlaComplianceReportQueryHandlerTests" -v n
```

Expected: FAIL — `SlaComplianceReportQuery` does not exist yet.

- [ ] **Step 3: Add GetSlaComplianceAsync to IReportRepository**

Open `src/CRM.Domain/Reports/IReportRepository.cs` and add the following records and method:

```csharp
public record SlaComplianceByPriority(
    decimal FirstResponseComplianceRate,
    decimal ResolutionComplianceRate,
    int TotalTickets);

public record SlaBreachReasons(int WarningCount, int BreachCount, int CriticalBreachCount);

public record SlaComplianceData(
    decimal FirstResponseComplianceRate,
    decimal ResolutionComplianceRate,
    decimal AvgFirstResponseMinutes,
    decimal AvgResolutionMinutes,
    Dictionary<string, SlaComplianceByPriority> ByPriority,
    SlaBreachReasons BreachReasons);
```

Add to `IReportRepository` interface:

```csharp
Task<SlaComplianceData> GetSlaComplianceAsync(
    DateTime dateFrom, DateTime dateTo,
    IReadOnlyList<Guid>? departmentIds,
    string? priority,
    CancellationToken ct = default);
```

- [ ] **Step 4: Create SlaComplianceReportDto**

```csharp
// src/CRM.Application/Reports/DTOs/SlaComplianceReportDto.cs
namespace CRM.Application.Reports.DTOs;

public record SlaComplianceReportDto(
    decimal FirstResponseComplianceRate,
    decimal ResolutionComplianceRate,
    decimal AvgFirstResponseMinutes,
    decimal AvgResolutionMinutes,
    Dictionary<string, SlaComplianceByPriorityDto> ByPriority,
    SlaBreachReasonsDto BreachReasons);

public record SlaComplianceByPriorityDto(
    decimal FirstResponseComplianceRate,
    decimal ResolutionComplianceRate,
    int TotalTickets);

public record SlaBreachReasonsDto(int WarningCount, int BreachCount, int CriticalBreachCount);
```

- [ ] **Step 5: Implement SlaComplianceReportQuery**

```csharp
// src/CRM.Application/Reports/Queries/SlaComplianceReportQuery.cs
using CRM.Application.Reports.DTOs;
using CRM.Domain.Reports;
using CRM.Domain.Users;
using FluentValidation;
using FluentValidation.Results;
using MediatR;

namespace CRM.Application.Reports.Queries;

public record SlaComplianceReportQuery(
    DateTime DateFrom,
    DateTime DateTo,
    Guid RequestingUserId,
    UserRole RequestingUserRole,
    Guid? DepartmentId,
    string? Priority) : IRequest<SlaComplianceReportDto>;

public class SlaComplianceReportQueryHandler
    : IRequestHandler<SlaComplianceReportQuery, SlaComplianceReportDto>
{
    private readonly IReportRepository _reports;
    private readonly IUserRepository _users;

    public SlaComplianceReportQueryHandler(IReportRepository reports, IUserRepository users)
    {
        _reports = reports;
        _users = users;
    }

    public async Task<SlaComplianceReportDto> Handle(
        SlaComplianceReportQuery query, CancellationToken ct)
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

        var data = await _reports.GetSlaComplianceAsync(
            query.DateFrom, query.DateTo, effectiveDepartmentIds, query.Priority, ct);

        return new SlaComplianceReportDto(
            data.FirstResponseComplianceRate,
            data.ResolutionComplianceRate,
            data.AvgFirstResponseMinutes,
            data.AvgResolutionMinutes,
            data.ByPriority.ToDictionary(
                kvp => kvp.Key,
                kvp => new SlaComplianceByPriorityDto(
                    kvp.Value.FirstResponseComplianceRate,
                    kvp.Value.ResolutionComplianceRate,
                    kvp.Value.TotalTickets)),
            new SlaBreachReasonsDto(
                data.BreachReasons.WarningCount,
                data.BreachReasons.BreachCount,
                data.BreachReasons.CriticalBreachCount));
    }
}
```

- [ ] **Step 6: Add SlaReport action to ReportsController**

Open `src/CRM.API/Controllers/ReportsController.cs` and add the following action inside the class:

```csharp
[HttpGet("sla")]
public async Task<IActionResult> SlaCompliance(
    [FromQuery] DateTime dateFrom,
    [FromQuery] DateTime dateTo,
    [FromQuery] Guid? departmentId,
    [FromQuery] string? priority,
    CancellationToken ct = default)
{
    try
    {
        var result = await _mediator.Send(
            new SlaComplianceReportQuery(
                dateFrom, dateTo, CurrentUserId, CurrentUserRole,
                departmentId, priority), ct);
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
dotnet test tests/CRM.Application.Tests/ --filter "SlaComplianceReportQueryHandlerTests" -v n
```

Expected: 4 tests PASS.

- [ ] **Step 8: Commit**

```bash
git add src/CRM.Domain/Reports/IReportRepository.cs \
        src/CRM.Application/Reports/DTOs/SlaComplianceReportDto.cs \
        src/CRM.Application/Reports/Queries/SlaComplianceReportQuery.cs \
        src/CRM.API/Controllers/ReportsController.cs \
        tests/CRM.Application.Tests/Reports/SlaComplianceReportQueryHandlerTests.cs
git commit -m "feat(reports): add GET /api/reports/sla — SLA compliance rates with priority filter and department scoping"
```
