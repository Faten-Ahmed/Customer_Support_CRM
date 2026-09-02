# Search KB Articles — Implementation Plan

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

**Story:** US-BE-051  
**Goal:** Implement `GET /api/kb/search?q=` (agent — all Published) and `GET /api/portal/kb/search?q=` (customer — Published + Public/Both), with minimum 2-char query and results ranked by relevance. Each result includes a 200-char excerpt.

**Architecture:** `SearchKbQuery(query, portalOnly)` → validates query length, calls `IKbArticleRepository.SearchAsync(query, portalOnly, limit)`. The infrastructure layer implements SQL Server FTS (`CONTAINS` or `FREETEXT`). Handler maps to `KbSearchResultDto` with computed excerpt.

**Tech Stack:** .NET 10, ASP.NET Core, MediatR, EF Core (FTS), xUnit, Moq

---

## File Structure

| Action | Path |
|--------|------|
| Create | `src/CRM.Application/KnowledgeBase/DTOs/KbSearchResultDto.cs` |
| Create | `src/CRM.Application/KnowledgeBase/Queries/SearchKbQuery.cs` |
| Modify | `src/CRM.API/Controllers/KbArticlesController.cs` |
| Test   | `tests/CRM.Application.Tests/KnowledgeBase/SearchKbQueryHandlerTests.cs` |
| Test   | `tests/CRM.API.Tests/KnowledgeBase/KbSearchControllerTests.cs` |

---

## Task 1: SearchKb Query + Handler

**Files:**
- Create: `src/CRM.Application/KnowledgeBase/DTOs/KbSearchResultDto.cs`
- Create: `src/CRM.Application/KnowledgeBase/Queries/SearchKbQuery.cs`
- Test: `tests/CRM.Application.Tests/KnowledgeBase/SearchKbQueryHandlerTests.cs`

- [ ] **Step 1: Write the failing tests**

```csharp
// tests/CRM.Application.Tests/KnowledgeBase/SearchKbQueryHandlerTests.cs
using CRM.Application.KnowledgeBase.Queries;
using CRM.Domain.KnowledgeBase;
using FluentValidation;
using Moq;
using Xunit;

namespace CRM.Application.Tests.KnowledgeBase;

public class SearchKbQueryHandlerTests
{
    private readonly Mock<IKbArticleRepository> _repo = new();
    private readonly SearchKbQueryHandler _handler;

    public SearchKbQueryHandlerTests()
    {
        _handler = new SearchKbQueryHandler(_repo.Object);
    }

    private KbArticle MakePublishedArticle(string content)
    {
        var article = KbArticle.Create(Guid.NewGuid(), "Reset Password Guide", Guid.NewGuid(),
            KbVisibility.Public, content);
        article.SubmitForReview();
        article.Approve();
        return article;
    }

    [Fact]
    public async Task Handle_ValidQuery_ReturnsResultsWithExcerpt()
    {
        var content = "To reset your password, navigate to the login page and click 'Forgot Password'.";
        var article = MakePublishedArticle(content);

        _repo.Setup(r => r.SearchAsync("reset", false, It.IsAny<int>(), default))
             .ReturnsAsync(new List<KbArticle> { article });

        var results = await _handler.Handle(new SearchKbQuery("reset", portalOnly: false), default);

        Assert.Single(results);
        Assert.NotEmpty(results[0].Excerpt);
        Assert.True(results[0].Excerpt.Length <= 200);
    }

    [Fact]
    public async Task Handle_QueryTooShort_ThrowsValidationException()
    {
        await Assert.ThrowsAsync<ValidationException>(() =>
            _handler.Handle(new SearchKbQuery("a", portalOnly: false), default));
    }

    [Fact]
    public async Task Handle_EmptyQuery_ThrowsValidationException()
    {
        await Assert.ThrowsAsync<ValidationException>(() =>
            _handler.Handle(new SearchKbQuery("", portalOnly: false), default));
    }

    [Fact]
    public async Task Handle_PortalOnlyTrue_PassesPortalOnlyToRepository()
    {
        _repo.Setup(r => r.SearchAsync("password", true, It.IsAny<int>(), default))
             .ReturnsAsync(new List<KbArticle>());

        var results = await _handler.Handle(
            new SearchKbQuery("password", portalOnly: true), default);

        Assert.Empty(results);
        _repo.Verify(r => r.SearchAsync("password", true, It.IsAny<int>(), default), Times.Once);
    }
}
```

- [ ] **Step 2: Run test to verify it fails**

```bash
dotnet test tests/CRM.Application.Tests/ --filter "SearchKbQueryHandlerTests" -v n
```

Expected: FAIL — `SearchKbQuery` does not exist yet.

- [ ] **Step 3: Create DTO**

```csharp
// src/CRM.Application/KnowledgeBase/DTOs/KbSearchResultDto.cs
namespace CRM.Application.KnowledgeBase.DTOs;

public record KbSearchResultDto(
    Guid Id,
    string Title,
    string? TitleAr,
    Guid CategoryId,
    string Visibility,
    DateTime? PublishedAt,
    string Excerpt);
```

