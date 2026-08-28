# Get & List KB Articles — Implementation Plan

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

**Story:** US-BE-050  
**Goal:** Implement `GET /api/kb/articles/{id}` (agent — all statuses), `GET /api/kb/articles` (agent — filtered, paginated), `GET /api/portal/kb/articles/{id}` (customer — Published + Public/Both only), and `GET /api/portal/kb/articles` (customer — Published + Public/Both, paginated).

**Architecture:** `GetKbArticleQuery(articleId, isPortalCaller)` → returns full article or 403/404 if portal access to internal/non-published. `ListKbArticlesQuery(filter, page, pageSize, isPortalCaller)` → applies visibility/status filter for portal callers. Separate portal controller restricts access.

**Tech Stack:** .NET 10, ASP.NET Core, MediatR, EF Core, xUnit, Moq

---

## File Structure

| Action | Path |
|--------|------|
| Create | `src/CRM.Application/KnowledgeBase/DTOs/KbArticleDetailDto.cs` |
| Create | `src/CRM.Application/KnowledgeBase/Queries/GetKbArticleQuery.cs` |
| Create | `src/CRM.Application/KnowledgeBase/Queries/ListKbArticlesQuery.cs` |
| Modify | `src/CRM.API/Controllers/KbArticlesController.cs` |
| Test   | `tests/CRM.Application.Tests/KnowledgeBase/GetKbArticleQueryHandlerTests.cs` |
| Test   | `tests/CRM.Application.Tests/KnowledgeBase/ListKbArticlesQueryHandlerTests.cs` |
| Test   | `tests/CRM.API.Tests/KnowledgeBase/KbArticlesControllerGetTests.cs` |

---

## Task 1: GetKbArticle + ListKbArticles Queries

**Files:**
- Create: `src/CRM.Application/KnowledgeBase/DTOs/KbArticleDetailDto.cs`
- Create: `src/CRM.Application/KnowledgeBase/Queries/GetKbArticleQuery.cs`
- Create: `src/CRM.Application/KnowledgeBase/Queries/ListKbArticlesQuery.cs`
- Test: `tests/CRM.Application.Tests/KnowledgeBase/GetKbArticleQueryHandlerTests.cs`
- Test: `tests/CRM.Application.Tests/KnowledgeBase/ListKbArticlesQueryHandlerTests.cs`

- [ ] **Step 1: Write the failing tests**

```csharp
// tests/CRM.Application.Tests/KnowledgeBase/GetKbArticleQueryHandlerTests.cs
using CRM.Application.KnowledgeBase.Queries;
using CRM.Domain.KnowledgeBase;
using Moq;
using Xunit;

namespace CRM.Application.Tests.KnowledgeBase;

public class GetKbArticleQueryHandlerTests
{
    private readonly Mock<IKbArticleRepository> _repo = new();
    private readonly GetKbArticleQueryHandler _handler;

    public GetKbArticleQueryHandlerTests()
    {
        _handler = new GetKbArticleQueryHandler(_repo.Object);
    }

    [Fact]
    public async Task Handle_AgentCaller_ReturnsAnyStatusArticle()
    {
        var article = KbArticle.Create(Guid.NewGuid(), "Title", Guid.NewGuid()); // Draft
        _repo.Setup(r => r.FindByIdAsync(article.Id, default)).ReturnsAsync(article);

        var result = await _handler.Handle(
            new GetKbArticleQuery(article.Id, isPortalCaller: false), default);

        Assert.Equal("Draft", result.Status);
    }

    [Fact]
    public async Task Handle_PortalCaller_InternalArticle_ThrowsUnauthorizedException()
    {
        var article = KbArticle.Create(Guid.NewGuid(), "Title", Guid.NewGuid(),
            KbVisibility.Internal, new string('x', 150));
        article.SubmitForReview();
        article.Approve(); // Published but Internal

        _repo.Setup(r => r.FindByIdAsync(article.Id, default)).ReturnsAsync(article);

        await Assert.ThrowsAsync<UnauthorizedAccessException>(() =>
            _handler.Handle(new GetKbArticleQuery(article.Id, isPortalCaller: true), default));
    }

    [Fact]
    public async Task Handle_PortalCaller_NonPublishedArticle_ThrowsKeyNotFoundException()
    {
        var article = KbArticle.Create(Guid.NewGuid(), "Title", Guid.NewGuid(),
            KbVisibility.Public, new string('x', 150)); // Draft, public — still not accessible

        _repo.Setup(r => r.FindByIdAsync(article.Id, default)).ReturnsAsync(article);

        await Assert.ThrowsAsync<KeyNotFoundException>(() =>
            _handler.Handle(new GetKbArticleQuery(article.Id, isPortalCaller: true), default));
    }

    [Fact]
    public async Task Handle_ArticleNotFound_ThrowsKeyNotFoundException()
    {
        _repo.Setup(r => r.FindByIdAsync(It.IsAny<Guid>(), default))
             .ReturnsAsync((KbArticle?)null);

        await Assert.ThrowsAsync<KeyNotFoundException>(() =>
            _handler.Handle(new GetKbArticleQuery(Guid.NewGuid(), false), default));
    }
}
```

