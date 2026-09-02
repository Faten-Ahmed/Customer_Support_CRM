# Report Export (CSV / Excel / PDF) — Implementation Plan

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

**Story:** US-BE-079  
**Goal:** Implement `GET /api/reports/export?reportType=tickets|sla|agents|csat&format=csv|xlsx|pdf&dateFrom=&dateTo=` — synchronous file download for ≤ 10,000 rows, `202 Accepted` with `{ jobId }` for > 10,000 rows (Hangfire async). Agent role returns 403. Sets correct Content-Type and Content-Disposition headers.

**Architecture:** `ExportReportCommand` dispatches to `IReportExportService.ExportAsync(type, format, scope, ct)`. Threshold check calls `IReportRepository.CountRowsAsync()`. Async path: Hangfire enqueues `GenerateReportExportJob`. Libraries: `CsvHelper` (CSV), `ClosedXML` (xlsx), `QuestPDF` (PDF).

**Tech Stack:** .NET 10, ASP.NET Core, MediatR, Hangfire, CsvHelper, ClosedXML, QuestPDF, xUnit, Moq

---

## File Structure

| Action | Path |
|--------|------|
| Create | `src/CRM.Application/Reports/Commands/ExportReportCommand.cs` |
| Create | `src/CRM.Application/Reports/Services/IReportExportService.cs` |
| Create | `src/CRM.Infrastructure/Reports/ReportExportService.cs` |
| Create | `src/CRM.Infrastructure/Reports/Jobs/GenerateReportExportJob.cs` |
| Modify | `src/CRM.Domain/Reports/IReportRepository.cs` |
| Modify | `src/CRM.API/Controllers/ReportsController.cs` |
| Test   | `tests/CRM.Application.Tests/Reports/ExportReportCommandHandlerTests.cs` |

---

## Task 1: Report Export Command

> Note: `IReportRepository` and `ReportsController` are from US-BE-073. `IUserRepository.GetDepartmentIdsAsync` from US-BE-073. Implement those plans first.

- [ ] **Step 1: Write the failing tests**

```csharp
// tests/CRM.Application.Tests/Reports/ExportReportCommandHandlerTests.cs
using CRM.Application.Reports.Commands;
using CRM.Application.Reports.Services;
using CRM.Domain.Reports;
using CRM.Domain.Users;
using Hangfire;
using Moq;
using Xunit;

namespace CRM.Application.Tests.Reports;

public class ExportReportCommandHandlerTests
{
    private readonly Mock<IReportRepository> _repo = new();
    private readonly Mock<IReportExportService> _exporter = new();
    private readonly Mock<IBackgroundJobClient> _jobs = new();
    private readonly Mock<IUserRepository> _users = new();
    private readonly ExportReportCommandHandler _handler;

    public ExportReportCommandHandlerTests()
    {
        _handler = new ExportReportCommandHandler(
            _repo.Object, _exporter.Object, _jobs.Object, _users.Object);
    }

    [Fact]
    public async Task Handle_AgentRole_ThrowsUnauthorizedAccessException()
    {
        var agentId = Guid.NewGuid();

        await Assert.ThrowsAsync<UnauthorizedAccessException>(() =>
            _handler.Handle(
                new ExportReportCommand(agentId, UserRole.Agent, "tickets", "csv",
                    new DateTime(2025, 10, 1), new DateTime(2025, 10, 31), null),
                default));
    }

    [Fact]
    public async Task Handle_SmallDataset_ReturnsSynchronousFile()
    {
        var adminId = Guid.NewGuid();
        var from = new DateTime(2025, 10, 1);
        var to = new DateTime(2025, 10, 31);
        var csvBytes = new byte[] { 0x41, 0x42, 0x43 };

        _repo.Setup(r => r.CountRowsAsync("tickets", null, from, to, default))
             .ReturnsAsync(500);
        _exporter.Setup(e => e.ExportAsync("tickets", "csv", null, from, to, default))
                 .ReturnsAsync(csvBytes);

        var result = await _handler.Handle(
            new ExportReportCommand(adminId, UserRole.Admin, "tickets", "csv", from, to, null),
            default);

        Assert.False(result.IsAsync);
        Assert.Equal(csvBytes, result.FileBytes);
        Assert.Equal("text/csv", result.ContentType);
    }

    [Fact]
    public async Task Handle_LargeDataset_ReturnsJobId()
    {
        var adminId = Guid.NewGuid();
        var from = new DateTime(2025, 1, 1);
        var to = new DateTime(2025, 10, 31);
        var jobId = "hangfire-job-123";

        _repo.Setup(r => r.CountRowsAsync("tickets", null, from, to, default))
             .ReturnsAsync(15000);
        _jobs.Setup(j => j.Create(It.IsAny<Job>(), It.IsAny<IState>()))
             .Returns(jobId);

        var result = await _handler.Handle(
            new ExportReportCommand(adminId, UserRole.Admin, "tickets", "csv", from, to, null),
            default);

        Assert.True(result.IsAsync);
        Assert.Equal(jobId, result.JobId);
    }

    [Fact]
    public async Task Handle_InvalidReportType_ThrowsArgumentException()
    {
        var adminId = Guid.NewGuid();

        await Assert.ThrowsAsync<ArgumentException>(() =>
            _handler.Handle(
                new ExportReportCommand(adminId, UserRole.Admin, "invalid", "csv",
                    new DateTime(2025, 10, 1), new DateTime(2025, 10, 31), null),
                default));
    }
}
```