- [ ] **Step 4: Implement query and handler**

```csharp
// src/CRM.Application/KnowledgeBase/Queries/SearchKbQuery.cs
using CRM.Application.KnowledgeBase.DTOs;
using CRM.Domain.KnowledgeBase;
using FluentValidation;
using FluentValidation.Results;
using MediatR;

namespace CRM.Application.KnowledgeBase.Queries;

public record SearchKbQuery(string Query, bool PortalOnly)
    : IRequest<IReadOnlyList<KbSearchResultDto>>;

public class SearchKbQueryHandler
    : IRequestHandler<SearchKbQuery, IReadOnlyList<KbSearchResultDto>>
{
    private const int MaxResults = 20;

    private readonly IKbArticleRepository _articles;

    public SearchKbQueryHandler(IKbArticleRepository articles) => _articles = articles;

    public async Task<IReadOnlyList<KbSearchResultDto>> Handle(
        SearchKbQuery query, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(query.Query) || query.Query.Length < 2)
            throw new ValidationException(new[]
            {
                new ValidationFailure("q",
                    "Search query must be at least 2 characters.",
                    "QUERY_TOO_SHORT")
            });

        var results = await _articles.SearchAsync(
            query.Query, query.PortalOnly, MaxResults, ct);

        return results.Select(a => new KbSearchResultDto(
            a.Id, a.Title, a.TitleAr, a.CategoryId,
            a.Visibility.ToString(), a.PublishedAt,
            BuildExcerpt(a.Content, query.Query))).ToList();
    }

    private static string BuildExcerpt(string? content, string query)
    {
        if (string.IsNullOrEmpty(content)) return string.Empty;
        var idx = content.IndexOf(query, StringComparison.OrdinalIgnoreCase);
        var start = idx > 0 ? Math.Max(0, idx - 50) : 0;
        var excerpt = content.Substring(start, Math.Min(200, content.Length - start));
        return excerpt;
    }
}
```

- [ ] **Step 5: Run tests to verify they pass**

```bash
dotnet test tests/CRM.Application.Tests/ --filter "SearchKbQueryHandlerTests" -v n
```

Expected: 4 tests PASS.

- [ ] **Step 6: Add endpoint to KbArticlesController**

```csharp
// Add to src/CRM.API/Controllers/KbArticlesController.cs:

[HttpGet("/api/kb/search")]
public async Task<IActionResult> Search(
    [FromQuery] string q, CancellationToken ct)
{
    var results = await _mediator.Send(new SearchKbQuery(q ?? string.Empty, false), ct);
    return Ok(results);
}
```

- [ ] **Step 7: Write controller test**

```csharp
// tests/CRM.API.Tests/KnowledgeBase/KbSearchControllerTests.cs
using System.Net;
using System.Net.Http.Json;
using CRM.Application.KnowledgeBase.DTOs;
using CRM.Application.KnowledgeBase.Queries;
using MediatR;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using Xunit;

namespace CRM.API.Tests.KnowledgeBase;

public class KbSearchControllerTests
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
    public async Task Search_ValidQuery_Returns200WithResults()
    {
        _mediator.Setup(m => m.Send(It.IsAny<SearchKbQuery>(), default))
                 .ReturnsAsync(new List<KbSearchResultDto>
                 {
                     new(Guid.NewGuid(), "Reset Password", null, Guid.NewGuid(),
                         "Public", DateTime.UtcNow, "To reset your password...")
                 });

        var response = await BuildClient().GetAsync("/api/kb/search?q=reset");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var results = await response.Content.ReadFromJsonAsync<List<KbSearchResultDto>>();
        Assert.Single(results!);
    }

    [Fact]
    public async Task Search_QueryTooShort_Returns422()
    {
        _mediator.Setup(m => m.Send(It.IsAny<SearchKbQuery>(), default))
                 .ThrowsAsync(new FluentValidation.ValidationException("Query too short."));

        var response = await BuildClient().GetAsync("/api/kb/search?q=a");

        Assert.Equal(HttpStatusCode.UnprocessableEntity, response.StatusCode);
    }
}
```

- [ ] **Step 8: Run tests to verify they pass**

```bash
dotnet test tests/CRM.API.Tests/ --filter "KbSearchControllerTests" -v n
```

Expected: 2 tests PASS.

- [ ] **Step 9: Commit**

```bash
git add src/CRM.Application/KnowledgeBase/DTOs/KbSearchResultDto.cs \
        src/CRM.Application/KnowledgeBase/Queries/SearchKbQuery.cs \
        src/CRM.API/Controllers/KbArticlesController.cs \
        tests/CRM.Application.Tests/KnowledgeBase/SearchKbQueryHandlerTests.cs \
        tests/CRM.API.Tests/KnowledgeBase/KbSearchControllerTests.cs
git commit -m "feat(kb): add GET /api/kb/search with minimum 2-char validation and excerpt generation"
```
