# Create KB Article (Draft) — Implementation Plan

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

**Story:** US-BE-045  
**Goal:** Implement `POST /api/kb/articles` — creates a `KbArticle` in Draft status with title and categoryId; optional content, Arabic translations, and visibility. Returns 201 with article summary.

**Architecture:** `CreateKbArticleCommand(title, categoryId, content?, titleAr?, contentAr?, visibility, createdByUserId)` → validates categoryId exists against `IKbCategoryRepository`, creates `KbArticle` domain entity, persists via `IKbArticleRepository`. Returns `KbArticleSummaryDto`.

**Tech Stack:** .NET 10, ASP.NET Core, MediatR, FluentValidation, EF Core, xUnit, Moq

---

## File Structure

| Action | Path |
|--------|------|
| Create | `src/CRM.Domain/KnowledgeBase/KbArticle.cs` |
| Create | `src/CRM.Domain/KnowledgeBase/IKbArticleRepository.cs` |
| Create | `src/CRM.Domain/KnowledgeBase/IKbCategoryRepository.cs` |
| Create | `src/CRM.Application/KnowledgeBase/DTOs/KbArticleSummaryDto.cs` |
| Create | `src/CRM.Application/KnowledgeBase/Commands/CreateKbArticleCommand.cs` |
| Create | `src/CRM.API/Controllers/KbArticlesController.cs` |
| Test   | `tests/CRM.Application.Tests/KnowledgeBase/CreateKbArticleCommandHandlerTests.cs` |
| Test   | `tests/CRM.API.Tests/KnowledgeBase/KbArticlesControllerCreateTests.cs` |

---

## Task 1: KbArticle Domain Entity

**Files:**
- Create: `src/CRM.Domain/KnowledgeBase/KbArticle.cs`
- Create: `src/CRM.Domain/KnowledgeBase/IKbArticleRepository.cs`
- Create: `src/CRM.Domain/KnowledgeBase/IKbCategoryRepository.cs`

- [ ] **Step 1: Create KbArticle entity and repository interfaces**

```csharp
// src/CRM.Domain/KnowledgeBase/KbArticle.cs
namespace CRM.Domain.KnowledgeBase;

public enum KbArticleStatus { Draft, PendingReview, Published, Archived }
public enum KbVisibility { Internal, Public, Both }

public class KbArticle
{
    public Guid Id { get; private set; }
    public string Title { get; private set; } = null!;
    public string? TitleAr { get; private set; }
    public string? Content { get; private set; }
    public string? ContentAr { get; private set; }
    public Guid CategoryId { get; private set; }
    public KbArticleStatus Status { get; private set; }
    public KbVisibility Visibility { get; private set; }
    public Guid CreatedByUserId { get; private set; }
    public DateTime? PublishedAt { get; private set; }
    public string? RejectionNote { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public DateTime UpdatedAt { get; private set; }

    private KbArticle() { }

    public static KbArticle Create(
        Guid categoryId, string title, Guid createdByUserId,
        KbVisibility visibility = KbVisibility.Internal,
        string? content = null, string? titleAr = null, string? contentAr = null)
        => new()
        {
            Id = Guid.NewGuid(),
            Title = title,
            TitleAr = titleAr,
            Content = content,
            ContentAr = contentAr,
            CategoryId = categoryId,
            Status = KbArticleStatus.Draft,
            Visibility = visibility,
            CreatedByUserId = createdByUserId,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

    public void SubmitForReview()
    {
        if (Status != KbArticleStatus.Draft)
            throw new InvalidOperationException($"Cannot submit article with status {Status}.");
        if ((Content?.Length ?? 0) < 100)
            throw new InvalidOperationException("Content must be at least 100 characters before submitting.");
        Status = KbArticleStatus.PendingReview;
        RejectionNote = null;
        UpdatedAt = DateTime.UtcNow;
    }

    public void Approve()
    {
        if (Status != KbArticleStatus.PendingReview)
            throw new InvalidOperationException("Only PendingReview articles can be approved.");
        Status = KbArticleStatus.Published;
        PublishedAt = DateTime.UtcNow;
        UpdatedAt = DateTime.UtcNow;
    }

    public void Reject(string rejectionNote)
    {
        if (Status != KbArticleStatus.PendingReview)
            throw new InvalidOperationException("Only PendingReview articles can be rejected.");
        Status = KbArticleStatus.Draft;
        RejectionNote = rejectionNote;
        UpdatedAt = DateTime.UtcNow;
    }

    public void Archive()
    {
        Status = KbArticleStatus.Archived;
        UpdatedAt = DateTime.UtcNow;
    }
}
```

