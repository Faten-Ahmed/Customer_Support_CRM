# Delete KB Article (Draft Only) — Implementation Plan

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

**Story:** US-BE-052  
**Goal:** Implement `DELETE /api/kb/articles/{id}` — hard-deletes Draft articles. Only the author or Manager+ can delete. Published/Archived articles return 422 with code `MUST_ARCHIVE_FIRST`.

**Architecture:** `DeleteKbArticleCommand(articleId, requestingUserId, requestingUserRole)` → validates article is Draft (else 422 MUST_ARCHIVE_FIRST), validates authorship (else 403), calls `IKbArticleRepository.RemoveAsync()`, saves. Returns 204.

**Tech Stack:** .NET 10, ASP.NET Core, MediatR, EF Core, xUnit, Moq

---

## File Structure

| Action | Path |
|--------|------|
| Create | `src/CRM.Application/KnowledgeBase/Commands/DeleteKbArticleCommand.cs` |
| Modify | `src/CRM.API/Controllers/KbArticlesController.cs` |
| Test   | `tests/CRM.Application.Tests/KnowledgeBase/DeleteKbArticleCommandHandlerTests.cs` |
| Test   | `tests/CRM.API.Tests/KnowledgeBase/KbArticlesControllerDeleteTests.cs` |

---

## Task 1: DeleteKbArticle Command + Handler

**Files:**
- Create: `src/CRM.Application/KnowledgeBase/Commands/DeleteKbArticleCommand.cs`
- Test: `tests/CRM.Application.Tests/KnowledgeBase/DeleteKbArticleCommandHandlerTests.cs`

- [ ] **Step 1: Write the failing tests**

```csharp
// tests/CRM.Application.Tests/KnowledgeBase/DeleteKbArticleCommandHandlerTests.cs
using CRM.Application.KnowledgeBase.Commands;
using CRM.Domain.KnowledgeBase;
using CRM.Domain.Users;
using Moq;
using Xunit;

namespace CRM.Application.Tests.KnowledgeBase;

public class DeleteKbArticleCommandHandlerTests
{
    private readonly Mock<IKbArticleRepository> _repo = new();
    private readonly DeleteKbArticleCommandHandler _handler;

    public DeleteKbArticleCommandHandlerTests()
    {
        _handler = new DeleteKbArticleCommandHandler(_repo.Object);
    }

    [Fact]
    public async Task Handle_AuthorDeletesDraftArticle_RemovesIt()
    {
        var authorId = Guid.NewGuid();
        var article = KbArticle.Create(Guid.NewGuid(), "Title", authorId);
        _repo.Setup(r => r.FindByIdAsync(article.Id, default)).ReturnsAsync(article);

        await _handler.Handle(
            new DeleteKbArticleCommand(article.Id, authorId, UserRole.Agent), default);

        _repo.Verify(r => r.RemoveAsync(article, default), Times.Once);
        _repo.Verify(r => r.SaveChangesAsync(default), Times.Once);
    }

    [Fact]
    public async Task Handle_ManagerDeletesAnyDraft_RemovesIt()
    {
        var authorId = Guid.NewGuid();
        var managerId = Guid.NewGuid();
        var article = KbArticle.Create(Guid.NewGuid(), "Title", authorId);
        _repo.Setup(r => r.FindByIdAsync(article.Id, default)).ReturnsAsync(article);

        await _handler.Handle(
            new DeleteKbArticleCommand(article.Id, managerId, UserRole.Manager), default);

        _repo.Verify(r => r.RemoveAsync(article, default), Times.Once);
    }

    [Fact]
    public async Task Handle_OtherAgentDeletesDraft_ThrowsUnauthorizedException()
    {
        var authorId = Guid.NewGuid();
        var otherId = Guid.NewGuid();
        var article = KbArticle.Create(Guid.NewGuid(), "Title", authorId);
        _repo.Setup(r => r.FindByIdAsync(article.Id, default)).ReturnsAsync(article);

        await Assert.ThrowsAsync<UnauthorizedAccessException>(() =>
            _handler.Handle(
                new DeleteKbArticleCommand(article.Id, otherId, UserRole.Agent), default));
    }

    [Fact]
    public async Task Handle_PublishedArticle_ThrowsInvalidOperationException()
    {
        var authorId = Guid.NewGuid();
        var article = KbArticle.Create(Guid.NewGuid(), "Title", authorId,
            KbVisibility.Internal, new string('x', 150));
        article.SubmitForReview();
        article.Approve();

        _repo.Setup(r => r.FindByIdAsync(article.Id, default)).ReturnsAsync(article);

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            _handler.Handle(
                new DeleteKbArticleCommand(article.Id, authorId, UserRole.Agent), default));

        Assert.Contains("MUST_ARCHIVE_FIRST", ex.Message);
    }

    [Fact]
    public async Task Handle_ArchivedArticle_ThrowsInvalidOperationException()
    {
        var authorId = Guid.NewGuid();
        var article = KbArticle.Create(Guid.NewGuid(), "Title", authorId);
        article.Archive();

        _repo.Setup(r => r.FindByIdAsync(article.Id, default)).ReturnsAsync(article);

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            _handler.Handle(
                new DeleteKbArticleCommand(article.Id, authorId, UserRole.Agent), default));

        Assert.Contains("MUST_ARCHIVE_FIRST", ex.Message);
    }
}
```

- [ ] **Step 2: Run test to verify it fails**

