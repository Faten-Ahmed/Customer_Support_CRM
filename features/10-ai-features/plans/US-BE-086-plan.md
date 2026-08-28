# AI Suggest Articles — Implementation Plan

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

**Story:** US-BE-086  
**Goal:** Implement `POST /api/ai/tickets/{id}/suggest-articles` — returns up to 5 relevant KB articles. Only `Published + Public/Both` articles are candidates. For > 1000 published articles: pre-filter top 50 via SQL Server FTS before AI. Empty KB → returns `suggestions: []` without calling AI. `excerpt` = first 200 chars of article body.

**Architecture:** `SuggestArticlesQuery(TicketId, RequestingUserId)` → count published articles → if > 1000 pre-filter via `IKbArticleRepository.FullTextSearchAsync()` → call `IAzureOpenAiService.SuggestArticlesAsync()` → map results with excerpt. Adds action to `AiController`.

**Tech Stack:** .NET 10, ASP.NET Core, MediatR, Azure OpenAI SDK, xUnit, Moq

---

## File Structure

| Action | Path |
|--------|------|
| Create | `src/CRM.Application/AI/DTOs/AiSuggestArticlesDto.cs` |
| Create | `src/CRM.Application/AI/Queries/SuggestArticlesQuery.cs` |
| Modify | `src/CRM.API/Controllers/AiController.cs` |
| Test   | `tests/CRM.Application.Tests/AI/SuggestArticlesQueryHandlerTests.cs` |

---

## Task 1: AI Suggest Articles Query

> Note: `IAzureOpenAiService` is from US-BE-083. `IKbArticleRepository` is from US-BE-047. Implement those plans first.

- [ ] **Step 1: Write the failing tests**

```csharp
// tests/CRM.Application.Tests/AI/SuggestArticlesQueryHandlerTests.cs
using CRM.Application.AI.Queries;
using CRM.Domain.AI;
using CRM.Domain.KnowledgeBase;
using CRM.Domain.Tickets;
using Moq;
using Xunit;

namespace CRM.Application.Tests.AI;

public class SuggestArticlesQueryHandlerTests
{
    private readonly Mock<ITicketRepository> _tickets = new();
    private readonly Mock<IKbArticleRepository> _articles = new();
    private readonly Mock<IAzureOpenAiService> _ai = new();
    private readonly SuggestArticlesQueryHandler _handler;

    public SuggestArticlesQueryHandlerTests()
    {
        _handler = new SuggestArticlesQueryHandler(
            _tickets.Object, _articles.Object, _ai.Object);
    }

    [Fact]
    public async Task Handle_SmallKb_PassesAllArticlesToAi()
    {
        var ticketId = Guid.NewGuid();
        var artId = Guid.NewGuid();

        _tickets.Setup(r => r.GetSubjectAndBodyAsync(ticketId, default))
                .ReturnsAsync(new TicketSubjectBody("Login issue", "Can't access account"));
        _articles.Setup(r => r.CountPublishedPublicAsync(default)).ReturnsAsync(50);
        _articles.Setup(r => r.ListPublishedPublicSummariesAsync(default))
                 .ReturnsAsync(new List<KbArticleSummary>
                 {
                     new(artId, "How to reset password", "How to reset password",
                         "To reset your password, click Forgot Password on the login page...", "en")
                 });

        _ai.Setup(a => a.SuggestArticlesAsync(
                "Can't access account", It.IsAny<IReadOnlyList<string>>(), default))
           .ReturnsAsync(new AiTextResult(artId.ToString(), "gpt-4o-mini"));

        var result = await _handler.Handle(
            new SuggestArticlesQuery(ticketId, Guid.NewGuid()), default);

        Assert.Single(result);
        Assert.Equal(artId, result[0].ArticleId);
        Assert.Equal("How to reset password", result[0].Title);
    }

    [Fact]
    public async Task Handle_EmptyKb_ReturnsEmptyWithoutCallingAi()
    {
        var ticketId = Guid.NewGuid();
        _tickets.Setup(r => r.GetSubjectAndBodyAsync(ticketId, default))
                .ReturnsAsync(new TicketSubjectBody("Issue", "Body"));
        _articles.Setup(r => r.CountPublishedPublicAsync(default)).ReturnsAsync(0);

        var result = await _handler.Handle(
            new SuggestArticlesQuery(ticketId, Guid.NewGuid()), default);

        Assert.Empty(result);
        _ai.Verify(a => a.SuggestArticlesAsync(
            It.IsAny<string>(), It.IsAny<IReadOnlyList<string>>(), default), Times.Never);
    }

    [Fact]
    public async Task Handle_LargeKb_PreFiltersViaFts()
    {
        var ticketId = Guid.NewGuid();
        _tickets.Setup(r => r.GetSubjectAndBodyAsync(ticketId, default))
                .ReturnsAsync(new TicketSubjectBody("Login issue", "Can't log in"));
        _articles.Setup(r => r.CountPublishedPublicAsync(default)).ReturnsAsync(1500);
        _articles.Setup(r => r.FullTextSearchAsync("Can't log in", 50, default))
                 .ReturnsAsync(new List<KbArticleSummary>());

        _ai.Setup(a => a.SuggestArticlesAsync(
                It.IsAny<string>(), It.IsAny<IReadOnlyList<string>>(), default))
           .ReturnsAsync(new AiTextResult("", "gpt-4o-mini"));

        await _handler.Handle(
            new SuggestArticlesQuery(ticketId, Guid.NewGuid()), default);

        _articles.Verify(r => r.FullTextSearchAsync("Can't log in", 50, default), Times.Once);
        _articles.Verify(r => r.ListPublishedPublicSummariesAsync(default), Times.Never);
    }
}
```

