# Submit KB Article for Review — Implementation Plan

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

**Story:** US-BE-046  
**Goal:** Implement `POST /api/kb/articles/{id}/submit-review` — transitions a Draft article to PendingReview after validating content length (≥100 chars) and authorship.

**Architecture:** `SubmitKbArticleForReviewCommand(articleId, requestingUserId, requestingUserRole)` → loads article, validates only author or Manager+ can submit, calls `article.SubmitForReview()` (throws if content < 100 chars), saves. Returns 204.

**Tech Stack:** .NET 10, ASP.NET Core, MediatR, EF Core, xUnit, Moq

---

## File Structure

| Action | Path |
|--------|------|
| Create | `src/CRM.Application/KnowledgeBase/Commands/SubmitKbArticleForReviewCommand.cs` |
| Modify | `src/CRM.API/Controllers/KbArticlesController.cs` |
| Test   | `tests/CRM.Application.Tests/KnowledgeBase/SubmitKbArticleForReviewCommandHandlerTests.cs` |
| Test   | `tests/CRM.API.Tests/KnowledgeBase/KbArticlesControllerSubmitTests.cs` |

---

## Task 1: SubmitKbArticleForReview Command + Handler

**Files:**
- Create: `src/CRM.Application/KnowledgeBase/Commands/SubmitKbArticleForReviewCommand.cs`
- Test: `tests/CRM.Application.Tests/KnowledgeBase/SubmitKbArticleForReviewCommandHandlerTests.cs`

- [ ] **Step 1: Write the failing tests**

```csharp
// tests/CRM.Application.Tests/KnowledgeBase/SubmitKbArticleForReviewCommandHandlerTests.cs
using CRM.Application.KnowledgeBase.Commands;
using CRM.Domain.KnowledgeBase;
using CRM.Domain.Users;
using Moq;
using Xunit;

namespace CRM.Application.Tests.KnowledgeBase;

public class SubmitKbArticleForReviewCommandHandlerTests
{
    private readonly Mock<IKbArticleRepository> _repo = new();
    private readonly SubmitKbArticleForReviewCommandHandler _handler;

    public SubmitKbArticleForReviewCommandHandlerTests()
    {
        _handler = new SubmitKbArticleForReviewCommandHandler(_repo.Object);
    }

    private KbArticle MakeDraftWithContent(Guid authorId)
    {
        var article = KbArticle.Create(Guid.NewGuid(), "Title", authorId);
        // Use reflection or a content helper to set Content >= 100 chars
        // Here we rely on the entity having content set on Create:
        return KbArticle.Create(Guid.NewGuid(), "Title", authorId,
            KbVisibility.Internal,
            new string('x', 150)); // 150-char content
    }

    [Fact]
    public async Task Handle_AuthorSubmits_TransitionsToPendingReview()
    {
        var authorId = Guid.NewGuid();
        var article = MakeDraftWithContent(authorId);
        _repo.Setup(r => r.FindByIdAsync(article.Id, default)).ReturnsAsync(article);

        await _handler.Handle(
            new SubmitKbArticleForReviewCommand(article.Id, authorId, UserRole.Agent), default);

        Assert.Equal(KbArticleStatus.PendingReview, article.Status);
        _repo.Verify(r => r.SaveChangesAsync(default), Times.Once);
    }

    [Fact]
    public async Task Handle_ManagerSubmits_AnyArticle_Succeeds()
    {
        var authorId = Guid.NewGuid();
        var managerId = Guid.NewGuid();
        var article = MakeDraftWithContent(authorId);
        _repo.Setup(r => r.FindByIdAsync(article.Id, default)).ReturnsAsync(article);

        await _handler.Handle(
            new SubmitKbArticleForReviewCommand(article.Id, managerId, UserRole.Manager), default);

        Assert.Equal(KbArticleStatus.PendingReview, article.Status);
    }

    [Fact]
    public async Task Handle_DifferentAgentSubmits_ThrowsUnauthorizedException()
    {
        var authorId = Guid.NewGuid();
        var otherId = Guid.NewGuid();
        var article = MakeDraftWithContent(authorId);
        _repo.Setup(r => r.FindByIdAsync(article.Id, default)).ReturnsAsync(article);

        await Assert.ThrowsAsync<UnauthorizedAccessException>(() =>
            _handler.Handle(
                new SubmitKbArticleForReviewCommand(article.Id, otherId, UserRole.Agent), default));
    }

    [Fact]
    public async Task Handle_ContentTooShort_ThrowsInvalidOperationException()
    {
        var authorId = Guid.NewGuid();
        var article = KbArticle.Create(Guid.NewGuid(), "Title", authorId,
            KbVisibility.Internal, "Short content."); // < 100 chars
        _repo.Setup(r => r.FindByIdAsync(article.Id, default)).ReturnsAsync(article);

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            _handler.Handle(
                new SubmitKbArticleForReviewCommand(article.Id, authorId, UserRole.Agent), default));
    }
}
```

- [ ] **Step 2: Run test to verify it fails**

