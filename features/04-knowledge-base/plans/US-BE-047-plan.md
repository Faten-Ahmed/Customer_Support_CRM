# Approve KB Article — Implementation Plan

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

**Story:** US-BE-047  
**Goal:** Implement `POST /api/kb/articles/{id}/approve` — transitions a PendingReview article to Published, setting `PublishedAt`. Manager/Admin only.

**Architecture:** `ApproveKbArticleCommand(articleId)` → loads article, validates Status == PendingReview, calls `article.Approve()`, saves. Returns 204. Agent role returns 403 at the controller level.

**Tech Stack:** .NET 10, ASP.NET Core, MediatR, EF Core, xUnit, Moq

---

## File Structure

| Action | Path |
|--------|------|
| Create | `src/CRM.Application/KnowledgeBase/Commands/ApproveKbArticleCommand.cs` |
| Modify | `src/CRM.API/Controllers/KbArticlesController.cs` |
| Test   | `tests/CRM.Application.Tests/KnowledgeBase/ApproveKbArticleCommandHandlerTests.cs` |
| Test   | `tests/CRM.API.Tests/KnowledgeBase/KbArticlesControllerApproveTests.cs` |

---

## Task 1: ApproveKbArticle Command + Handler

**Files:**
- Create: `src/CRM.Application/KnowledgeBase/Commands/ApproveKbArticleCommand.cs`
- Test: `tests/CRM.Application.Tests/KnowledgeBase/ApproveKbArticleCommandHandlerTests.cs`

- [ ] **Step 1: Write the failing tests**

```csharp
// tests/CRM.Application.Tests/KnowledgeBase/ApproveKbArticleCommandHandlerTests.cs
using CRM.Application.KnowledgeBase.Commands;
using CRM.Domain.KnowledgeBase;
using Moq;
using Xunit;

namespace CRM.Application.Tests.KnowledgeBase;

public class ApproveKbArticleCommandHandlerTests
{
    private readonly Mock<IKbArticleRepository> _repo = new();
    private readonly ApproveKbArticleCommandHandler _handler;

    public ApproveKbArticleCommandHandlerTests()
    {
        _handler = new ApproveKbArticleCommandHandler(_repo.Object);
    }

    private KbArticle MakePendingReviewArticle()
    {
        var article = KbArticle.Create(Guid.NewGuid(), "Title", Guid.NewGuid(),
            KbVisibility.Internal, new string('x', 150));
        article.SubmitForReview();
        return article;
    }

    [Fact]
    public async Task Handle_PendingReviewArticle_TransitionsToPublished()
    {
        var article = MakePendingReviewArticle();
        _repo.Setup(r => r.FindByIdAsync(article.Id, default)).ReturnsAsync(article);

        await _handler.Handle(new ApproveKbArticleCommand(article.Id), default);

        Assert.Equal(KbArticleStatus.Published, article.Status);
        Assert.NotNull(article.PublishedAt);
        _repo.Verify(r => r.SaveChangesAsync(default), Times.Once);
    }

    [Fact]
    public async Task Handle_DraftArticle_ThrowsInvalidOperationException()
    {
        var article = KbArticle.Create(Guid.NewGuid(), "Title", Guid.NewGuid());
        _repo.Setup(r => r.FindByIdAsync(article.Id, default)).ReturnsAsync(article);

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            _handler.Handle(new ApproveKbArticleCommand(article.Id), default));
    }

    [Fact]
    public async Task Handle_ArticleNotFound_ThrowsKeyNotFoundException()
    {
        _repo.Setup(r => r.FindByIdAsync(It.IsAny<Guid>(), default))
             .ReturnsAsync((KbArticle?)null);

        await Assert.ThrowsAsync<KeyNotFoundException>(() =>
            _handler.Handle(new ApproveKbArticleCommand(Guid.NewGuid()), default));
    }
}
```

- [ ] **Step 2: Run test to verify it fails**

```bash
dotnet test tests/CRM.Application.Tests/ --filter "ApproveKbArticleCommandHandlerTests" -v n
```

Expected: FAIL — `ApproveKbArticleCommand` does not exist yet.

- [ ] **Step 3: Implement command and handler**