- [ ] **Step 2: Run tests to verify they fail**

```bash
dotnet test tests/CRM.Application.Tests/ --filter "ExportReportCommandHandlerTests" -v n
```

Expected: FAIL — `ExportReportCommand` does not exist yet.

- [ ] **Step 3: Add CountRowsAsync to IReportRepository**

Open `src/CRM.Domain/Reports/IReportRepository.cs` and add to the `IReportRepository` interface:

```csharp
Task<int> CountRowsAsync(
    string reportType,
    IReadOnlyList<Guid>? departmentIds,
    DateTime dateFrom,
    DateTime dateTo,
    CancellationToken ct = default);
```

- [ ] **Step 4: Create IReportExportService**

```csharp
// src/CRM.Application/Reports/Services/IReportExportService.cs
namespace CRM.Application.Reports.Services;

public interface IReportExportService
{
    Task<byte[]> ExportAsync(
        string reportType,
        string format,
        IReadOnlyList<Guid>? departmentIds,
        DateTime dateFrom,
        DateTime dateTo,
        CancellationToken ct = default);
}
```

- [ ] **Step 5: Implement ExportReportCommand**

```csharp
// src/CRM.Application/Reports/Commands/ExportReportCommand.cs
using CRM.Application.Reports.Services;
using CRM.Domain.Reports;
using CRM.Domain.Users;
using Hangfire;
using MediatR;

namespace CRM.Application.Reports.Commands;

public record ExportReportCommand(
    Guid RequestingUserId,
    UserRole RequestingUserRole,
    string ReportType,
    string Format,
    DateTime DateFrom,
    DateTime DateTo,
    Guid? DepartmentId) : IRequest<ExportReportResult>;

public record ExportReportResult(
    bool IsAsync,
    byte[]? FileBytes,
    string? ContentType,
    string? FileName,
    string? JobId);

public class ExportReportCommandHandler : IRequestHandler<ExportReportCommand, ExportReportResult>
{
    private static readonly string[] ValidReportTypes = ["tickets", "sla", "agents", "csat"];
    private static readonly string[] ValidFormats = ["csv", "xlsx", "pdf"];
    private const int AsyncThreshold = 10_000;

    private readonly IReportRepository _repo;
    private readonly IReportExportService _exporter;
    private readonly IBackgroundJobClient _jobs;
    private readonly IUserRepository _users;

    public ExportReportCommandHandler(
        IReportRepository repo,
        IReportExportService exporter,
        IBackgroundJobClient jobs,
        IUserRepository users)
    {
        _repo = repo;
        _exporter = exporter;
        _jobs = jobs;
        _users = users;
    }

    public async Task<ExportReportResult> Handle(
        ExportReportCommand cmd, CancellationToken ct)
    {
        if (cmd.RequestingUserRole == UserRole.Agent)
            throw new UnauthorizedAccessException(
                "Agents are not permitted to export reports.");

        if (!ValidReportTypes.Contains(cmd.ReportType))
            throw new ArgumentException($"Unknown report type: {cmd.ReportType}");

        if (!ValidFormats.Contains(cmd.Format))
            throw new ArgumentException($"Unknown format: {cmd.Format}");

        IReadOnlyList<Guid>? effectiveDepartmentIds = null;
        if (cmd.RequestingUserRole == UserRole.Manager)
        {
            var depts = await _users.GetDepartmentIdsAsync(cmd.RequestingUserId, ct);
            effectiveDepartmentIds = cmd.DepartmentId.HasValue
                ? new[] { cmd.DepartmentId.Value }
                : depts;
        }
        else if (cmd.DepartmentId.HasValue)
        {
            effectiveDepartmentIds = new[] { cmd.DepartmentId.Value };
        }

        var rowCount = await _repo.CountRowsAsync(
            cmd.ReportType, effectiveDepartmentIds, cmd.DateFrom, cmd.DateTo, ct);

        if (rowCount > AsyncThreshold)
        {
            var jobId = _jobs.Enqueue<GenerateReportExportJob>(
                j => j.ExecuteAsync(
                    cmd.ReportType, cmd.Format,
                    effectiveDepartmentIds == null ? null : effectiveDepartmentIds.ToArray(),
                    cmd.DateFrom, cmd.DateTo));
            return new ExportReportResult(true, null, null, null, jobId);
        }

        var bytes = await _exporter.ExportAsync(
            cmd.ReportType, cmd.Format, effectiveDepartmentIds,
            cmd.DateFrom, cmd.DateTo, ct);

        var (contentType, ext) = cmd.Format switch
        {
            "csv" => ("text/csv", "csv"),
            "xlsx" => ("application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", "xlsx"),
            "pdf" => ("application/pdf", "pdf"),
            _ => ("application/octet-stream", "bin")
        };

        var fileName = $"report-{cmd.ReportType}-{cmd.DateFrom:yyyy-MM-dd}.{ext}";
        return new ExportReportResult(false, bytes, contentType, fileName, null);
    }
}
```