- [ ] **Step 2: Run tests to verify they fail**

```bash
dotnet test tests/CRM.Application.Tests/ --filter "SuggestArticlesQueryHandlerTests" -v n
```

Expected: FAIL — `SuggestArticlesQuery` does not exist yet.

- [ ] **Step 3: Add methods to IKbArticleRepository**

Open `src/CRM.Domain/KnowledgeBase/IKbArticleRepository.cs` and add:

```csharp
public record KbArticleSummary(Guid Id, string Title, string TitleAr, string Body, string Language);

Task<int> CountPublishedPublicAsync(CancellationToken ct = default);
Task<IReadOnlyList<KbArticleSummary>> ListPublishedPublicSummariesAsync(
    CancellationToken ct = default);
Task<IReadOnlyList<KbArticleSummary>> FullTextSearchAsync(
    string query, int maxResults, CancellationToken ct = default);
```

- [ ] **Step 4: Create AiSuggestArticlesDto**

```csharp
// src/CRM.Application/AI/DTOs/AiSuggestArticlesDto.cs
namespace CRM.Application.AI.DTOs;

public record AiSuggestArticleDto(
    Guid ArticleId,
    string Title,
    string? TitleAr,
    double RelevanceScore,
    string Excerpt);
```

- [ ] **Step 5: Implement SuggestArticlesQuery**