```csharp
// tests/CRM.Application.Tests/KnowledgeBase/ListKbArticlesQueryHandlerTests.cs
using CRM.Application.Common;
using CRM.Application.KnowledgeBase.Queries;
using CRM.Domain.KnowledgeBase;
using Moq;
using Xunit;

namespace CRM.Application.Tests.KnowledgeBase;

public class ListKbArticlesQueryHandlerTests
{
    private readonly Mock<IKbArticleRepository> _repo = new();
    private readonly ListKbArticlesQueryHandler _handler;

    public ListKbArticlesQueryHandlerTests()
    {
        _handler = new ListKbArticlesQueryHandler(_repo.Object);
    }

    [Fact]
    public async Task Handle_AgentCaller_AppliesProvidedFilter()
    {
        var filter = new KbArticleFilter(KbArticleStatus.Published, null, null);
        _repo.Setup(r => r.ListAsync(
            It.Is<KbArticleFilter>(f => f.Status == KbArticleStatus.Published),
            1, 20, default))
            .ReturnsAsync(new PagedResult<KbArticle>(new List<KbArticle>(), 0, 1, 20));

        var result = await _handler.Handle(
            new ListKbArticlesQuery(filter, 1, 20, isPortalCaller: false), default);

        Assert.Equal(0, result.TotalCount);
    }

    [Fact]
    public async Task Handle_PortalCaller_ForcesPublishedAndPublicFilter()
    {
        _repo.Setup(r => r.ListAsync(
            It.Is<KbArticleFilter>(f =>
                f.Status == KbArticleStatus.Published &&
                f.Visibility != KbVisibility.Internal),
            1, 20, default))
            .ReturnsAsync(new PagedResult<KbArticle>(new List<KbArticle>(), 0, 1, 20));

        var result = await _handler.Handle(
            new ListKbArticlesQuery(null, 1, 20, isPortalCaller: true), default);

        Assert.Equal(0, result.TotalCount);
    }
}
```

- [ ] **Step 2: Run tests to verify they fail**

```bash
dotnet test tests/CRM.Application.Tests/ --filter "GetKbArticleQueryHandlerTests|ListKbArticlesQueryHandlerTests" -v n
```

Expected: FAIL — queries do not exist yet.

- [ ] **Step 3: Create DTO**

```csharp
// src/CRM.Application/KnowledgeBase/DTOs/KbArticleDetailDto.cs
namespace CRM.Application.KnowledgeBase.DTOs;

public record KbArticleDetailDto(
    Guid Id,
    string Title,
    string? TitleAr,
    string? Content,
    string? ContentAr,
    Guid CategoryId,
    string Status,
    string Visibility,
    Guid CreatedByUserId,
    DateTime? PublishedAt,
    string? RejectionNote,
    DateTime CreatedAt,
    DateTime UpdatedAt);
```

- [ ] **Step 4: Implement GetKbArticleQuery**

```csharp
// src/CRM.Application/KnowledgeBase/Queries/GetKbArticleQuery.cs
using CRM.Application.KnowledgeBase.DTOs;
using CRM.Domain.KnowledgeBase;
using MediatR;

namespace CRM.Application.KnowledgeBase.Queries;

public record GetKbArticleQuery(Guid ArticleId, bool IsPortalCaller)
    : IRequest<KbArticleDetailDto>;

public class GetKbArticleQueryHandler : IRequestHandler<GetKbArticleQuery, KbArticleDetailDto>
{
    private readonly IKbArticleRepository _articles;

    public GetKbArticleQueryHandler(IKbArticleRepository articles) => _articles = articles;

    public async Task<KbArticleDetailDto> Handle(GetKbArticleQuery query, CancellationToken ct)
    {
        var article = await _articles.FindByIdAsync(query.ArticleId, ct)
            ?? throw new KeyNotFoundException($"KB Article {query.ArticleId} not found.");

        if (query.IsPortalCaller)
        {
            if (article.Status != KbArticleStatus.Published)
                throw new KeyNotFoundException($"KB Article {query.ArticleId} not found.");

            if (article.Visibility == KbVisibility.Internal)
                throw new UnauthorizedAccessException(
                    "This article is for internal use only.");
        }

        return Map(article);
    }

    private static KbArticleDetailDto Map(KbArticle a)
        => new(a.Id, a.Title, a.TitleAr, a.Content, a.ContentAr,
               a.CategoryId, a.Status.ToString(), a.Visibility.ToString(),
               a.CreatedByUserId, a.PublishedAt, a.RejectionNote,
               a.CreatedAt, a.UpdatedAt);
}
```

- [ ] **Step 5: Implement ListKbArticlesQuery**