- [ ] **Step 6: Create GenerateReportExportJob stub**

```csharp
// src/CRM.Infrastructure/Reports/Jobs/GenerateReportExportJob.cs
using CRM.Application.Reports.Services;

namespace CRM.Infrastructure.Reports.Jobs;

public class GenerateReportExportJob
{
    private readonly IReportExportService _exporter;

    public GenerateReportExportJob(IReportExportService exporter) => _exporter = exporter;

    public async Task ExecuteAsync(
        string reportType, string format,
        Guid[]? departmentIds, DateTime dateFrom, DateTime dateTo)
    {
        var bytes = await _exporter.ExportAsync(
            reportType, format, departmentIds, dateFrom, dateTo);
        // Store result (e.g., S3/blob) and notify requester; stub for now.
        _ = bytes;
    }
}
```

- [ ] **Step 7: Run tests to verify they pass**

```bash
dotnet test tests/CRM.Application.Tests/ --filter "ExportReportCommandHandlerTests" -v n
```

Expected: 4 tests PASS.

- [ ] **Step 8: Add Export action to ReportsController**

Open `src/CRM.API/Controllers/ReportsController.cs`. Add the using for `ExportReportCommand` and add:

```csharp
[HttpGet("export")]
[Authorize(Roles = "Admin,Manager")]
public async Task<IActionResult> Export(
    [FromQuery] string reportType,
    [FromQuery] string format,
    [FromQuery] DateTime dateFrom,
    [FromQuery] DateTime dateTo,
    [FromQuery] Guid? departmentId,
    CancellationToken ct = default)
{
    try
    {
        var result = await _mediator.Send(
            new ExportReportCommand(
                CurrentUserId, CurrentUserRole, reportType, format,
                dateFrom, dateTo, departmentId), ct);

        if (result.IsAsync)
            return Accepted(new { jobId = result.JobId });

        return File(result.FileBytes!, result.ContentType!,
            result.FileName!);
    }
    catch (UnauthorizedAccessException ex)
        { return StatusCode(403, new { error = ex.Message }); }
    catch (ArgumentException ex)
        { return BadRequest(new { error = ex.Message }); }
}
```

- [ ] **Step 9: Commit**

```bash
git add src/CRM.Domain/Reports/IReportRepository.cs \
        src/CRM.Application/Reports/Commands/ExportReportCommand.cs \
        src/CRM.Application/Reports/Services/IReportExportService.cs \
        src/CRM.Infrastructure/Reports/ \
        src/CRM.API/Controllers/ReportsController.cs \
        tests/CRM.Application.Tests/Reports/ExportReportCommandHandlerTests.cs
git commit -m "feat(reports): add GET /api/reports/export — sync download ≤10k rows, async Hangfire job >10k rows"
```