```csharp
// src/CRM.Domain/KnowledgeBase/IKbArticleRepository.cs
using CRM.Application.Common;

namespace CRM.Domain.KnowledgeBase;

public record KbArticleFilter(
    KbArticleStatus? Status,
    Guid? CategoryId,
    KbVisibility? Visibility);

public interface IKbArticleRepository
{
    Task<KbArticle?> FindByIdAsync(Guid id, CancellationToken ct = default);
    Task<PagedResult<KbArticle>> ListAsync(
        KbArticleFilter filter, int page, int pageSize, CancellationToken ct = default);
    Task<IReadOnlyList<KbArticle>> SearchAsync(
        string query, bool portalOnly, int limit, CancellationToken ct = default);
    Task AddAsync(KbArticle article, CancellationToken ct = default);
    Task RemoveAsync(KbArticle article, CancellationToken ct = default);
    Task SaveChangesAsync(CancellationToken ct = default);
}
```

```csharp
// src/CRM.Domain/KnowledgeBase/IKbCategoryRepository.cs
namespace CRM.Domain.KnowledgeBase;

public class KbCategory
{
    public Guid Id { get; private set; }
    public string Name { get; private set; } = null!;
    public bool IsActive { get; private set; }

    private KbCategory() { }

    public static KbCategory Create(string name)
        => new() { Id = Guid.NewGuid(), Name = name, IsActive = true };
}

public interface IKbCategoryRepository
{
    Task<KbCategory?> FindByIdAsync(Guid id, CancellationToken ct = default);
    Task<IReadOnlyList<KbCategory>> ListActiveAsync(CancellationToken ct = default);
    Task AddAsync(KbCategory category, CancellationToken ct = default);
    Task SaveChangesAsync(CancellationToken ct = default);
}
```

- [ ] **Step 2: Commit domain entities**

```bash
git add src/CRM.Domain/KnowledgeBase/
git commit -m "feat(domain): add KbArticle entity with status transitions and IKbArticleRepository"
```

---

## Task 2: CreateKbArticle Command + Handler

**Files:**
- Create: `src/CRM.Application/KnowledgeBase/DTOs/KbArticleSummaryDto.cs`
- Create: `src/CRM.Application/KnowledgeBase/Commands/CreateKbArticleCommand.cs`
- Test: `tests/CRM.Application.Tests/KnowledgeBase/CreateKbArticleCommandHandlerTests.cs`

- [ ] **Step 1: Write the failing tests**

```csharp
// tests/CRM.Application.Tests/KnowledgeBase/CreateKbArticleCommandHandlerTests.cs
using CRM.Application.KnowledgeBase.Commands;
using CRM.Domain.KnowledgeBase;
using Moq;
using Xunit;

namespace CRM.Application.Tests.KnowledgeBase;

public class CreateKbArticleCommandHandlerTests
{
    private readonly Mock<IKbArticleRepository> _articleRepo = new();
    private readonly Mock<IKbCategoryRepository> _categoryRepo = new();
    private readonly CreateKbArticleCommandHandler _handler;

    public CreateKbArticleCommandHandlerTests()
    {
        _handler = new CreateKbArticleCommandHandler(_articleRepo.Object, _categoryRepo.Object);
    }

    [Fact]
    public async Task Handle_ValidCategoryId_CreatesDraftArticle()
    {
        var categoryId = Guid.NewGuid();
        var category = KbCategory.Create("Support");
        _categoryRepo.Setup(r => r.FindByIdAsync(categoryId, default)).ReturnsAsync(category);

        var result = await _handler.Handle(new CreateKbArticleCommand(
            "How to reset password", categoryId, Guid.NewGuid(),
            KbVisibility.Internal, null, null, null), default);

        Assert.NotEqual(Guid.Empty, result.Id);
        Assert.Equal("Draft", result.Status);
        _articleRepo.Verify(r => r.AddAsync(It.IsAny<KbArticle>(), default), Times.Once);
    }

    [Fact]
    public async Task Handle_InvalidCategoryId_ThrowsKeyNotFoundException()
    {
        _categoryRepo.Setup(r => r.FindByIdAsync(It.IsAny<Guid>(), default))
                     .ReturnsAsync((KbCategory?)null);

        await Assert.ThrowsAsync<KeyNotFoundException>(() =>
            _handler.Handle(new CreateKbArticleCommand(
                "Title", Guid.NewGuid(), Guid.NewGuid(),
                KbVisibility.Internal, null, null, null), default));
    }

    [Fact]
    public async Task Handle_WithOptionalContent_IncludesContentInArticle()
    {
        var categoryId = Guid.NewGuid();
        _categoryRepo.Setup(r => r.FindByIdAsync(categoryId, default))
                     .ReturnsAsync(KbCategory.Create("Help"));

        var result = await _handler.Handle(new CreateKbArticleCommand(
            "Title", categoryId, Guid.NewGuid(),
            KbVisibility.Public, "Some content here", "عنوان", "محتوى"), default);

        Assert.Equal("Draft", result.Status);
        Assert.Equal("Public", result.Visibility);
    }
}
```