```bash
dotnet test tests/CRM.Application.Tests/ --filter "DeleteKbArticleCommandHandlerTests" -v n
```

Expected: FAIL — `DeleteKbArticleCommand` does not exist yet.

- [ ] **Step 3: Implement command and handler**

```csharp
// src/CRM.Application/KnowledgeBase/Commands/DeleteKbArticleCommand.cs
using CRM.Domain.KnowledgeBase;
using CRM.Domain.Users;
using MediatR;

namespace CRM.Application.KnowledgeBase.Commands;

public record DeleteKbArticleCommand(
    Guid ArticleId,
    Guid RequestingUserId,
    UserRole RequestingUserRole) : IRequest;

public class DeleteKbArticleCommandHandler : IRequestHandler<DeleteKbArticleCommand>
{
    private readonly IKbArticleRepository _articles;

    public DeleteKbArticleCommandHandler(IKbArticleRepository articles)
        => _articles = articles;

    public async Task Handle(DeleteKbArticleCommand cmd, CancellationToken ct)
    {
        var article = await _articles.FindByIdAsync(cmd.ArticleId, ct)
            ?? throw new KeyNotFoundException($"KB Article {cmd.ArticleId} not found.");

        if (article.Status != KbArticleStatus.Draft)
            throw new InvalidOperationException(
                "Only Draft articles can be deleted. Archive the article first. [MUST_ARCHIVE_FIRST]");

        bool isPrivileged = cmd.RequestingUserRole is UserRole.Admin or UserRole.Manager;
        bool isAuthor = article.CreatedByUserId == cmd.RequestingUserId;

        if (!isPrivileged && !isAuthor)
            throw new UnauthorizedAccessException(
                "Only the article author or a Manager/Admin can delete this draft.");

        await _articles.RemoveAsync(article, ct);
        await _articles.SaveChangesAsync(ct);
    }
}
```

- [ ] **Step 4: Run tests to verify they pass**

```bash
dotnet test tests/CRM.Application.Tests/ --filter "DeleteKbArticleCommandHandlerTests" -v n
```

Expected: 5 tests PASS.

- [ ] **Step 5: Add endpoint to KbArticlesController**

```csharp
// Add to src/CRM.API/Controllers/KbArticlesController.cs:

[HttpDelete("{id:guid}")]
public async Task<IActionResult> Delete(Guid id, CancellationToken ct)
{
    var roleClaim = User.FindFirst(System.Security.Claims.ClaimTypes.Role)?.Value ?? "Agent";
    Enum.TryParse<UserRole>(roleClaim, out var role);

    try
    {
        await _mediator.Send(new DeleteKbArticleCommand(id, CurrentUserId, role), ct);
        return NoContent();
    }
    catch (KeyNotFoundException ex) { return NotFound(new { error = ex.Message }); }
    catch (UnauthorizedAccessException ex) { return StatusCode(403, new { error = ex.Message }); }
    catch (InvalidOperationException ex)
    {
        return UnprocessableEntity(new { error = ex.Message, code = "MUST_ARCHIVE_FIRST" });
    }
}
```

- [ ] **Step 6: Write controller test**

```csharp
// tests/CRM.API.Tests/KnowledgeBase/KbArticlesControllerDeleteTests.cs
using System.Net;
using CRM.Application.KnowledgeBase.Commands;
using MediatR;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using Xunit;

namespace CRM.API.Tests.KnowledgeBase;

public class KbArticlesControllerDeleteTests
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
    public async Task Delete_DraftArticle_Returns204()
    {
        _mediator.Setup(m => m.Send(It.IsAny<DeleteKbArticleCommand>(), default))
                 .Returns(Task.CompletedTask);

        var response = await BuildClient().DeleteAsync($"/api/kb/articles/{Guid.NewGuid()}");

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
    }

    [Fact]
    public async Task Delete_NotAuthor_Returns403()
    {
        _mediator.Setup(m => m.Send(It.IsAny<DeleteKbArticleCommand>(), default))
                 .ThrowsAsync(new UnauthorizedAccessException("Not the author."));

        var response = await BuildClient().DeleteAsync($"/api/kb/articles/{Guid.NewGuid()}");

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task Delete_PublishedArticle_Returns422()
    {
        _mediator.Setup(m => m.Send(It.IsAny<DeleteKbArticleCommand>(), default))
                 .ThrowsAsync(new InvalidOperationException("Must archive first."));

        var response = await BuildClient().DeleteAsync($"/api/kb/articles/{Guid.NewGuid()}");

        Assert.Equal(HttpStatusCode.UnprocessableEntity, response.StatusCode);
    }
}
```

- [ ] **Step 7: Run tests to verify they pass**

```bash
dotnet test tests/CRM.API.Tests/ --filter "KbArticlesControllerDeleteTests" -v n
```

Expected: 3 tests PASS.

- [ ] **Step 8: Commit**

```bash
git add src/CRM.Application/KnowledgeBase/Commands/DeleteKbArticleCommand.cs \
        src/CRM.API/Controllers/KbArticlesController.cs \
        tests/CRM.Application.Tests/KnowledgeBase/DeleteKbArticleCommandHandlerTests.cs \
        tests/CRM.API.Tests/KnowledgeBase/KbArticlesControllerDeleteTests.cs
git commit -m "feat(kb): add DELETE /api/kb/articles/{id} — draft-only, with author ownership check"
```
