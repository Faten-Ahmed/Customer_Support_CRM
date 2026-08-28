# Reject KB Article — Implementation Plan

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

**Story:** US-BE-048  
**Goal:** Implement `POST /api/kb/articles/{id}/reject` — transitions PendingReview article back to Draft with a required rejection note (≥10 chars). Manager/Admin only.

**Architecture:** `RejectKbArticleCommand(articleId, rejectionNote)` → validates note length, calls `article.Reject(note)`, saves. Returns 204.

**Tech Stack:** .NET 10, ASP.NET Core, MediatR, FluentValidation, EF Core, xUnit, Moq

---

## File Structure

| Action | Path |
|--------|------|
| Create | `src/CRM.Application/KnowledgeBase/Commands/RejectKbArticleCommand.cs` |
| Modify | `src/CRM.API/Controllers/KbArticlesController.cs` |
| Test   | `tests/CRM.Application.Tests/KnowledgeBase/RejectKbArticleCommandHandlerTests.cs` |
| Test   | `tests/CRM.API.Tests/KnowledgeBase/KbArticlesControllerRejectTests.cs` |

---

## Task 1: RejectKbArticle Command + Handler

**Files:**
- Create: `src/CRM.Application/KnowledgeBase/Commands/RejectKbArticleCommand.cs`
- Test: `tests/CRM.Application.Tests/KnowledgeBase/RejectKbArticleCommandHandlerTests.cs`

- [ ] **Step 1: Write the failing tests**

```csharp
// tests/CRM.Application.Tests/KnowledgeBase/RejectKbArticleCommandHandlerTests.cs
using CRM.Application.KnowledgeBase.Commands;
using CRM.Domain.KnowledgeBase;
using FluentValidation;
using Moq;
using Xunit;

namespace CRM.Application.Tests.KnowledgeBase;

public class RejectKbArticleCommandHandlerTests
{
    private readonly Mock<IKbArticleRepository> _repo = new();
    private readonly RejectKbArticleCommandHandler _handler;

    public RejectKbArticleCommandHandlerTests()
    {
        _handler = new RejectKbArticleCommandHandler(_repo.Object);
    }

    private KbArticle MakePendingReviewArticle()
    {
        var article = KbArticle.Create(Guid.NewGuid(), "Title", Guid.NewGuid(),
            KbVisibility.Internal, new string('x', 150));
        article.SubmitForReview();
        return article;
    }

    [Fact]
    public async Task Handle_ValidNote_TransitionsBackToDraft()
    {
        var article = MakePendingReviewArticle();
        _repo.Setup(r => r.FindByIdAsync(article.Id, default)).ReturnsAsync(article);

        await _handler.Handle(
            new RejectKbArticleCommand(article.Id, "Please add more examples and diagrams."), default);

        Assert.Equal(KbArticleStatus.Draft, article.Status);
        Assert.NotNull(article.RejectionNote);
        _repo.Verify(r => r.SaveChangesAsync(default), Times.Once);
    }

    [Fact]
    public async Task Handle_RejectionNoteTooShort_ThrowsValidationException()
    {
        var article = MakePendingReviewArticle();
        _repo.Setup(r => r.FindByIdAsync(article.Id, default)).ReturnsAsync(article);

        await Assert.ThrowsAsync<ValidationException>(() =>
            _handler.Handle(
                new RejectKbArticleCommand(article.Id, "Too short"), default));
    }

    [Fact]
    public async Task Handle_EmptyRejectionNote_ThrowsValidationException()
    {
        var article = MakePendingReviewArticle();
        _repo.Setup(r => r.FindByIdAsync(article.Id, default)).ReturnsAsync(article);

        await Assert.ThrowsAsync<ValidationException>(() =>
            _handler.Handle(new RejectKbArticleCommand(article.Id, ""), default));
    }

    [Fact]
    public async Task Handle_ArticleNotPendingReview_ThrowsInvalidOperationException()
    {
        var article = KbArticle.Create(Guid.NewGuid(), "Title", Guid.NewGuid()); // Draft
        _repo.Setup(r => r.FindByIdAsync(article.Id, default)).ReturnsAsync(article);

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            _handler.Handle(
                new RejectKbArticleCommand(article.Id, "This is a valid rejection note."), default));
    }
}
```

- [ ] **Step 2: Run test to verify it fails**

```bash
dotnet test tests/CRM.Application.Tests/ --filter "RejectKbArticleCommandHandlerTests" -v n
```