```csharp
// src/CRM.Application/KnowledgeBase/Commands/ApproveKbArticleCommand.cs
using CRM.Domain.KnowledgeBase;
using MediatR;

namespace CRM.Application.KnowledgeBase.Commands;

public record ApproveKbArticleCommand(Guid ArticleId) : IRequest;

public class ApproveKbArticleCommandHandler : IRequestHandler<ApproveKbArticleCommand>
{
    private readonly IKbArticleRepository _articles;

    public ApproveKbArticleCommandHandler(IKbArticleRepository articles)
        => _articles = articles;

    public async Task Handle(ApproveKbArticleCommand cmd, CancellationToken ct)
    {
        var article = await _articles.FindByIdAsync(cmd.ArticleId, ct)
            ?? throw new KeyNotFoundException($"KB Article {cmd.ArticleId} not found.");

        article.Approve();

        await _articles.SaveChangesAsync(ct);
    }
}
```

- [ ] **Step 4: Run tests to verify they pass**

```bash
dotnet test tests/CRM.Application.Tests/ --filter "ApproveKbArticleCommandHandlerTests" -v n
```

Expected: 3 tests PASS.

- [ ] **Step 5: Add endpoint to KbArticlesController**

```csharp
// Add to src/CRM.API/Controllers/KbArticlesController.cs:

[HttpPost("{id:guid}/approve")]
[Authorize(Roles = "Admin,Manager")]
public async Task<IActionResult> Approve(Guid id, CancellationToken ct)
{
    try
    {
        await _mediator.Send(new ApproveKbArticleCommand(id), ct);
        return NoContent();
    }
    catch (KeyNotFoundException ex) { return NotFound(new { error = ex.Message }); }
    catch (InvalidOperationException ex) { return UnprocessableEntity(new { error = ex.Message, code = "INVALID_STATUS_TRANSITION" }); }
}
```

- [ ] **Step 6: Write controller test**

```csharp
// tests/CRM.API.Tests/KnowledgeBase/KbArticlesControllerApproveTests.cs
using System.Net;
using CRM.Application.KnowledgeBase.Commands;
using MediatR;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using Xunit;

namespace CRM.API.Tests.KnowledgeBase;

public class KbArticlesControllerApproveTests
{
    private readonly Mock<IMediator> _mediator = new();

    private HttpClient BuildClient(string role)
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
    public async Task Approve_ManagerRole_Returns204()
    {
        _mediator.Setup(m => m.Send(It.IsAny<ApproveKbArticleCommand>(), default))
                 .Returns(Task.CompletedTask);

        var response = await BuildClient("Manager")
            .PostAsync($"/api/kb/articles/{Guid.NewGuid()}/approve", null);

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
    }

    [Fact]
    public async Task Approve_AgentRole_Returns403()
    {
        var response = await BuildClient("Agent")
            .PostAsync($"/api/kb/articles/{Guid.NewGuid()}/approve", null);

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task Approve_InvalidStatus_Returns422()
    {
        _mediator.Setup(m => m.Send(It.IsAny<ApproveKbArticleCommand>(), default))
                 .ThrowsAsync(new InvalidOperationException("Not in PendingReview status."));

        var response = await BuildClient("Manager")
            .PostAsync($"/api/kb/articles/{Guid.NewGuid()}/approve", null);

        Assert.Equal(HttpStatusCode.UnprocessableEntity, response.StatusCode);
    }
}
```

- [ ] **Step 7: Run tests to verify they pass**

```bash
dotnet test tests/CRM.API.Tests/ --filter "KbArticlesControllerApproveTests" -v n
```

Expected: 3 tests PASS.

- [ ] **Step 8: Commit**

```bash
git add src/CRM.Application/KnowledgeBase/Commands/ApproveKbArticleCommand.cs \
        src/CRM.API/Controllers/KbArticlesController.cs \
        tests/CRM.Application.Tests/KnowledgeBase/ApproveKbArticleCommandHandlerTests.cs \
        tests/CRM.API.Tests/KnowledgeBase/KbArticlesControllerApproveTests.cs
git commit -m "feat(kb): add POST /api/kb/articles/{id}/approve — Manager/Admin only"
```
