# Purge Orphaned Attachments & Completed Tasks — Implementation Plan

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

**Story:** US-BE-095  
**Goal:** Implement two Hangfire recurring jobs: `PurgeOrphanedAttachmentsJob` — nightly, finds `TicketAttachment` where `DeletedAt IS NOT NULL AND DeletedAt < now - 1 day`; deletes S3 objects; hard-deletes DB records. `PurgeCompletedTasksJob` — nightly, finds `AgentTask` where `IsCompleted = true AND UpdatedAt < now - 30 days`; hard-deletes. Both idempotent. S3 delete failures logged and skipped (DB record retained for next run).

**Architecture:** Both jobs registered as Hangfire recurring at `0 2 * * *` (2 AM UTC). `PurgeOrphanedAttachmentsJob` uses `ITicketAttachmentRepository` and `IS3StorageService`. `PurgeCompletedTasksJob` uses `IAgentTaskRepository.PurgeCompletedOlderThanAsync` (defined in US-BE-062).

**Tech Stack:** .NET 10, Hangfire, EF Core, xUnit, Moq

---

## File Structure

| Action | Path |
|--------|------|
| Create | `src/CRM.Domain/Tickets/ITicketAttachmentRepository.cs` |
| Create | `src/CRM.Domain/Storage/IS3StorageService.cs` |
| Create | `src/CRM.Infrastructure/Jobs/PurgeOrphanedAttachmentsJob.cs` |
| Create | `src/CRM.Infrastructure/Jobs/PurgeCompletedTasksJob.cs` |
| Modify | `src/CRM.API/Program.cs` |
| Test   | `tests/CRM.Application.Tests/Jobs/PurgeOrphanedAttachmentsJobTests.cs` |
| Test   | `tests/CRM.Application.Tests/Jobs/PurgeCompletedTasksJobTests.cs` |

---

## Task 1: Purge Jobs

> Note: `IAgentTaskRepository.PurgeCompletedOlderThanAsync` is from US-BE-062. Implement that plan first.

- [ ] **Step 1: Write the failing tests**

```csharp
// tests/CRM.Application.Tests/Jobs/PurgeOrphanedAttachmentsJobTests.cs
using CRM.Domain.Storage;
using CRM.Domain.Tickets;
using CRM.Infrastructure.Jobs;
using Moq;
using Xunit;

namespace CRM.Application.Tests.Jobs;

public class PurgeOrphanedAttachmentsJobTests
{
    private readonly Mock<ITicketAttachmentRepository> _attachments = new();
    private readonly Mock<IS3StorageService> _s3 = new();
    private readonly Mock<ILogger<PurgeOrphanedAttachmentsJob>> _logger = new();
    private readonly PurgeOrphanedAttachmentsJob _job;

    public PurgeOrphanedAttachmentsJobTests()
    {
        _job = new PurgeOrphanedAttachmentsJob(
            _attachments.Object, _s3.Object, _logger.Object);
    }

    [Fact]
    public async Task Execute_DeletesS3ObjectAndDbRecord()
    {
        var attachment = new TicketAttachment
        {
            Id = Guid.NewGuid(),
            S3Key = "attachments/123/file.pdf",
            DeletedAt = DateTime.UtcNow.AddDays(-2)
        };
        _attachments.Setup(r => r.ListSoftDeletedOlderThanAsync(
            It.IsAny<DateTime>(), default))
            .ReturnsAsync(new List<TicketAttachment> { attachment });

        await _job.ExecuteAsync();

        _s3.Verify(s => s.DeleteAsync("attachments/123/file.pdf", default), Times.Once);
        _attachments.Verify(r => r.HardDeleteAsync(attachment, default), Times.Once);
        _attachments.Verify(r => r.SaveChangesAsync(default), Times.Once);
    }

    [Fact]
    public async Task Execute_S3DeleteFailure_SkipsDbDelete()
    {
        var attachment = new TicketAttachment
        {
            Id = Guid.NewGuid(),
            S3Key = "attachments/456/file.pdf",
            DeletedAt = DateTime.UtcNow.AddDays(-2)
        };
        _attachments.Setup(r => r.ListSoftDeletedOlderThanAsync(
            It.IsAny<DateTime>(), default))
            .ReturnsAsync(new List<TicketAttachment> { attachment });
        _s3.Setup(s => s.DeleteAsync("attachments/456/file.pdf", default))
           .ThrowsAsync(new Exception("S3 unavailable"));

        await _job.ExecuteAsync(); // should NOT throw

        _attachments.Verify(r => r.HardDeleteAsync(It.IsAny<TicketAttachment>(), default),
            Times.Never);
    }

    [Fact]
    public async Task Execute_NoAttachments_DoesNothing()
    {
        _attachments.Setup(r => r.ListSoftDeletedOlderThanAsync(
            It.IsAny<DateTime>(), default))
            .ReturnsAsync(new List<TicketAttachment>());

        await _job.ExecuteAsync();

        _s3.Verify(s => s.DeleteAsync(It.IsAny<string>(), default), Times.Never);
        _attachments.Verify(r => r.SaveChangesAsync(default), Times.Never);
    }
}
```

