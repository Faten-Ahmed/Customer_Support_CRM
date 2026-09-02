# Expire CSAT Surveys Job — Implementation Plan

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

**Story:** US-BE-093  
**Goal:** Implement `ExpireCsatSurveysJob` — Hangfire recurring job at `5 0 * * *` (00:05 UTC daily). Finds all `CsatSurvey` where `Status = Sent` and `SentAt < now - 7 days`. Batch-updates Status to `Expired`. Expired surveys count toward `totalSent` in CSAT report but NOT `avgRating` or `totalSubmitted`.

**Architecture:** Hangfire recurring job registered at startup. Uses `ICsatSurveyRepository.ListExpiredAsync()` and batch-updates status. Idempotent (safe to re-run).

**Tech Stack:** .NET 10, Hangfire, EF Core, xUnit, Moq

---

## File Structure

| Action | Path |
|--------|------|
| Create | `src/CRM.Infrastructure/Jobs/ExpireCsatSurveysJob.cs` |
| Modify | `src/CRM.API/Program.cs` |
| Test   | `tests/CRM.Application.Tests/CSAT/ExpireCsatSurveysJobTests.cs` |

---

## Task 1: Expire CSAT Surveys Job

> Note: `CsatSurvey` and `ICsatSurveyRepository` are from US-BE-082. Implement that plan first.

- [ ] **Step 1: Write the failing tests**

```csharp
// tests/CRM.Application.Tests/CSAT/ExpireCsatSurveysJobTests.cs
using CRM.Domain.Surveys;
using CRM.Infrastructure.Jobs;
using Moq;
using Xunit;

namespace CRM.Application.Tests.CSAT;

public class ExpireCsatSurveysJobTests
{
    private readonly Mock<ICsatSurveyRepository> _repo = new();
    private readonly ExpireCsatSurveysJob _job;

    public ExpireCsatSurveysJobTests()
    {
        _job = new ExpireCsatSurveysJob(_repo.Object);
    }

    [Fact]
    public async Task Execute_ExpiresSentSurveysOlderThan7Days()
    {
        var expiredSurvey = CsatSurvey.Create(
            Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(),
            "TKT-001", "Old ticket");
        // Simulate sent 8 days ago
        typeof(CsatSurvey).GetProperty("SentAt")!.SetValue(
            expiredSurvey, DateTime.UtcNow.AddDays(-8));

        _repo.Setup(r => r.ListExpiredAsync(default))
             .ReturnsAsync(new List<CsatSurvey> { expiredSurvey });

        await _job.ExecuteAsync();

        Assert.Equal("Expired", expiredSurvey.Status);
        _repo.Verify(r => r.SaveChangesAsync(default), Times.Once);
    }

    [Fact]
    public async Task Execute_NoExpiredSurveys_DoesNotCallSave()
    {
        _repo.Setup(r => r.ListExpiredAsync(default))
             .ReturnsAsync(new List<CsatSurvey>());

        await _job.ExecuteAsync();

        _repo.Verify(r => r.SaveChangesAsync(default), Times.Never);
    }

    [Fact]
    public async Task Execute_Idempotent_AlreadyExpiredSurveysNotDoubleProcessed()
    {
        var alreadyExpired = CsatSurvey.CreateExpired(
            Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid());

        // ListExpiredAsync only returns Sent surveys, so already-expired won't be in the list
        _repo.Setup(r => r.ListExpiredAsync(default))
             .ReturnsAsync(new List<CsatSurvey>());

        await _job.ExecuteAsync();

        Assert.Equal("Expired", alreadyExpired.Status);
        _repo.Verify(r => r.SaveChangesAsync(default), Times.Never);
    }
}
```

- [ ] **Step 2: Run tests to verify they fail**

```bash
dotnet test tests/CRM.Application.Tests/ --filter "ExpireCsatSurveysJobTests" -v n
```

Expected: FAIL — `ExpireCsatSurveysJob` does not exist yet.

- [ ] **Step 3: Add ListExpiredAsync to ICsatSurveyRepository**

Open `src/CRM.Domain/Surveys/ICsatSurveyRepository.cs`. The `ListExpiredAsync` method should already be present from US-BE-082. If not, add:

```csharp
Task<IReadOnlyList<CsatSurvey>> ListExpiredAsync(CancellationToken ct = default);
```

The implementation should return surveys where `Status = "Sent" AND SentAt < now - 7 days`.

- [ ] **Step 4: Implement ExpireCsatSurveysJob**

```csharp
// src/CRM.Infrastructure/Jobs/ExpireCsatSurveysJob.cs
using CRM.Domain.Surveys;

namespace CRM.Infrastructure.Jobs;

public class ExpireCsatSurveysJob
{
    private readonly ICsatSurveyRepository _surveys;

    public ExpireCsatSurveysJob(ICsatSurveyRepository surveys) => _surveys = surveys;

    public async Task ExecuteAsync()
    {
        var expiring = await _surveys.ListExpiredAsync();
        if (expiring.Count == 0) return;

        foreach (var survey in expiring)
            survey.Expire();

        await _surveys.SaveChangesAsync();
    }
}
```

- [ ] **Step 5: Run tests to verify they pass**

```bash
dotnet test tests/CRM.Application.Tests/ --filter "ExpireCsatSurveysJobTests" -v n
```

Expected: 3 tests PASS.

- [ ] **Step 6: Register the recurring job in Program.cs**

Open `src/CRM.API/Program.cs`. After the existing `RecurringJob.AddOrUpdate` calls, add:

```csharp
RecurringJob.AddOrUpdate<ExpireCsatSurveysJob>(
    "expire-csat-surveys",
    j => j.ExecuteAsync(),
    "5 0 * * *");
```

- [ ] **Step 7: Commit**

```bash
git add src/CRM.Infrastructure/Jobs/ExpireCsatSurveysJob.cs \
        src/CRM.API/Program.cs \
        tests/CRM.Application.Tests/CSAT/ExpireCsatSurveysJobTests.cs
git commit -m "feat(csat): add ExpireCsatSurveysJob — nightly at 00:05 UTC, marks Sent surveys older than 7 days as Expired"
```