- [ ] **Step 2: Run test to verify it fails**

```bash
dotnet test tests/CRM.Application.Tests/ --filter "CreateKbArticleCommandHandlerTests" -v n
```

Expected: FAIL — `CreateKbArticleCommand` does not exist yet.

- [ ] **Step 3: Create DTO**

```csharp
// src/CRM.Application/KnowledgeBase/DTOs/KbArticleSummaryDto.cs
namespace CRM.Application.KnowledgeBase.DTOs;

public record KbArticleSummaryDto(
    Guid Id,
    string Title,
    string? TitleAr,
    Guid CategoryId,
    string Status,
    string Visibility,
    Guid CreatedByUserId,
    DateTime CreatedAt);
```

- [ ] **Step 4: Implement command and handler**

```csharp
// src/CRM.Application/KnowledgeBase/Commands/CreateKbArticleCommand.cs
using CRM.Application.KnowledgeBase.DTOs;
using CRM.Domain.KnowledgeBase;
using MediatR;

namespace CRM.Application.KnowledgeBase.Commands;

public record CreateKbArticleCommand(
    string Title,
    Guid CategoryId,
    Guid CreatedByUserId,
    KbVisibility Visibility,
    string? Content,
    string? TitleAr,
    string? ContentAr) : IRequest<KbArticleSummaryDto>;

public class CreateKbArticleCommandHandler
    : IRequestHandler<CreateKbArticleCommand, KbArticleSummaryDto>
{
    private readonly IKbArticleRepository _articles;
    private readonly IKbCategoryRepository _categories;

    public CreateKbArticleCommandHandler(
        IKbArticleRepository articles, IKbCategoryRepository categories)
    {
        _articles = articles;
        _categories = categories;
    }

    public async Task<KbArticleSummaryDto> Handle(
        CreateKbArticleCommand cmd, CancellationToken ct)
    {
        var category = await _categories.FindByIdAsync(cmd.CategoryId, ct)
            ?? throw new KeyNotFoundException($"KB Category {cmd.CategoryId} not found or inactive.");

        var article = KbArticle.Create(
            cmd.CategoryId, cmd.Title, cmd.CreatedByUserId,
            cmd.Visibility, cmd.Content, cmd.TitleAr, cmd.ContentAr);

        await _articles.AddAsync(article, ct);
        await _articles.SaveChangesAsync(ct);

        return new KbArticleSummaryDto(
            article.Id, article.Title, article.TitleAr,
            article.CategoryId, article.Status.ToString(),
            article.Visibility.ToString(), article.CreatedByUserId, article.CreatedAt);
    }
}
```

- [ ] **Step 5: Run tests to verify they pass**

```bash
dotnet test tests/CRM.Application.Tests/ --filter "CreateKbArticleCommandHandlerTests" -v n
```

Expected: 3 tests PASS.

- [ ] **Step 6: Commit**

```bash
git add src/CRM.Application/KnowledgeBase/DTOs/KbArticleSummaryDto.cs \
        src/CRM.Application/KnowledgeBase/Commands/CreateKbArticleCommand.cs \
        tests/CRM.Application.Tests/KnowledgeBase/CreateKbArticleCommandHandlerTests.cs
git commit -m "feat(kb): add CreateKbArticleCommand — creates Draft article with category validation"
```

