# Archive KB Article — Implementation Plan

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

**Story:** US-BE-049  
**Goal:** Implement `POST /api/kb/articles/{id}/archive` — transitions any non-Archived article to Archived status. Manager/Admin only. Returns 200.

**Architecture:** `ArchiveKbArticleCommand(articleId)` → loads article, calls `article.Archive()`, saves. The entity allows archiving from any status. Returns 200 with updated article summary.

**Tech Stack:** .NET 10, ASP.NET Core, MediatR, EF Core, xUnit, Moq

---

## File Structure

| Action | Path |
|--------|------|
| Create | `src/CRM.Application/KnowledgeBase/Commands/ArchiveKbArticleCommand.cs` |
| Modify | `src/CRM.API/Controllers/KbArticlesController.cs` |
| Test   | `tests/CRM.Application.Tests/KnowledgeBase/ArchiveKbArticleCommandHandlerTests.cs` |
| Test   | `tests/CRM.API.Tests/KnowledgeBase/KbArticlesControllerArchiveTests.cs` |

---

## Task 1: ArchiveKbArticle Command + Handler

**Files:**
- Create: `src/CRM.Application/KnowledgeBase/Commands/ArchiveKbArticleCommand.cs`
- Test: `tests/CRM.Application.Tests/KnowledgeBase/ArchiveKbArticleCommandHandlerTests.cs`

- [ ] **Step 1: Write the failing tests**

```csharp
// tests/CRM.Application.Tests/KnowledgeBase/ArchiveKbArticleCommandHandlerTests.cs
using CRM.Application.KnowledgeBase.Commands;
using CRM.Domain.KnowledgeBase;
using Moq;
using Xunit;

namespace CRM.Application.Tests.KnowledgeBase;

public class ArchiveKbArticleCommandHandlerTests
{
    private readonly Mock<IKbArticleRepository> _repo = new();
    private readonly ArchiveKbArticleCommandHandler _handler;

    public ArchiveKbArticleCommandHandlerTests()
    {
        _handler = new ArchiveKbArticleCommandHandler(_repo.Object);
    }

    [Theory]
    [InlineData(KbArticleStatus.Draft)]
    [InlineData(KbArticleStatus.PendingReview)]
    [InlineData(KbArticleStatus.Published)]
    public async Task Handle_AnyNonArchivedStatus_Archives(KbArticleStatus startStatus)
    {
        var article = KbArticle.Create(Guid.NewGuid(), "Title", Guid.NewGuid(),
            KbVisibility.Internal, new string('x', 150));

        if (startStatus == KbArticleStatus.PendingReview) article.SubmitForReview();
        if (startStatus == KbArticleStatus.Published)
        {
            article.SubmitForReview();
            article.Approve();
        }

        _repo.Setup(r => r.FindByIdAsync(article.Id, default)).ReturnsAsync(article);

        await _handler.Handle(new ArchiveKbArticleCommand(article.Id), default);

        Assert.Equal(KbArticleStatus.Archived, article.Status);
        _repo.Verify(r => r.SaveChangesAsync(default), Times.Once);
    }

    [Fact]
    public async Task Handle_AlreadyArchived_SucceedsIdempotently()
    {
        var article = KbArticle.Create(Guid.NewGuid(), "Title", Guid.NewGuid());
        article.Archive();
        _repo.Setup(r => r.FindByIdAsync(article.Id, default)).ReturnsAsync(article);

        // Should not throw — archiving an already-archived article is a no-op
        await _handler.Handle(new ArchiveKbArticleCommand(article.Id), default);

        Assert.Equal(KbArticleStatus.Archived, article.Status);
    }

    [Fact]
    public async Task Handle_ArticleNotFound_ThrowsKeyNotFoundException()
    {
        _repo.Setup(r => r.FindByIdAsync(It.IsAny<Guid>(), default))
             .ReturnsAsync((KbArticle?)null);

        await Assert.ThrowsAsync<KeyNotFoundException>(() =>
            _handler.Handle(new ArchiveKbArticleCommand(Guid.NewGuid()), default));
    }
}
```