```csharp
// tests/CRM.Application.Tests/Jobs/PurgeCompletedTasksJobTests.cs
using CRM.Domain.Tasks;
using CRM.Infrastructure.Jobs;
using Moq;
using Xunit;

namespace CRM.Application.Tests.Jobs;

public class PurgeCompletedTasksJobTests
{
    private readonly Mock<IAgentTaskRepository> _tasks = new();
    private readonly PurgeCompletedTasksJob _job;

    public PurgeCompletedTasksJobTests()
    {
        _job = new PurgeCompletedTasksJob(_tasks.Object);
    }

    [Fact]
    public async Task Execute_PurgesCompletedTasksOlderThan30Days()
    {
        _tasks.Setup(r => r.PurgeCompletedOlderThanAsync(
            It.Is<DateTime>(d => d <= DateTime.UtcNow.AddDays(-29)), default))
            .ReturnsAsync(5);

        await _job.ExecuteAsync();

        _tasks.Verify(r => r.PurgeCompletedOlderThanAsync(
            It.IsAny<DateTime>(), default), Times.Once);
    }

    [Fact]
    public async Task Execute_Idempotent_SafeToRunTwice()
    {
        _tasks.Setup(r => r.PurgeCompletedOlderThanAsync(It.IsAny<DateTime>(), default))
              .ReturnsAsync(0);

        await _job.ExecuteAsync();
        await _job.ExecuteAsync();

        _tasks.Verify(r => r.PurgeCompletedOlderThanAsync(
            It.IsAny<DateTime>(), default), Times.Exactly(2));
    }
}
```

- [ ] **Step 2: Run tests to verify they fail**

```bash
dotnet test tests/CRM.Application.Tests/ --filter "PurgeOrphanedAttachmentsJobTests|PurgeCompletedTasksJobTests" -v n
```

Expected: FAIL — types do not exist yet.

- [ ] **Step 3: Create ITicketAttachmentRepository and TicketAttachment**

```csharp
// src/CRM.Domain/Tickets/ITicketAttachmentRepository.cs
namespace CRM.Domain.Tickets;

public class TicketAttachment
{
    public Guid Id { get; set; }
    public Guid TicketId { get; set; }
    public string FileName { get; set; } = string.Empty;
    public string S3Key { get; set; } = string.Empty;
    public long SizeBytes { get; set; }
    public DateTime UploadedAt { get; set; }
    public DateTime? DeletedAt { get; set; }
}

public interface ITicketAttachmentRepository
{
    Task<IReadOnlyList<TicketAttachment>> ListSoftDeletedOlderThanAsync(
        DateTime threshold, CancellationToken ct = default);
    Task HardDeleteAsync(TicketAttachment attachment, CancellationToken ct = default);
    Task SaveChangesAsync(CancellationToken ct = default);
}
```

- [ ] **Step 4: Create IS3StorageService**

```csharp
// src/CRM.Domain/Storage/IS3StorageService.cs
namespace CRM.Domain.Storage;

public interface IS3StorageService
{
    Task<string> UploadAsync(
        string key, Stream content, string contentType,
        CancellationToken ct = default);
    Task DeleteAsync(string key, CancellationToken ct = default);
    Task<string> GetPresignedUrlAsync(string key, TimeSpan expiry);
}
```