```csharp
// src/CRM.Application/KnowledgeBase/Queries/ListKbArticlesQuery.cs
using CRM.Application.Common;
using CRM.Application.KnowledgeBase.DTOs;
using CRM.Domain.KnowledgeBase;
using MediatR;

namespace CRM.Application.KnowledgeBase.Queries;

public record ListKbArticlesQuery(
    KbArticleFilter? Filter,
    int Page,
    int PageSize,
    bool IsPortalCaller) : IRequest<PagedResult<KbArticleSummaryDto>>;

public class ListKbArticlesQueryHandler
    : IRequestHandler<ListKbArticlesQuery, PagedResult<KbArticleSummaryDto>>
{
    private readonly IKbArticleRepository _articles;

    public ListKbArticlesQueryHandler(IKbArticleRepository articles) => _articles = articles;

    public async Task<PagedResult<KbArticleSummaryDto>> Handle(
        ListKbArticlesQuery query, CancellationToken ct)
    {
        var filter = query.IsPortalCaller
            ? new KbArticleFilter(KbArticleStatus.Published, query.Filter?.CategoryId,
                KbVisibility.Public) // portal: always Published + Public/Both
            : query.Filter ?? new KbArticleFilter(null, null, null);

        var paged = await _articles.ListAsync(filter, query.Page, query.PageSize, ct);

        var dtos = paged.Items
            .Select(a => new KbArticleSummaryDto(
                a.Id, a.Title, a.TitleAr, a.CategoryId,
                a.Status.ToString(), a.Visibility.ToString(),
                a.CreatedByUserId, a.CreatedAt))
            .ToList();

        return new PagedResult<KbArticleSummaryDto>(dtos, paged.TotalCount, paged.Page, paged.PageSize);
    }
}
```

- [ ] **Step 6: Run tests to verify they pass**

```bash
dotnet test tests/CRM.Application.Tests/ --filter "GetKbArticleQueryHandlerTests|ListKbArticlesQueryHandlerTests" -v n
```

Expected: 6 tests PASS.

- [ ] **Step 7: Add endpoints to KbArticlesController**

```csharp
// Add to src/CRM.API/Controllers/KbArticlesController.cs:

[HttpGet("{id:guid}")]
public async Task<IActionResult> GetById(Guid id, CancellationToken ct)
{
    try
    {
        var result = await _mediator.Send(new GetKbArticleQuery(id, false), ct);
        return Ok(result);
    }
    catch (KeyNotFoundException ex) { return NotFound(new { error = ex.Message }); }
}

[HttpGet]
public async Task<IActionResult> List(
    [FromQuery] string? status,
    [FromQuery] Guid? categoryId,
    [FromQuery] int page = 1,
    [FromQuery] int pageSize = 20,
    CancellationToken ct = default)
{
    KbArticleStatus? parsedStatus = Enum.TryParse<KbArticleStatus>(status, out var s) ? s : null;
    var filter = new KbArticleFilter(parsedStatus, categoryId, null);

    var result = await _mediator.Send(new ListKbArticlesQuery(filter, page, pageSize, false), ct);
    return Ok(result);
}
```

- [ ] **Step 8: Write controller test**

```csharp
// tests/CRM.API.Tests/KnowledgeBase/KbArticlesControllerGetTests.cs
using System.Net;
using System.Net.Http.Json;
using CRM.Application.Common;
using CRM.Application.KnowledgeBase.DTOs;
using CRM.Application.KnowledgeBase.Queries;
using MediatR;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using Xunit;

namespace CRM.API.Tests.KnowledgeBase;

public class KbArticlesControllerGetTests
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
    public async Task GetById_Returns200WithArticle()
    {
        var articleId = Guid.NewGuid();
        _mediator.Setup(m => m.Send(It.IsAny<GetKbArticleQuery>(), default))
                 .ReturnsAsync(new KbArticleDetailDto(
                     articleId, "Title", null, "Content...", null,
                     Guid.NewGuid(), "Published", "Internal",
                     Guid.NewGuid(), DateTime.UtcNow, null,
                     DateTime.UtcNow.AddDays(-1), DateTime.UtcNow));

        var response = await BuildClient().GetAsync($"/api/kb/articles/{articleId}");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task List_Returns200WithPagedResult()
    {
        _mediator.Setup(m => m.Send(It.IsAny<ListKbArticlesQuery>(), default))
                 .ReturnsAsync(new PagedResult<KbArticleSummaryDto>(
                     new List<KbArticleSummaryDto>(), 0, 1, 20));

        var response = await BuildClient().GetAsync("/api/kb/articles?page=1&pageSize=20");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }
}
```

- [ ] **Step 9: Run tests to verify they pass**

```bash
dotnet test tests/CRM.API.Tests/ --filter "KbArticlesControllerGetTests" -v n
```

Expected: 2 tests PASS.

- [ ] **Step 10: Commit**

```bash
git add src/CRM.Application/KnowledgeBase/DTOs/KbArticleDetailDto.cs \
        src/CRM.Application/KnowledgeBase/Queries/ \
        src/CRM.API/Controllers/KbArticlesController.cs \
        tests/CRM.Application.Tests/KnowledgeBase/GetKbArticleQueryHandlerTests.cs \
        tests/CRM.Application.Tests/KnowledgeBase/ListKbArticlesQueryHandlerTests.cs \
        tests/CRM.API.Tests/KnowledgeBase/KbArticlesControllerGetTests.cs
git commit -m "feat(kb): add GET /api/kb/articles and GET /api/kb/articles/{id} with portal access control"
```