```bash
dotnet test tests/CRM.Application.Tests/ --filter "SubmitKbArticleForReviewCommandHandlerTests" -v n
```

Expected: FAIL — `SubmitKbArticleForReviewCommand` does not exist yet.

- [ ] **Step 3: Implement command and handler**

```csharp
// src/CRM.Application/KnowledgeBase/Commands/SubmitKbArticleForReviewCommand.cs
using CRM.Domain.KnowledgeBase;
using CRM.Domain.Users;
using MediatR;

namespace CRM.Application.KnowledgeBase.Commands;

public record SubmitKbArticleForReviewCommand(
    Guid ArticleId,
    Guid RequestingUserId,
    UserRole RequestingUserRole) : IRequest;

public class SubmitKbArticleForReviewCommandHandler
    : IRequestHandler<SubmitKbArticleForReviewCommand>
{
    private readonly IKbArticleRepository _articles;

    public SubmitKbArticleForReviewCommandHandler(IKbArticleRepository articles)
        => _articles = articles;

    public async Task Handle(SubmitKbArticleForReviewCommand cmd, CancellationToken ct)
    {
        var article = await _articles.FindByIdAsync(cmd.ArticleId, ct)
            ?? throw new KeyNotFoundException($"KB Article {cmd.ArticleId} not found.");

        bool isPrivileged = cmd.RequestingUserRole is UserRole.Admin or UserRole.Manager;
        bool isAuthor = article.CreatedByUserId == cmd.RequestingUserId;

        if (!isPrivileged && !isAuthor)
            throw new UnauthorizedAccessException(
                "Only the article author or a Manager/Admin can submit for review.");

        article.SubmitForReview();

        await _articles.SaveChangesAsync(ct);
    }
}
```

- [ ] **Step 4: Run tests to verify they pass**

```bash
dotnet test tests/CRM.Application.Tests/ --filter "SubmitKbArticleForReviewCommandHandlerTests" -v n
```

Expected: 4 tests PASS.

- [ ] **Step 5: Add endpoint to KbArticlesController**

```csharp
// Add to src/CRM.API/Controllers/KbArticlesController.cs:

[HttpPost("{id:guid}/submit-review")]
public async Task<IActionResult> SubmitForReview(Guid id, CancellationToken ct)
{
    var roleClaim = User.FindFirst(System.Security.Claims.ClaimTypes.Role)?.Value ?? "Agent";
    Enum.TryParse<UserRole>(roleClaim, out var role);

    try
    {
        await _mediator.Send(
            new SubmitKbArticleForReviewCommand(id, CurrentUserId, role), ct);
        return NoContent();
    }
    catch (KeyNotFoundException ex) { return NotFound(new { error = ex.Message }); }
    catch (UnauthorizedAccessException ex) { return StatusCode(403, new { error = ex.Message }); }
    catch (InvalidOperationException ex) { return UnprocessableEntity(new { error = ex.Message }); }
}
```

Add `using CRM.Domain.Users;` to the controller file.

- [ ] **Step 6: Write controller test**

```csharp
// tests/CRM.API.Tests/KnowledgeBase/KbArticlesControllerSubmitTests.cs
using System.Net;
using CRM.Application.KnowledgeBase.Commands;
using MediatR;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using Xunit;

namespace CRM.API.Tests.KnowledgeBase;

public class KbArticlesControllerSubmitTests
{
    private readonly Mock<IMediator> _mediator = new();

    private HttpClient BuildClient(string role = "Agent")
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
    public async Task SubmitReview_AsAuthor_Returns204()
    {
        _mediator.Setup(m => m.Send(It.IsAny<SubmitKbArticleForReviewCommand>(), default))
                 .Returns(Task.CompletedTask);

        var response = await BuildClient("Agent")
            .PostAsync($"/api/kb/articles/{Guid.NewGuid()}/submit-review", null);

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
    }

    [Fact]
    public async Task SubmitReview_NotAuthor_Returns403()
    {
        _mediator.Setup(m => m.Send(It.IsAny<SubmitKbArticleForReviewCommand>(), default))
                 .ThrowsAsync(new UnauthorizedAccessException("Not the author."));

        var response = await BuildClient("Agent")
            .PostAsync($"/api/kb/articles/{Guid.NewGuid()}/submit-review", null);

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }
}
```

- [ ] **Step 7: Run tests to verify they pass**

```bash
dotnet test tests/CRM.API.Tests/ --filter "KbArticlesControllerSubmitTests" -v n
```

Expected: 2 tests PASS.

- [ ] **Step 8: Commit**

```bash
git add src/CRM.Application/KnowledgeBase/Commands/SubmitKbArticleForReviewCommand.cs \
        src/CRM.API/Controllers/KbArticlesController.cs \
        tests/CRM.Application.Tests/KnowledgeBase/SubmitKbArticleForReviewCommandHandlerTests.cs \
        tests/CRM.API.Tests/KnowledgeBase/KbArticlesControllerSubmitTests.cs
git commit -m "feat(kb): add POST /api/kb/articles/{id}/submit-review with authorship enforcement"
```