```csharp
// src/CRM.Application/AI/Queries/SuggestArticlesQuery.cs
using CRM.Application.AI.DTOs;
using CRM.Domain.AI;
using CRM.Domain.KnowledgeBase;
using CRM.Domain.Tickets;
using MediatR;

namespace CRM.Application.AI.Queries;

public record SuggestArticlesQuery(Guid TicketId, Guid RequestingUserId)
    : IRequest<IReadOnlyList<AiSuggestArticleDto>>;

public class SuggestArticlesQueryHandler
    : IRequestHandler<SuggestArticlesQuery, IReadOnlyList<AiSuggestArticleDto>>
{
    private const int FtsThreshold = 1000;
    private const int FtsMaxResults = 50;
    private const int MaxSuggestions = 5;
    private const int ExcerptLength = 200;

    private readonly ITicketRepository _tickets;
    private readonly IKbArticleRepository _articles;
    private readonly IAzureOpenAiService _ai;

    public SuggestArticlesQueryHandler(
        ITicketRepository tickets, IKbArticleRepository articles, IAzureOpenAiService ai)
    {
        _tickets = tickets;
        _articles = articles;
        _ai = ai;
    }

    public async Task<IReadOnlyList<AiSuggestArticleDto>> Handle(
        SuggestArticlesQuery query, CancellationToken ct)
    {
        var ticketContent = await _tickets.GetSubjectAndBodyAsync(query.TicketId, ct);
        var count = await _articles.CountPublishedPublicAsync(ct);

        if (count == 0) return Array.Empty<AiSuggestArticleDto>();

        IReadOnlyList<KbArticleSummary> candidates;
        if (count > FtsThreshold)
        {
            candidates = await _articles.FullTextSearchAsync(
                ticketContent.Body, FtsMaxResults, ct);
        }
        else
        {
            candidates = await _articles.ListPublishedPublicSummariesAsync(ct);
        }

        if (candidates.Count == 0) return Array.Empty<AiSuggestArticleDto>();

        var titleList = candidates.Select(a => $"{a.Id}|{a.Title}").ToList();
        var aiResult = await _ai.SuggestArticlesAsync(ticketContent.Body, titleList, ct);

        // AI returns ranked article IDs; parse and map
        var articleMap = candidates.ToDictionary(a => a.Id);
        var rankedIds = aiResult.Text
            .Split('\n', StringSplitOptions.RemoveEmptyEntries)
            .Select(line => Guid.TryParse(line.Trim(), out var g) ? g : (Guid?)null)
            .Where(g => g.HasValue && articleMap.ContainsKey(g.Value))
            .Select(g => g!.Value)
            .Take(MaxSuggestions)
            .ToList();

        return rankedIds.Select((id, idx) =>
        {
            var art = articleMap[id];
            var excerpt = art.Body.Length > ExcerptLength
                ? art.Body[..ExcerptLength]
                : art.Body;
            return new AiSuggestArticleDto(
                art.Id, art.Title, art.TitleAr,
                1.0 - (idx * 0.1), // decreasing relevance by rank
                excerpt);
        }).ToList();
    }
}
```

- [ ] **Step 6: Run tests to verify they pass**

```bash
dotnet test tests/CRM.Application.Tests/ --filter "SuggestArticlesQueryHandlerTests" -v n
```

Expected: 3 tests PASS.

- [ ] **Step 7: Add SuggestArticles action to AiController**

Open `src/CRM.API/Controllers/AiController.cs` and add:

```csharp
[HttpPost("tickets/{id:guid}/suggest-articles")]
public async Task<IActionResult> SuggestArticles(Guid id, CancellationToken ct)
{
    try
    {
        var result = await _mediator.Send(
            new SuggestArticlesQuery(id, CurrentUserId), ct);
        return Ok(new { data = result });
    }
    catch (AiProviderException ex)
        { return StatusCode(503, new { error = ex.Message }); }
    catch (KeyNotFoundException ex)
        { return NotFound(new { error = ex.Message }); }
}
```

- [ ] **Step 8: Commit**

```bash
git add src/CRM.Application/AI/DTOs/AiSuggestArticlesDto.cs \
        src/CRM.Application/AI/Queries/SuggestArticlesQuery.cs \
        src/CRM.API/Controllers/AiController.cs \
        tests/CRM.Application.Tests/AI/SuggestArticlesQueryHandlerTests.cs
git commit -m "feat(ai): add POST /api/ai/tickets/{id}/suggest-articles — FTS pre-filter >1k, up to 5 KB suggestions"
```
