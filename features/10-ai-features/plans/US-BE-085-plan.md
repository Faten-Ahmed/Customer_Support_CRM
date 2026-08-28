# AI Suggest Category — Implementation Plan

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

**Story:** US-BE-085  
**Goal:** Implement `POST /api/ai/tickets/{id}/suggest-category` — returns up to 3 category suggestions with `categoryId`, `categoryName`, `parentCategoryName`, `confidence`, `confidenceBand` (High ≥ 0.80, Medium 0.50–0.80, Low < 0.50), `label`. Suggestions with confidence < 0.20 filtered out. AI-hallucinated category IDs not in active list silently filtered. Agent confirms via `PUT /tickets/{id}`.

**Architecture:** `SuggestCategoryQuery(TicketId, RequestingUserId)` → fetches ticket subject/body → loads active categories → calls `IAzureOpenAiService.SuggestCategoriesAsync()` → filters invalid and low-confidence results. Adds action to `AiController`.

**Tech Stack:** .NET 10, ASP.NET Core, MediatR, Azure OpenAI SDK, xUnit, Moq

---

## File Structure

| Action | Path |
|--------|------|
| Create | `src/CRM.Application/AI/DTOs/AiSuggestCategoryDto.cs` |
| Create | `src/CRM.Application/AI/Queries/SuggestCategoryQuery.cs` |
| Modify | `src/CRM.API/Controllers/AiController.cs` |
| Test   | `tests/CRM.Application.Tests/AI/SuggestCategoryQueryHandlerTests.cs` |

---

## Task 1: AI Suggest Category Query

> Note: `IAzureOpenAiService` is from US-BE-083. `ICategoryRepository` is from US-BE-069. `ITicketRepository` is from US-BE-019. Implement those plans first.

- [ ] **Step 1: Write the failing tests**

```csharp
// tests/CRM.Application.Tests/AI/SuggestCategoryQueryHandlerTests.cs
using CRM.Application.AI.Queries;
using CRM.Domain.AI;
using CRM.Domain.Categories;
using CRM.Domain.Tickets;
using Moq;
using Xunit;

namespace CRM.Application.Tests.AI;

public class SuggestCategoryQueryHandlerTests
{
    private readonly Mock<ITicketRepository> _tickets = new();
    private readonly Mock<ICategoryRepository> _categories = new();
    private readonly Mock<IAzureOpenAiService> _ai = new();
    private readonly SuggestCategoryQueryHandler _handler;

    public SuggestCategoryQueryHandlerTests()
    {
        _handler = new SuggestCategoryQueryHandler(
            _tickets.Object, _categories.Object, _ai.Object);
    }

    [Fact]
    public async Task Handle_ReturnsSuggestionsFilteredByActiveCategories()
    {
        var ticketId = Guid.NewGuid();
        var activeCategoryId = Guid.NewGuid();
        var hallucCategoryId = Guid.NewGuid(); // not in active list

        _tickets.Setup(r => r.GetSubjectAndBodyAsync(ticketId, default))
                .ReturnsAsync(new TicketSubjectBody("Login issue", "Can't log in"));

        _categories.Setup(r => r.GetActiveCategorySummariesAsync(default))
                   .ReturnsAsync(new List<CategorySummary>
                   {
                       new(activeCategoryId, "Technical", null, "Technical")
                   });

        _ai.Setup(a => a.SuggestCategoriesAsync("Login issue", "Can't log in",
                It.IsAny<IReadOnlyList<string>>(), default))
           .ReturnsAsync(new List<AiCategorySuggestion>
           {
               new(activeCategoryId, "Technical", null, 0.92, "High", "Technical"),
               new(hallucCategoryId, "NonExistent", null, 0.75, "Medium", "NonExistent"),
               new(Guid.NewGuid(), "LowConf", null, 0.15, "Low", "LowConf")
           });

        var result = await _handler.Handle(
            new SuggestCategoryQuery(ticketId, Guid.NewGuid()), default);

        // hallucinated and <0.20 confidence filtered
        Assert.Single(result);
        Assert.Equal("High", result[0].ConfidenceBand);
        Assert.Equal(activeCategoryId, result[0].CategoryId);
    }

    [Fact]
    public async Task Handle_EmptyCategories_ReturnsEmptyList()
    {
        var ticketId = Guid.NewGuid();
        _tickets.Setup(r => r.GetSubjectAndBodyAsync(ticketId, default))
                .ReturnsAsync(new TicketSubjectBody("Test", "Body"));
        _categories.Setup(r => r.GetActiveCategorySummariesAsync(default))
                   .ReturnsAsync(new List<CategorySummary>());

        var result = await _handler.Handle(
            new SuggestCategoryQuery(ticketId, Guid.NewGuid()), default);

        Assert.Empty(result);
        _ai.Verify(a => a.SuggestCategoriesAsync(
            It.IsAny<string>(), It.IsAny<string>(), It.IsAny<IReadOnlyList<string>>(), default),
            Times.Never);
    }

    [Fact]
    public async Task Handle_ConfidenceBands_AssignedCorrectly()
    {
        var ticketId = Guid.NewGuid();
        var catHigh = Guid.NewGuid();
        var catMed = Guid.NewGuid();
        var catLow = Guid.NewGuid();

        _tickets.Setup(r => r.GetSubjectAndBodyAsync(ticketId, default))
                .ReturnsAsync(new TicketSubjectBody("Billing", "Invoice wrong"));

        _categories.Setup(r => r.GetActiveCategorySummariesAsync(default))
                   .ReturnsAsync(new List<CategorySummary>
                   {
                       new(catHigh, "Billing", null, "Billing"),
                       new(catMed, "Account", null, "Account"),
                       new(catLow, "Other", null, "Other")
                   });

        _ai.Setup(a => a.SuggestCategoriesAsync(
                It.IsAny<string>(), It.IsAny<string>(), It.IsAny<IReadOnlyList<string>>(), default))
           .ReturnsAsync(new List<AiCategorySuggestion>
           {
               new(catHigh, "Billing", null, 0.85, "High", "Billing"),
               new(catMed, "Account", null, 0.60, "Medium", "Account"),
               new(catLow, "Other", null, 0.30, "Low", "Other")
           });

        var result = await _handler.Handle(
            new SuggestCategoryQuery(ticketId, Guid.NewGuid()), default);

        Assert.Equal(3, result.Count);
        Assert.Equal("High", result[0].ConfidenceBand);
        Assert.Equal("Medium", result[1].ConfidenceBand);
        Assert.Equal("Low", result[2].ConfidenceBand);
    }
}
```