---

## Task 3: KbArticlesController — POST /api/kb/articles

**Files:**
- Create: `src/CRM.API/Controllers/KbArticlesController.cs`
- Test: `tests/CRM.API.Tests/KnowledgeBase/KbArticlesControllerCreateTests.cs`

- [ ] **Step 1: Write the failing test**

```csharp
// tests/CRM.API.Tests/KnowledgeBase/KbArticlesControllerCreateTests.cs
using System.Net;
using System.Net.Http.Json;
using CRM.Application.KnowledgeBase.Commands;
using CRM.Application.KnowledgeBase.DTOs;
using MediatR;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using Xunit;

namespace CRM.API.Tests.KnowledgeBase;

public class KbArticlesControllerCreateTests
{
    private readonly Mock<IMediator> _mediator = new();

    private HttpClient BuildClient()
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
                "Bearer", TestJwtHelper.CreateTestToken(role: "Agent"));
        return client;
    }

    [Fact]
    public async Task CreateArticle_ValidRequest_Returns201()
    {
        var articleId = Guid.NewGuid();
        _mediator.Setup(m => m.Send(It.IsAny<CreateKbArticleCommand>(), default))
                 .ReturnsAsync(new KbArticleSummaryDto(
                     articleId, "How to reset password", null,
                     Guid.NewGuid(), "Draft", "Internal", Guid.NewGuid(), DateTime.UtcNow));

        var response = await BuildClient().PostAsJsonAsync("/api/kb/articles", new
        {
            title = "How to reset password",
            categoryId = Guid.NewGuid()
        });

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
    }

    [Fact]
    public async Task CreateArticle_InvalidCategory_Returns422()
    {
        _mediator.Setup(m => m.Send(It.IsAny<CreateKbArticleCommand>(), default))
                 .ThrowsAsync(new KeyNotFoundException("Category not found."));

        var response = await BuildClient().PostAsJsonAsync("/api/kb/articles", new
        {
            title = "Title",
            categoryId = Guid.NewGuid()
        });

        Assert.Equal(HttpStatusCode.UnprocessableEntity, response.StatusCode);
    }
}
```

- [ ] **Step 2: Run test to verify it fails**

```bash
dotnet test tests/CRM.API.Tests/ --filter "KbArticlesControllerCreateTests" -v n
```

Expected: FAIL — controller does not exist.

- [ ] **Step 3: Create KbArticlesController**

```csharp
// src/CRM.API/Controllers/KbArticlesController.cs
using CRM.Application.KnowledgeBase.Commands;
using CRM.Domain.KnowledgeBase;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CRM.API.Controllers;

[ApiController]
[Route("api/kb/articles")]
[Authorize(Roles = "Admin,Manager,Agent")]
public class KbArticlesController : ControllerBase
{
    private readonly IMediator _mediator;
    private Guid CurrentUserId =>
        Guid.Parse(User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)!.Value);

    public KbArticlesController(IMediator mediator) => _mediator = mediator;

    [HttpPost]
    public async Task<IActionResult> Create(
        [FromBody] CreateKbArticleRequest req, CancellationToken ct)
    {
        Enum.TryParse<KbVisibility>(req.Visibility ?? "Internal", out var visibility);
        try
        {
            var result = await _mediator.Send(new CreateKbArticleCommand(
                req.Title, req.CategoryId, CurrentUserId,
                visibility, req.Content, req.TitleAr, req.ContentAr), ct);
            return StatusCode(201, result);
        }
        catch (KeyNotFoundException ex)
        {
            return UnprocessableEntity(new { error = ex.Message });
        }
    }
}

public record CreateKbArticleRequest(
    string Title,
    Guid CategoryId,
    string? Content,
    string? TitleAr,
    string? ContentAr,
    string? Visibility);
```

- [ ] **Step 4: Run tests to verify they pass**

```bash
dotnet test tests/CRM.API.Tests/ --filter "KbArticlesControllerCreateTests" -v n
```

Expected: 2 tests PASS.

- [ ] **Step 5: Commit**

```bash
git add src/CRM.API/Controllers/KbArticlesController.cs \
        tests/CRM.API.Tests/KnowledgeBase/KbArticlesControllerCreateTests.cs
git commit -m "feat(api): add POST /api/kb/articles endpoint"
```