- [ ] **Step 2: Run test to verify it fails**

```bash
dotnet test tests/CRM.Application.Tests/ --filter "ArchiveKbArticleCommandHandlerTests" -v n
```

Expected: FAIL — `ArchiveKbArticleCommand` does not exist yet.

- [ ] **Step 3: Implement command and handler**

```csharp
// src/CRM.Application/KnowledgeBase/Commands/ArchiveKbArticleCommand.cs
using CRM.Domain.KnowledgeBase;
using MediatR;

namespace CRM.Application.KnowledgeBase.Commands;

public record ArchiveKbArticleCommand(Guid ArticleId) : IRequest;

public class ArchiveKbArticleCommandHandler : IRequestHandler<ArchiveKbArticleCommand>
{
    private readonly IKbArticleRepository _articles;

    public ArchiveKbArticleCommandHandler(IKbArticleRepository articles)
        => _articles = articles;

    public async Task Handle(ArchiveKbArticleCommand cmd, CancellationToken ct)
    {
        var article = await _articles.FindByIdAsync(cmd.ArticleId, ct)
            ?? throw new KeyNotFoundException($"KB Article {cmd.ArticleId} not found.");

        article.Archive();

        await _articles.SaveChangesAsync(ct);
    }
}
```

- [ ] **Step 4: Run tests to verify they pass**

```bash
dotnet test tests/CRM.Application.Tests/ --filter "ArchiveKbArticleCommandHandlerTests" -v n
```

Expected: 5 tests PASS.

- [ ] **Step 5: Add endpoint to KbArticlesController**

```csharp
// Add to src/CRM.API/Controllers/KbArticlesController.cs:

[HttpPost("{id:guid}/archive")]
[Authorize(Roles = "Admin,Manager")]
public async Task<IActionResult> Archive(Guid id, CancellationToken ct)
{
    try
    {
        await _mediator.Send(new ArchiveKbArticleCommand(id), ct);
        return Ok();
    }
    catch (KeyNotFoundException ex) { return NotFound(new { error = ex.Message }); }
}
```

- [ ] **Step 6: Write controller test**

```csharp
// tests/CRM.API.Tests/KnowledgeBase/KbArticlesControllerArchiveTests.cs
using System.Net;
using CRM.Application.KnowledgeBase.Commands;
using MediatR;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using Xunit;

namespace CRM.API.Tests.KnowledgeBase;

public class KbArticlesControllerArchiveTests
{
    private readonly Mock<IMediator> _mediator = new();

    private HttpClient BuildClient(string role = "Manager")
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
    public async Task Archive_ManagerRole_Returns200()
    {
        _mediator.Setup(m => m.Send(It.IsAny<ArchiveKbArticleCommand>(), default))
                 .Returns(Task.CompletedTask);

        var response = await BuildClient()
            .PostAsync($"/api/kb/articles/{Guid.NewGuid()}/archive", null);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task Archive_AgentRole_Returns403()
    {
        var response = await BuildClient("Agent")
            .PostAsync($"/api/kb/articles/{Guid.NewGuid()}/archive", null);

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }
}
```

- [ ] **Step 7: Run tests to verify they pass**

```bash
dotnet test tests/CRM.API.Tests/ --filter "KbArticlesControllerArchiveTests" -v n
```

Expected: 2 tests PASS.

- [ ] **Step 8: Commit**

```bash
git add src/CRM.Application/KnowledgeBase/Commands/ArchiveKbArticleCommand.cs \
        src/CRM.API/Controllers/KbArticlesController.cs \
        tests/CRM.Application.Tests/KnowledgeBase/ArchiveKbArticleCommandHandlerTests.cs \
        tests/CRM.API.Tests/KnowledgeBase/KbArticlesControllerArchiveTests.cs
git commit -m "feat(kb): add POST /api/kb/articles/{id}/archive — Manager/Admin only"
```