- [ ] **Step 2: Run tests to verify they fail**

```bash
dotnet test tests/CRM.Application.Tests/ --filter "SuggestCategoryQueryHandlerTests" -v n
```

Expected: FAIL — `SuggestCategoryQuery` does not exist yet.

- [ ] **Step 3: Add required types to ITicketRepository and ICategoryRepository**

Open `src/CRM.Domain/Tickets/ITicketRepository.cs` and add:

```csharp
public record TicketSubjectBody(string Subject, string Body);

Task<TicketSubjectBody> GetSubjectAndBodyAsync(Guid ticketId, CancellationToken ct = default);
```

Open `src/CRM.Domain/Categories/ICategoryRepository.cs` and add:

```csharp
public record CategorySummary(Guid Id, string Name, string? ParentName, string Label);

Task<IReadOnlyList<CategorySummary>> GetActiveCategorySummariesAsync(
    CancellationToken ct = default);
```

- [ ] **Step 4: Create AiSuggestCategoryDto**

```csharp
// src/CRM.Application/AI/DTOs/AiSuggestCategoryDto.cs
namespace CRM.Application.AI.DTOs;

public record AiSuggestCategoryDto(
    Guid CategoryId,
    string CategoryName,
    string? ParentCategoryName,
    double Confidence,
    string ConfidenceBand,
    string Label);
```

- [ ] **Step 5: Implement SuggestCategoryQuery**

```csharp
// src/CRM.Application/AI/Queries/SuggestCategoryQuery.cs
using CRM.Application.AI.DTOs;
using CRM.Domain.AI;
using CRM.Domain.Categories;
using CRM.Domain.Tickets;
using MediatR;

namespace CRM.Application.AI.Queries;

public record SuggestCategoryQuery(Guid TicketId, Guid RequestingUserId)
    : IRequest<IReadOnlyList<AiSuggestCategoryDto>>;

public class SuggestCategoryQueryHandler
    : IRequestHandler<SuggestCategoryQuery, IReadOnlyList<AiSuggestCategoryDto>>
{
    private const double MinConfidence = 0.20;

    private readonly ITicketRepository _tickets;
    private readonly ICategoryRepository _categories;
    private readonly IAzureOpenAiService _ai;

    public SuggestCategoryQueryHandler(
        ITicketRepository tickets, ICategoryRepository categories, IAzureOpenAiService ai)
    {
        _tickets = tickets;
        _categories = categories;
        _ai = ai;
    }

    public async Task<IReadOnlyList<AiSuggestCategoryDto>> Handle(
        SuggestCategoryQuery query, CancellationToken ct)
    {
        var ticketContent = await _tickets.GetSubjectAndBodyAsync(query.TicketId, ct);
        var activeCategories = await _categories.GetActiveCategorySummariesAsync(ct);

        if (activeCategories.Count == 0) return Array.Empty<AiSuggestCategoryDto>();

        var activeLabelList = activeCategories.Select(c => c.Label).ToList();
        var suggestions = await _ai.SuggestCategoriesAsync(
            ticketContent.Subject, ticketContent.Body, activeLabelList, ct);

        var activeCategoryMap = activeCategories.ToDictionary(c => c.Id);

        return suggestions
            .Where(s => s.Confidence >= MinConfidence && activeCategoryMap.ContainsKey(s.CategoryId))
            .OrderByDescending(s => s.Confidence)
            .Take(3)
            .Select(s =>
            {
                var cat = activeCategoryMap[s.CategoryId];
                var band = s.Confidence >= 0.80 ? "High"
                    : s.Confidence >= 0.50 ? "Medium" : "Low";
                return new AiSuggestCategoryDto(
                    s.CategoryId, cat.Name, cat.ParentName, s.Confidence, band, cat.Label);
            })
            .ToList();
    }
}
```

- [ ] **Step 6: Run tests to verify they pass**

```bash
dotnet test tests/CRM.Application.Tests/ --filter "SuggestCategoryQueryHandlerTests" -v n
```

Expected: 3 tests PASS.

- [ ] **Step 7: Add SuggestCategory action to AiController**

Open `src/CRM.API/Controllers/AiController.cs` and add:

```csharp
[HttpPost("tickets/{id:guid}/suggest-category")]
public async Task<IActionResult> SuggestCategory(Guid id, CancellationToken ct)
{
    try
    {
        var result = await _mediator.Send(
            new SuggestCategoryQuery(id, CurrentUserId), ct);
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
git add src/CRM.Application/AI/DTOs/AiSuggestCategoryDto.cs \
        src/CRM.Application/AI/Queries/SuggestCategoryQuery.cs \
        src/CRM.API/Controllers/AiController.cs \
        tests/CRM.Application.Tests/AI/SuggestCategoryQueryHandlerTests.cs
git commit -m "feat(ai): add POST /api/ai/tickets/{id}/suggest-category — up to 3 filtered suggestions with confidence bands"
```