Expected: FAIL — `RejectKbArticleCommand` does not exist yet.

- [ ] **Step 3: Implement command and handler**

```csharp
// src/CRM.Application/KnowledgeBase/Commands/RejectKbArticleCommand.cs
using CRM.Domain.KnowledgeBase;
using FluentValidation;
using FluentValidation.Results;
using MediatR;

namespace CRM.Application.KnowledgeBase.Commands;

public record RejectKbArticleCommand(Guid ArticleId, string RejectionNote) : IRequest;

public class RejectKbArticleCommandHandler : IRequestHandler<RejectKbArticleCommand>
{
    private readonly IKbArticleRepository _articles;

    public RejectKbArticleCommandHandler(IKbArticleRepository articles)
        => _articles = articles;

    public async Task Handle(RejectKbArticleCommand cmd, CancellationToken ct)
    {
        var article = await _articles.FindByIdAsync(cmd.ArticleId, ct)
            ?? throw new KeyNotFoundException($"KB Article {cmd.ArticleId} not found.");

        if (string.IsNullOrWhiteSpace(cmd.RejectionNote) || cmd.RejectionNote.Length < 10)
            throw new ValidationException(new[]
            {
                new ValidationFailure(nameof(cmd.RejectionNote),
                    "Rejection note must be at least 10 characters.")
            });

        article.Reject(cmd.RejectionNote);

        await _articles.SaveChangesAsync(ct);
    }
}
```

- [ ] **Step 4: Run tests to verify they pass**

```bash
dotnet test tests/CRM.Application.Tests/ --filter "RejectKbArticleCommandHandlerTests" -v n
```

Expected: 4 tests PASS.

- [ ] **Step 5: Add endpoint to KbArticlesController**

```csharp
// Add to src/CRM.API/Controllers/KbArticlesController.cs:

[HttpPost("{id:guid}/reject")]
[Authorize(Roles = "Admin,Manager")]
public async Task<IActionResult> Reject(
    Guid id, [FromBody] RejectKbArticleRequest req, CancellationToken ct)
{
    try
    {
        await _mediator.Send(new RejectKbArticleCommand(id, req.RejectionNote), ct);
        return NoContent();
    }
    catch (KeyNotFoundException ex) { return NotFound(new { error = ex.Message }); }
    catch (InvalidOperationException ex) { return UnprocessableEntity(new { error = ex.Message }); }
}

public record RejectKbArticleRequest(string RejectionNote);
```

- [ ] **Step 6: Write controller test**

```csharp
// tests/CRM.API.Tests/KnowledgeBase/KbArticlesControllerRejectTests.cs
using System.Net;
using System.Net.Http.Json;
using CRM.Application.KnowledgeBase.Commands;
using FluentValidation;
using FluentValidation.Results;
using MediatR;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using Xunit;

namespace CRM.API.Tests.KnowledgeBase;

public class KbArticlesControllerRejectTests
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
    public async Task Reject_ValidNote_Returns204()
    {
        _mediator.Setup(m => m.Send(It.IsAny<RejectKbArticleCommand>(), default))
                 .Returns(Task.CompletedTask);

        var response = await BuildClient().PostAsJsonAsync(
            $"/api/kb/articles/{Guid.NewGuid()}/reject",
            new { rejectionNote = "Please add more context and examples here." });

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
    }

    [Fact]
    public async Task Reject_AgentRole_Returns403()
    {
        var response = await BuildClient("Agent").PostAsJsonAsync(
            $"/api/kb/articles/{Guid.NewGuid()}/reject",
            new { rejectionNote = "Too short" });

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }
}
```

- [ ] **Step 7: Run tests to verify they pass**

```bash
dotnet test tests/CRM.API.Tests/ --filter "KbArticlesControllerRejectTests" -v n
```

Expected: 2 tests PASS.

- [ ] **Step 8: Commit**

```bash
git add src/CRM.Application/KnowledgeBase/Commands/RejectKbArticleCommand.cs \
        src/CRM.API/Controllers/KbArticlesController.cs \
        tests/CRM.Application.Tests/KnowledgeBase/RejectKbArticleCommandHandlerTests.cs \
        tests/CRM.API.Tests/KnowledgeBase/KbArticlesControllerRejectTests.cs
git commit -m "feat(kb): add POST /api/kb/articles/{id}/reject with required rejection note"
```