- [ ] **Step 5: Implement PurgeOrphanedAttachmentsJob**

```csharp
// src/CRM.Infrastructure/Jobs/PurgeOrphanedAttachmentsJob.cs
using CRM.Domain.Storage;
using CRM.Domain.Tickets;
using Microsoft.Extensions.Logging;

namespace CRM.Infrastructure.Jobs;

public class PurgeOrphanedAttachmentsJob
{
    private readonly ITicketAttachmentRepository _attachments;
    private readonly IS3StorageService _s3;
    private readonly ILogger<PurgeOrphanedAttachmentsJob> _logger;

    public PurgeOrphanedAttachmentsJob(
        ITicketAttachmentRepository attachments,
        IS3StorageService s3,
        ILogger<PurgeOrphanedAttachmentsJob> logger)
    {
        _attachments = attachments;
        _s3 = s3;
        _logger = logger;
    }

    public async Task ExecuteAsync()
    {
        var threshold = DateTime.UtcNow.AddDays(-1);
        var toDelete = await _attachments.ListSoftDeletedOlderThanAsync(threshold);

        if (toDelete.Count == 0) return;

        var hardDeleteBatch = new List<TicketAttachment>();

        foreach (var att in toDelete)
        {
            try
            {
                await _s3.DeleteAsync(att.S3Key);
                hardDeleteBatch.Add(att);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex,
                    "Failed to delete S3 object {S3Key} — retaining DB record for next run",
                    att.S3Key);
            }
        }

        foreach (var att in hardDeleteBatch)
            await _attachments.HardDeleteAsync(att);

        if (hardDeleteBatch.Count > 0)
            await _attachments.SaveChangesAsync();
    }
}
```

- [ ] **Step 6: Implement PurgeCompletedTasksJob**

```csharp
// src/CRM.Infrastructure/Jobs/PurgeCompletedTasksJob.cs
using CRM.Domain.Tasks;

namespace CRM.Infrastructure.Jobs;

public class PurgeCompletedTasksJob
{
    private readonly IAgentTaskRepository _tasks;
    public PurgeCompletedTasksJob(IAgentTaskRepository tasks) => _tasks = tasks;

    public async Task ExecuteAsync()
    {
        var threshold = DateTime.UtcNow.AddDays(-30);
        await _tasks.PurgeCompletedOlderThanAsync(threshold);
    }
}
```

- [ ] **Step 7: Run tests to verify they pass**

```bash
dotnet test tests/CRM.Application.Tests/ --filter "PurgeOrphanedAttachmentsJobTests|PurgeCompletedTasksJobTests" -v n
```

Expected: 5 tests PASS.

- [ ] **Step 8: Register both jobs in Program.cs**

Open `src/CRM.API/Program.cs` and add:

```csharp
RecurringJob.AddOrUpdate<PurgeOrphanedAttachmentsJob>(
    "purge-orphaned-attachments",
    j => j.ExecuteAsync(),
    "0 2 * * *");

RecurringJob.AddOrUpdate<PurgeCompletedTasksJob>(
    "purge-completed-tasks",
    j => j.ExecuteAsync(),
    "0 2 * * *");
```

Note: `PurgeCompletedTasksJob` was already defined in the US-BE-062 plan with a different implementation. This US-BE-095 plan supersedes that stub with the production implementation.

- [ ] **Step 9: Commit**

```bash
git add src/CRM.Domain/Tickets/ITicketAttachmentRepository.cs \
        src/CRM.Domain/Storage/IS3StorageService.cs \
        src/CRM.Infrastructure/Jobs/PurgeOrphanedAttachmentsJob.cs \
        src/CRM.Infrastructure/Jobs/PurgeCompletedTasksJob.cs \
        src/CRM.API/Program.cs \
        tests/CRM.Application.Tests/Jobs/PurgeOrphanedAttachmentsJobTests.cs \
        tests/CRM.Application.Tests/Jobs/PurgeCompletedTasksJobTests.cs
git commit -m "feat(maintenance): add nightly purge jobs — orphaned S3 attachments and completed agent tasks >30 days"
```
